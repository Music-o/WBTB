using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VivoxUnity;
using System.Net;

public class VoiceManager : MonoBehaviour
{

    public enum ChangedProperty
    {
        // OnAfterTYPEValueUpdated 이벤트를 subscribe하는 함수에서 사용됨
        None,
        Speaking,
        Typing,
        Muted
    }

    public enum ChatCapability
    {
        // 채팅에 참여할때 조건을 위한 enum
        TextOnly,
        AudioOnly,
        TextAndAudio
    };

    // delegate : 다른 method와 연결하여 인스턴스화 시켜주는 것(이벤트를 처리하기 위한것, method를 가리킬 수 있는 타입의 간단한 표기)
    // event : 이벤트를 정의(delegate를 사용해 이벤트를 처리하기 위한 것)
    //event에 += method를 해주면 그 event의 handler로 등록하는 것과 같음(event 구독, -=시 구독 해지 느낌)

    // participant의 value의 변화에 관한 event
    public delegate void ParticipantValueChangedHandler(string username, ChannelId channel, bool value);
    public event ParticipantValueChangedHandler OnSpeechDetectedEvent;
    public delegate void ParticipantValueUpdatedHandler(string username, ChannelId channel, double value);
    public event ParticipantValueUpdatedHandler OnAudioEnergyChangedEvent;  // 볼륨 변화 이벤트

    // Participant상태의 변화에 관한 event
    public delegate void ParticipantStatusChangedHandler(string username, ChannelId channel, IParticipant participant);
    public event ParticipantStatusChangedHandler OnParticipantAddedEvent;
    public event ParticipantStatusChangedHandler OnParticipantRemovedEvent;

    // Channel의 text Message의 변화?event
    public delegate void ChannelTextMessageChangedHandler(string sender, IChannelTextMessage channelTextMessage);
    public event ChannelTextMessageChangedHandler OnTextMessageLogReceivedEvent;

    // login 상태에 관한 event
    public delegate void LoginStatusChangedHandler();
    public event LoginStatusChangedHandler OnUserLoggedInEvent;
    public event LoginStatusChangedHandler OnUserLoggedOutEvent;

    public string channelName;

