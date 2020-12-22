using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using VivoxUnity;
using System.Net;
using System.Linq;
using UnityEngine.Android;
using System.Text;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public class NetworkManagerHUDWBTB : NetworkBehaviour // 서버의 Connect&Disconnect를 관리하는 스크립트.
{
    NetworkRoomManagerWBTB manager; // NetworkRoomManager의 기능을 이용할 예정이므로 이 변수를 정의.
    public GameObject errorPanel;
    public GameObject hostingRoomHideBtn;
    public GameObject searchingRoomHideBtn;
    public Button hostingBtn;
    public GameObject searchingBtn;
    public InputField PlayerName;
    public string SetPlayerName;

    [Header("Winner")]
    public SyncListBool alienDisList;
    public SyncListInt colorDisList;
    [SyncVar]
    public bool isAlienWin;

    public SyncListString PlayerNameList_;
    public enum MatchStatus
    {
        Open,
        Closed,
        Seeking
    }

    private VoiceManager _voiceManager;
    private bool PermissionsDenied; // 마이크 사용 permission에 관한 변수

    public string clientPublicIpAddress // client의 public ip address
    {
        get
        {
            string publicIpAddress = new WebClient().DownloadString("https://api.ipify.org");    //외부 ip를 알려주는 페이지에서 ip가져옴
            if (string.IsNullOrWhiteSpace(publicIpAddress)) // publicIpAddress에 ip가 저장되지 않은경우 error
                Debug.LogError("Unable to find clientPublicIpAddress");
            return publicIpAddress.Trim();  // 앞뒤 공백 제거
        }
    }

    public string HostingIp // hosting 하고 있는 ip
    {
        get
        {
            if (NetworkServer.active || NetworkClient.serverIp == "localhost")   // server가 active이고 server ip가 localhost인 경우
            {
                return clientPublicIpAddress;   // client ip return
            }
            return NetworkClient.serverIp ?? clientPublicIpAddress; // server ip가 null이라면 client의 ip가 hosting ip가 될것이므로 client ip return 아니면 server ip return
        }
    }

    private void Awake() // Title Scene으로 들어왔을때 제일 처음 실행.
    {
        manager = GetComponent<NetworkRoomManagerWBTB>(); // NetworkRoomManager를 가져옴.
        _voiceManager = VoiceManager.Instance;  // VoiceManager의 instance불러옴
        for (int i = 0; i < 6; i++)
        {
            PlayerNameList_.Add("");
        }

        errorPanel = GameObject.Find("UICanvas").transform.Find("ErrorPanel").gameObject;
        hostingRoomHideBtn = GameObject.Find("HostingHide");
        searchingRoomHideBtn = GameObject.Find("SerachingHide");
        hostingBtn = GameObject.Find("Hosting").GetComponent<Button>();
        searchingBtn = GameObject.Find("Searching");
        PlayerName = GameObject.Find("InputName").GetComponent<InputField>();

        PlayerName.onEndEdit.AddListener(delegate
        {
            setAndLogin();
        });
        PlayerName.onValueChanged.AddListener(delegate
        {
            hideBtn();
        });
        hostingBtn.onClick.AddListener(delegate
        {
            MakeRoom();
        });
        searchingBtn.transform.Find("InputField").GetComponent<InputField>().onEndEdit.AddListener(delegate
        {
            SearchingRoom();
        });
        hostingRoomHideBtn.GetComponent<Button>().onClick.AddListener(delegate
        {
            RoomHide();
        });
        searchingRoomHideBtn.GetComponent<Button>().onClick.AddListener(delegate
        {
            RoomHide();
        });
    }

    private void Start()
    {
        var inputname = GameObject.Find("InputName").GetComponent<InputField>();
        inputname.onValueChanged.AddListener(delegate
        {
            char[] tempchar = inputname.text.ToCharArray();
            if(Encoding.Default.GetBytes(tempchar).Length > 15)
            {
                inputname.text = inputname.text.Substring(0, inputname.text.Length - 1); 
            }
            SetPlayerName = inputname.text;
        });
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "Title")
        {
            errorPanel = GameObject.Find("UICanvas").transform.Find("ErrorPanel").gameObject;
            hostingRoomHideBtn = GameObject.Find("HostingHide");
            searchingRoomHideBtn = GameObject.Find("SerachingHide");
            hostingBtn = GameObject.Find("Hosting").GetComponent<Button>();
            searchingBtn = GameObject.Find("Searching");
            PlayerName = GameObject.Find("InputName").GetComponent<InputField>();

            PlayerName.onEndEdit.AddListener(delegate
            {
                setAndLogin();
            });
            PlayerName.onValueChanged.AddListener(delegate
            {
                hideBtn();
            });
            hostingBtn.onClick.AddListener(delegate
            {
                MakeRoom();
            });
            searchingBtn.transform.Find("InputField").GetComponent<InputField>().onEndEdit.AddListener(delegate
            {
                SearchingRoom();
            });
            hostingRoomHideBtn.GetComponent<Button>().onClick.AddListener(delegate
            {
                RoomHide();
            });
            searchingRoomHideBtn.GetComponent<Button>().onClick.AddListener(delegate
            {
                RoomHide();
            });
        }
    }

    public void SendLobbyUpdate(NetworkManagerHUDWBTB.MatchStatus status)
    {
        // lobby에 update를 보냄
        var lobbyChannelSession = _voiceManager.ActiveChannels.FirstOrDefault(ac => ac.Channel.Name == _voiceManager.channelName);   // Voice Manager의 Active상태의 Channel중 lobby channel session 불러옴
        foreach (var channel in _voiceManager.ActiveChannels)
        {
            if(channel.Channel.Name == _voiceManager.channelName)
            {
                lobbyChannelSession = channel;
                break;
            }
        }
        if (lobbyChannelSession != null)
        {
            // NB: message의 첫번째 인자는 출력되지 않음 로그의 가독성을 위한것
            _voiceManager.SendTextMessage($"<{_voiceManager.LoginSession.LoginSessionId.DisplayName}:{status}>", lobbyChannelSession.Key, $"MatchStatus:{status}", (status == NetworkManagerHUDWBTB.MatchStatus.Open ? HostingIp : "blank"));
            // text message 출력
        }
        else
        {
            Debug.LogError($"Cannot send MatusStatus.{status}: not joined to {_voiceManager.channelName}");
        }
    }

    private IEnumerator AwaitLobbyRejoin()
    {   // 작업이 너무 자주 반복될 필요가 없는경우 coroutine에 넣어 정기적으로 업데이트
        // join을 기다리고 있는 lobby에 다시 join하는 method
        IChannelSession lobbyChannel = _voiceManager.ActiveChannels.FirstOrDefault(ar => ar.Channel.Name == _voiceManager.channelName);  // lobby channel을 가져옴

        // yield한 지점에서 값을 유지하고 있다가 적절한 상태가 되면 yield한 직후의 프레임에서 재개
        yield return new WaitUntil(() => lobbyChannel == null || (lobbyChannel.AudioState != ConnectionState.Connected && lobbyChannel.TextState != ConnectionState.Connected));
        // lobbyChannel이 null이거나 lobbyChannel의 AudioState와 TextState가 모두 Connected상태가 아닐경우 기다림

        _voiceManager.JoinChannel(_voiceManager.channelName, ChannelType.NonPositional, VoiceManager.ChatCapability.AudioOnly, false);   // lobbyChannel에 join함
    }

    public void LeaveAllChannels()  // 모든 채널의 연결을 끊는 method
    {
        Debug.Log("LeaveAllChannels");
        foreach (var channelSession in _voiceManager.ActiveChannels) // voice Manager의 모든 active상태의 channel을 확인해 disconnect 시켜줌
        {
            if (channelSession.AudioState == ConnectionState.Connected || channelSession.TextState == ConnectionState.Connected)
                // channel의 AudioSatate나 TextSate가 Connected상태인 경우
                channelSession.Disconnect();    // channel을 disconnect
        }
    }

    private void voiceManager_OnParticipantAddedEvent(string username, ChannelId channel, IParticipant participant) // 참가자 추가 event handler
    {
        UnityEngine.Debug.Log("OnParticipantAdded NMH");
        //if (participant.IsSelf) // 참가자 추가 event의 발생 주체가 자신인경우
        //    StartCoroutine(AwaitLobbyRejoin());    // WaitLobbyJoin method 실행
        if (participant.IsSelf && NetworkServer.active)
            // lobby channel에 join하고 match를 hosting하는 경우
            // 잠시 채널을 벗어났을때 요청을 놓쳤을 수 있으므로 업데이트를 보냄
            SendLobbyUpdate(NetworkManagerHUDWBTB.MatchStatus.Open);
    }

    private void voiceManager_OnTextMessageLogReceivedEvent(string sender, IChannelTextMessage channelTextMessage)  // TextMessage를 받는 event handler
    {
        if (channelTextMessage.ApplicationStanzaNamespace.EndsWith(NetworkManagerHUDWBTB.MatchStatus.Seeking.ToString()) && NetworkServer.active)   // 
            SendLobbyUpdate(NetworkManagerHUDWBTB.MatchStatus.Open);
    }

    public override void OnStartClient() //OnStartClient mehtod 재정의
    {
        UnityEngine.Debug.Log("Starting client");
        _voiceManager.OnParticipantAddedEvent += voiceManager_OnParticipantAddedEvent;  // 참가자 추가 event 구독
        _voiceManager.OnTextMessageLogReceivedEvent += voiceManager_OnTextMessageLogReceivedEvent;  // message 수신 event 구독
        base.OnStartClient();   // 원래의 mehtod 실행
    }

    public override void OnStopClient() //OnStopClient mehtod 재정의
    {
        UnityEngine.Debug.Log("Stopping client");
        _voiceManager.OnParticipantAddedEvent -= voiceManager_OnParticipantAddedEvent;  // 참가자 추가 event 구독 해지
        _voiceManager.OnTextMessageLogReceivedEvent -= voiceManager_OnTextMessageLogReceivedEvent;  // message 수신 event 구독 해지
        LeaveAllChannels(); // 모든 채널 disconnect
        base.OnStopClient();    // 원래의 mehtod 실행
    }

    private void LoginToVivox(string playerName) // vivox에 login
    {
        //UnityEngine.Debug.Log("LoginToVivox");
        _voiceManager.Login(playerName);  //display되는 이름 설정할 수 있을듯
    }

    private void LoginToVivoxService(string playerName)  // vivox service(마이크 사용)에 관한 허가 받고 login
    {
        //UnityEngine.Debug.Log("LoginToVivoxService");
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {   // user가 마이크 사용을 허가 했다면
            LoginToVivox(playerName); //vivox에 login 시도
        }
        else
        {
            // user가 이미 permission deny를 했는지 확인
            if (PermissionsDenied)  // permission deny를 안했다면
            {
                PermissionsDenied = false;
                LoginToVivoxService(playerName); //vivox에 login 시도
            }
            else  // permission deny를 했다면
            {
                PermissionsDenied = true;
                // 마이크를 사용하기 위한 permission을 갖지 못함
                // permission을 허가해달라고 request
                Permission.RequestUserPermission(Permission.Microphone);
            }
        }
    }
    public void setAndLogin()
    {
        if (SetPlayerName != null)
        {
            _voiceManager.LogOut();
        }
        //SetPlayerName = GameObject.Find("InputName").GetComponent<InputField>().text;
        if (SetPlayerName != "")
        {
            hostingRoomHideBtn.SetActive(false);
            searchingRoomHideBtn.SetActive(false);
            LoginToVivoxService(SetPlayerName);  // vivox service에 login
        }
    }

    public void RoomHide()
    {
        errorPanel.SetActive(true); // 에러 패널을 띄우고,
        GameObject.Find("ErrorMessage").GetComponent<Text>().text = "플레이어 이름을 설정해주세요!!"; // 에러 메세지를 표시.
    }

    public void hideBtn()
    {
        hostingRoomHideBtn.SetActive(true);
        searchingRoomHideBtn.SetActive(true);
    }

    public void MakeRoom() // 방만들기 버튼 UI를 누를 경우
    {
        //UnityEngine.Debug.Log("MakeRoom");
        // Client로서 서버에 잘 연결돼있거나(앞의 두 조건: 2중 체크), 이미 본인이 서버를 호스팅하고 있으면,
        if (NetworkClient.active || NetworkClient.isConnected || NetworkServer.active) return; // 아무것도 안함.

        CreatePortMapping.Create();
        //_voiceManager.OnParticipantAddedEvent += voiceManager_OnParticipantAddedEvent;  // 참가자 추가 event 구독
        //_voiceManager.OnTextMessageLogReceivedEvent += voiceManager_OnTextMessageLogReceivedEvent;  // message 수신 event 구독
        //SetPlayerName = GameObject.Find("InputName").GetComponent<InputField>().text;

        //LoginToVivoxService(SetPlayerName);  // vivox service에 login
        manager.networkAddress = new WebClient().DownloadString("https://api.ipify.org");
        //this.gameObject.GetComponent<TelepathyTransport>().port = 
        manager.StartHost(); // 위와 같은 상황이 아니면, 서버를 호스팅.

        // 호스팅: 본인의 IP로 서버를 만들고, Client로서 이 서버에 참여하게 됨.
        if (NetworkClient.isConnected && !ClientScene.ready) // Client로서 서버에 연결은 됐는데, Client에 게임을 로드할 준비까지는 안됐었다면,
        {
            ClientScene.Ready(NetworkClient.connection); // 해당 준비를 마치고,
            if (ClientScene.localPlayer == null) // 플레이어로 서버에 등록이 안됐다면,
            {
                ClientScene.AddPlayer(NetworkClient.connection); // 서버에 플레이어로 등록한다.
            }
        }
    }

    

    public void SearchingRoom() // 방참여 버튼 UI를 눌러 IP 입력 후 Enter를 누르면,
    {
        UnityEngine.Debug.Log("SearchingRoom");
        // 위와 같음.
            if (NetworkClient.active || NetworkClient.isConnected || NetworkServer.active) return;
        _voiceManager.setChannelName();
            //SetPlayerName = GameObject.Find("InputName").GetComponent<InputField>().text;
            //LoginToVivoxService(SetPlayerName);  // vivox service에 login
        //var InputNetworkAddress = GameObject.Find("InputAddress").GetComponent<Text>().text;
        //int parseNetworkAddress0 = int.Parse(InputNetworkAddress.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        //int parseNetworkAddress1 = int.Parse(InputNetworkAddress.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        //int parseNetworkAddress2 = int.Parse(InputNetworkAddress.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        //int parseNetworkAddress3 = int.Parse(InputNetworkAddress.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        //int parsePort = int.Parse(InputNetworkAddress.Substring(8, 2), System.Globalization.NumberStyles.HexNumber);
        //manager.networkAddress = $"{parseNetworkAddress0}.{parseNetworkAddress1}.{parseNetworkAddress2}.{parseNetworkAddress3}";
        manager.networkAddress = GameObject.Find("InputAddress").GetComponent<Text>().text;
            //_voiceManager.OnParticipantAddedEvent += voiceManager_OnParticipantAddedEvent;  // 참가자 추가 event 구독
            //_voiceManager.OnTextMessageLogReceivedEvent += voiceManager_OnTextMessageLogReceivedEvent;  // message 수신 event 구독
        manager.StartClient(); // 위와 같은 상황이 아니면, 입력한 IP 서버에 Client로 참여.
    }
    
    public void StopButtons() // Lobby에서 뒤로가기 버튼 UI를 누를 경우,
    {
        //UnityEngine.Debug.Log("StopButton");
        // 호스트중이라면(본인의 IP로 서버를 만들고 있고, Client로서 이 서버에 참여하고 있으면),
        //_voiceManager.OnParticipantAddedEvent -= voiceManager_OnParticipantAddedEvent;  // 참가자 추가 event 구독 해지
        //_voiceManager.OnTextMessageLogReceivedEvent -= voiceManager_OnTextMessageLogReceivedEvent;  // message 수신 event 구독 해지
        //LeaveAllChannels(); // 모든 채널 disconnect
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            //Process foo = new Process();
            //foo.StartInfo.FileName = Environment.CurrentDirectory + "\\PortForwardingDeletion\\Open.Nat.ConsoleTest.exe";
            //foo.Start();
            DeletePortMapping.Delete();
            manager.StopHost(); // 호스팅을 멈추고 Title Scene으로 넘어감.
        }
        
        else if (NetworkClient.isConnected) // Client로서 서버에 연결중이라면,
        {
            manager.StopClient(); // Client로서 서버 연결을 끊고, Title Scene으로 넘어감.
        }
    }

    private void OnApplicationQuit()
    {
        DeletePortMapping.Delete();
    }
}