    private Uri _serverUri
    {
        // server의 uri에 대한 property
        // get, set 등 C#의 속성은, 해당 속성을 읽을 때, 실행시킬 구문들을 지정
        get => new Uri(_server);
        set
        {
            _server = value.ToString();
        }
    }
    [SerializeField]
    private string _server = "https://mt1s.www.vivox.com/api2"; //vivox server 주소
    [SerializeField]
    private string _domain = "mt1s.vivox.com";  // domain
    [SerializeField]
    private string _tokenIssuer = "noneng2580-te97-dev";    // token issuer
    [SerializeField]
    private string _tokenKey = "bank866";   // token key
    private TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);   // TimeSpan => 시간간격 , token 만료 시간

    private Client _client = new Client();  // 연결하는 session 정보를 가질 client
    private AccountId _accountId;   // 연결할 계정 및 접속 정보를 가지는 object

    public static VoiceManager Instance = null;
    private void Awake()
    {
       // Debug.Log("voiceManager Awake");
        //싱글턴 패턴 object로 만들어줌
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);  
    }
    
    private void Start()
    {
        //Debug.Log("voiceManager Start");
        _client.Uninitialize(); //client를 uninitialize시켜줌

        _client.Initialize();  //client를 initialize시켜줌
        channelName = new WebClient().DownloadString("https://api.ipify.org").Replace(".", "");
    }

    private void OnApplicationQuit()
        //application 종료시 수행할 동작
    {
        Client.Cleanup();   // client를 전부 지움??
        if(_client != null)
        {
            VivoxLog("Uninitializing client.");
            _client.Uninitialize(); // uninitialize
            _client = null; // 
        }
    }

    public LoginState loginState { get; private set; }   // login session의 상태
    public ILoginSession LoginSession;  // Client와 accountId를 이용한 로그인 session 
    public VivoxUnity.IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannels => LoginSession?.ChannelSessions;
    // =>는 람다 연산자가 아니라 멤버에 대한 정의를 하는것
    // ?. 은 왼쪽이 null이면 결과는 null이고 아니면 멤버를 호출한다.
    // 여기서는 ActiveChannels에 LoginSession이 null이 아니면 LoginSession.ChannelSessoins를 호출하는것
    public IAudioDevices AudioInputDevices => _client.AudioInputDevices;   // audio를 관리하기 위한 IAudioDevices에 client의 audio input device를 넣음
    public IAudioDevices AudioOutputDevices => _client.AudioOutputDevices;  // audio를 관리하기 위한 IAudioDevices에 client의 audio output device를 넣음

    public IChannelSession TransmittingSession  // channel 연결
    {
        get
        {
            if (_client == null)
                throw new NullReferenceException("client");
            return _client.GetLoginSession(_accountId).ChannelSessions.FirstOrDefault(x => x.IsTransmitting);
        }
        set
        {
            if (value != null)
            {
                _client.GetLoginSession(_accountId).SetTransmissionMode(TransmissionMode.Single, value.Channel);
            }
        }
    }

    public void setChannelName()
    {
        channelName = GameObject.Find("InputAddress").GetComponent<Text>().text;
        if (channelName == "localhost")
            channelName = new WebClient().DownloadString("https://api.ipify.org");
        channelName = channelName.Replace(".", "");
        Debug.Log("channelName");
    }

    public void Login(string displayName = null)
    {   //vivox instance에 사용자를 login 시킴
        //Debug.Log("Login");
        string uniqueId = Guid.NewGuid().ToString();    // client에게 할당할 unique한 id
        _accountId = new AccountId(_tokenIssuer, uniqueId, _domain, displayName);   // accounr id 초기화
        // 각 participant는 unique한 AccountId를 가져야함 (AccountId class의 name부분)
        LoginSession = _client.GetLoginSession(_accountId); // client의 loginSession을 가져옴
        LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;  // PropertyChanged event를 구독함
        // LoginSession.PropertyChanged는 로그인 세션의 상태가 변경되었을때의 event
        LoginSession.BeginLogin(_serverUri, LoginSession.GetLoginToken(_tokenKey, _tokenExpiration), ar =>
        {   // ar은 로그인이 완료된 후 호출되는 AsyncCallback
            try
            {
                // 호출시 오류가 발생하는 경우 try 블록내에서 EndLogin method를 호출해야함, error가 발생하지 않으면 user가 로그인 된것
                LoginSession.EndLogin(ar);
            }
            catch(Exception e)
            {
                //error handling
                LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;  //PropertyChanged event를 구독 해지함
                return;
            }   // user를 Vivox에 로그인 시키기 위한 호출
            // 이 시점에서 login이 완료되고 다른 작업이 수행 가능해짐
        });
        // GetLoginToken을 사용한 토큰 생성은 나중에 가능하면 수정 보안의 문제나 토큰 만료(token expiration) 오류가 발생할 수 있다고 함
    }

    private void OnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {   // LoginSession.PropertyChanged event를 확인해 State값에 따른 처리
        //로그인 세션의 상태가 변경되었을때의 event 처리 method
        if (propertyChangedEventArgs.PropertyName != "State")   // 발생한 event의 PropertyName이 State가 아닌경우 return
            return;
        var loginSession = (ILoginSession)sender;   // event를 발생시킨 session을 가져옴
        
        loginState = loginSession.State;    // event를 발생시킨 session의 state를 가져옴
        VivoxLog("Detecting Login Session Change voice manager"); //log 찍기
        //Debug.Log("OnLoginSessionPropertyChanged"+loginState);
        switch (loginState)
        {
            case LoginState.LoggingIn:
                VivoxLog("Logging in");
                break;
            case LoginState.LoggingOut:
                VivoxLog("Logging out");
                break;
            case LoginState.LoggedIn:
                VivoxLog("Connected to voice server and logged in.");
                OnUserLoggedInEvent?.Invoke();
                break;
            case LoginState.LoggedOut:  //vivox 연결이 예기치 못한 문제로 끊어지게 되는 경우(user가 logout한것 x)
                VivoxLog("Logged out");
                LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;  // PropertyChanged event 구독 해지
                break;
            default:
                break;
        }
    }

    public void LogOut()
    {   // vivox instance에서 user를 log out 시킴
        Debug.Log("LogOut  voice manager");
        if (LoginSession != null && loginState != LoginState.LoggedOut && loginState != LoginState.LoggingOut)
        {
            // LoginSession이 null이거나 LoginState가 LoggedOut이나 LogginOut인경우는 이미 로그아웃된 상태이므로 세 경우가 아닐때 수행
            OnUserLoggedOutEvent?.Invoke();  // OnUserLoggedInEvent가 null이라면 아무일도x 실행될 method가 있다면 호출
            LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;  // PropertyChanged event 구독 해지
            LoginSession.Logout();  // LogOut 실행
        }
    }

    /*
     * LoginSession.PropertyChanged : 로그인 세션 상태 변경 event
     * ChannelSession.PropertyChanged : 채널 상태 변경 event
     * ChannelSession.Participants.AfterKeyAdded : 참가자 추가 event (user join channel)
     * ChannelSession.Participants.BeforeKeyRemoved : 참가자 제거 event (user leave channel)
     * ChannelSession.Participants.AfterValueUpdate : 참가자 정보 update event (user의 speaking이나 typing같은 상태)
     * ChannelSession.MessageLog.AfterItemAdded : 채널 채팅 메시지 추가 event
    */ 
    public void JoinChannel(string channelName, ChannelType channelType, ChatCapability chatCapability, bool switchTransmission = true, Channel3DProperties properties = null)
    {   // channel에 join하기 위해서는 Channel에 대한 identifier와 channel type을 사용해 IChannelSession을 만들고 BeginConnect()를 사용해 호출해야함
        //Debug.Log("JoinChannel  voice manager");
        if (loginState == LoginState.LoggedIn)   // loginState가 LoggedIn상태인경우 채널 연결 수행
        {
            ChannelId channelId = new ChannelId(_tokenIssuer, channelName, _domain, channelType, properties);
            // channel 이름으로 대소문자만 다른건 불가능
            // client는 한번에 최대 10개의 채널 까지만 참여가능
            IChannelSession channelSession = LoginSession.GetChannelSession(channelId); // LoginSession의 ChannelSession가져옴
            channelSession.PropertyChanged += OnChannelPropertyChanged; // 채널의 상태 변경 event 구독
            channelSession.Participants.AfterKeyAdded += OnParticipantAdded;    // 참가자 추가 event 구독
            channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;   // 참가자 제거 event 구독
            channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated; // 참가자 정보 update event 구독
            channelSession.MessageLog.AfterItemAdded += OnMessageLogRecieved;   // 채널 채팅 메시지 추가 event 구독
            channelSession.BeginConnect(chatCapability != ChatCapability.TextOnly, chatCapability != ChatCapability.AudioOnly, switchTransmission, channelSession.GetConnectToken(_tokenKey, _tokenExpiration), ar =>
            {
                try
                {
                    channelSession.EndConnect(ar);
                    // error가 발생하지 않고 EndConnect가 실행된 경우 채널에 성공적으로 참여한 것이 아니라 method 호출이 완료되었다는 것 channel이 join되는 시기를 확인 하려면 IChannelSession.PropertyChanged를 확인해야함
                }
                catch (Exception e)
                {
                    //Handle error
                    VivoxLogError($"Could not connect to voice channel: {e.Message}");
                    return;
                }
            }); // 채널에 연결
            //connectAudio 인자와 connectText 인자로 플레이어가 채널에 참가할때 텍스트나 오디오를 사용할지 여부를 결정함, switchTransmission 인자를 true로 설정해 자동으로 새로 연결한 채널로만 오디오 전송을 하도록 함
            // GetLoginToken을 사용한 토큰 생성은 나중에 가능하면 수정 보안의 문제나 토큰 만료(token expiration) 오류가 발생할 수 있다고 함
        }
        else
        {
            VivoxLogError("Cannot join a channel when not logged in");
        }
    }

    public void SendTextMessage(string messageToSend, ChannelId channel, string applicationStanzaNamesapce = null, string applicationStanzaBody = null)
    {
        // text를 활성화한 생태로 채널에 참여한 경우 user가 group text message 보내고 받기 위한 method
        // messageToSend : 보낼 message
        // channel : 사용할 channel
        // applicationStanzaNamesapce : ??
        // applicationStanzaBody : ??
        // 아래 두개는 document에서는 안씀
        if (ChannelId.IsNullOrEmpty(channel))   // channel인자가 비어있는경우 error
        {
            throw new ArgumentNullException("Must provide a valid ChannelId");
        }
        if (string.IsNullOrEmpty(messageToSend))    // messageToSend인자가 비어있는 경우 error(보낼 메시지가 안 들어온경우)
        {
            throw new ArgumentNullException("Must provide a message to send");
        }

        var channelSession = LoginSession.GetChannelSession(channel);   // channel 인자의 channel session가져옴
        channelSession.BeginSendText(null, messageToSend, applicationStanzaNamesapce, applicationStanzaBody, ar =>
        {  
            try
            {
                channelSession.EndSendText(ar);
            }
            catch(Exception e)
            {
                VivoxLog($"SendTextMessage failed with exception {e.Message}");
            }
        });
        // channelSession에 받아온 channel로 message를 보내기 위한 method 호출
        // 해당 채널의 다른 user는 IChannelSession.MessageLog.AfterItemAdded event를 받게됨
    }

    public void DisconnectAllChannels()
    {
        Debug.Log("DisconnectAllChannel");
        if (ActiveChannels?.Count > 0)  // ActiveChannel이 null이 아닌 경우 0보다 큰지 확인
        {
            foreach(var channelSession in ActiveChannels)   // ActiveChannel의 channelSession들 Discionnect
            {
                channelSession?.Disconnect();
            }
        }
    }

    private static void ValidateArgs(object[] objs)
    {
        // object들이 null인 경우 error handling
        foreach(var obj in objs)
        {
            if (obj == null)
                throw new ArgumentNullException(obj.GetType().ToString().ToString(), "Specify a non-null/non-empty argument.");
        }
    }

    private void OnMessageLogRecieved(object sender, QueueItemAddedEventArgs<IChannelTextMessage> textMessage)
    {
        //Debug.Log("OnMessageLogRecieved voiceManager");
        // Channel이 message를 받는 event handler method
        ValidateArgs(new object[] { sender, textMessage });

        IChannelTextMessage channelTextMessage = textMessage.Value; // message의 값 받아옴
        VivoxLog(channelTextMessage.Message);   // Log에 message 출력
        OnTextMessageLogReceivedEvent?.Invoke(channelTextMessage.Sender.DisplayName, channelTextMessage);   // OnTextMessageLogReceivedEvent event호출
    }

    private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
    {
        //Debug.Log("OnParticipantAdded voiceManager");
        // 참가자 추가 event handler
        ValidateArgs(new object[] { sender, keyEventArg }); 

        // sender는 event를 변경하고 trigger하는 dictionary이다. access하려면 다시 casting해야함
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        // key를 통한 participant lookup
        var participant = source[keyEventArg.Key];  // source로부터  participant 정보 가져옴
        var username = participant.Account.Name;    // participant의 name 가져옴
        var channel = participant.ParentChannelSession.Key; // participant의 channel session 의 key가져옴
        var channelSession = participant.ParentChannelSession;  // participant의 channel session가져옴

        // Trigger callback
        OnParticipantAddedEvent?.Invoke(username, channel, participant);
    }

    private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
    {
        // 참가자 정보 update(user의 speaking이나 typing같은 상태) event handler
        ValidateArgs(new object[] { sender, valueEventArg });

        // sender는 event를 변경하고 trigger하는 dictionary이다. access하려면 다시 casting해야함
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>) sender;  // surce에 sender의 정보 casting
        // key를 통한 participant lookup
        var participant = source[valueEventArg.Key];  // source로부터  participant 정보 가져옴
        // 왜 OnParticipantAdded처럼 participant에서 호출안했는지 알수없음 값은 똑같을듯
        string username = valueEventArg.Value.Account.Name;   // participant의 name 가져옴
        ChannelId channel = valueEventArg.Value.ParentChannelSession.Key; // participant의 channel session 의 key가져옴
        string property = valueEventArg.PropertyName;     // participant의 변화된 property의 name 가져옴

        switch (property)
        {
            // property값에 따른 event handling
            case "SpeechDetected":
                VivoxLog($"OnSpeechDetectedEvent: {username} in {channel}.");
                OnSpeechDetectedEvent?.Invoke(username, channel, valueEventArg.Value.SpeechDetected);
                break;
            case "AudioEnergy":
                OnAudioEnergyChangedEvent?.Invoke(username, channel, valueEventArg.Value.AudioEnergy);
                break;
            default:
                break;
        }
    }

    private void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
    {
        //Debug.Log("OnParticipantRemoved voiceManager");
        // 참가자 제거 event handler
        ValidateArgs(new object[] { sender, keyEventArg });

        // sender는 event를 변경하고 trigger하는 dictionary이다. access하려면 다시 casting해야함
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;  // source에 sender의 정보 casting
        // key를 통한 participant lookup
        var participant = source[keyEventArg.Key];  // source로부터  participant 정보 가져옴
        var username = participant.Account.Name;   // participant의 name 가져옴
        var channel = participant.ParentChannelSession.Key; // participant의 channel session 의 key가져옴
        var channelSession = participant.ParentChannelSession;     // participant의 channel session가져옴

        if (participant.IsSelf) // 자신과 관련된 event인지 다른 사람과 관련된 event인지 확인
        {
            VivoxLog($"Unsubscribing from: {channelSession.Key.Name}");
            //disconnect되었으므로 unsubscribe
            channelSession.PropertyChanged -= OnChannelPropertyChanged;     // 채널 세션 상태 변경 event 구독 해지
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;     // 참가자 추가 event (user join channel) 구독 해지
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;     // 참가자 제거 event (user leave channel) 구독 해지
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;     // 참가자 정보 update event (user의 speaking이나 typing같은 상태) 구독 해지
            channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;     // 채널 채팅 메시지 추가 event 구독 해지

            // session 제거
            var user = _client.GetLoginSession(_accountId); // user에 client의 LoginSession을 가져옴
            user.DeleteChannelSession(channelSession.Channel);  // user에서 channel session을 제거
        }
        // Trigger callback
        OnParticipantRemovedEvent?.Invoke(username, channel, participant);
    }

    private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        //Debug.Log("OnChannelPorpertyChanged voiceManager");
        // 채널의 상태 변화 event handler
        ValidateArgs(new object[] { sender, propertyChangedEventArgs });

        // sender는 event를 변경하고 trigger하는 dictionary이다. access하려면 다시 casting해야함
        var channelSession = (IChannelSession)sender;  // source에 sender의 정보 casting

        // channel에서 audio가 제거된 경우 모든 VAD indicator가 speaking을 안보여주는지 확인
        if (propertyChangedEventArgs.PropertyName == "AudioState" && channelSession.AudioState == ConnectionState.Disconnected)
        {// event가 발생한 propert name이 AudioState이고 channelSession의 AudioState가 Disconnected인경우
            VivoxLog($"Audio disconnected from: {channelSession.Key.Name}");

            // channelSession의 모든 participant에게 OnSpeechDetectedEvent 호출
            foreach (var participant in channelSession.Participants)
                OnSpeechDetectedEvent?.Invoke(participant.Account.Name, channelSession.Channel, false);
        }

        //channel이 전부 disconnect됐으면 unsubscribe하고 제거
        if((propertyChangedEventArgs.PropertyName == "AudioState"||propertyChangedEventArgs.PropertyName == "TextState") && channelSession.AudioState == ConnectionState.Disconnected && channelSession.TextState == ConnectionState.Disconnected)
        {// event의 property name이 AudioState이거나 TextState이고 AudioState 와 TextState가 Disconnected인 경우
            VivoxLog($"Unsubscribing from:{channelSession.Key.Name}");
            channelSession.PropertyChanged -= OnChannelPropertyChanged;     // 채널 세션 상태 변경 event 구독 해지
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;     // 참가자 추가 event (user join channel) 구독 해지
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;     // 참가자 제거 event (user leave channel) 구독 해지
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;     // 참가자 정보 update event (user의 speaking이나 typing같은 상태) 구독 해지
            channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;     // 채널 채팅 메시지 추가 event 구독 해지

            //session 제거
            var user = _client.GetLoginSession(_accountId); // user에 client의 login sessino 가져옴
            user.DeleteChannelSession(channelSession.Channel);  // user에서 channel session 제거

        }
    }
    public void setEchoCancelation(bool onOff)
    {
        _client.SetAudioEchoCancellation(onOff);
    }

    private void VivoxLog(string msg)   // 로그찍는 method
    {
        Debug.Log("<color=green>VivoxVoice: </color>: " + msg);
    }

    private void VivoxLogError(string msg)  // error 로그 찍는 method
    {
        Debug.LogError("<color=green>VivoxVoice: </color>: " + msg);
    }
}
