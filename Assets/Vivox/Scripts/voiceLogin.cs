using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using VivoxUnity;
using System.Linq;
using UnityEngine.UI;

public class voiceLogin : MonoBehaviour
{
    VoiceManager _voiceManager;
    GameObject _networkroommanager;
    NetworkManagerHUDWBTB _voiceNetworkManager;

    GameObject[] player;
    NetworkRoomPlayerWBTB[] _player;

    public IParticipant Participant;

    public Button voiceOnOffBtn;
    public Image voiceOnOffImg;
    public Sprite voiceOnImg;
    public Sprite voiceOffImg;
    public int PlayerIndex = -1;

    public Canvas textChatCanvas;


    //public static voiceLogin Instance = null;

    private void Awake() // 각 Scene에 들어왔을때 가장 먼저 실행.
    {
        //Debug.Log("voiceUI Awake");
        //싱글턴 패턴 object로 만들어줌
        //if (Instance == null)
        //    Instance = this;
        //else if (Instance != this)
        //    Destroy(gameObject);
        //DontDestroyOnLoad(gameObject);

        OnEnable();

        _voiceManager = VoiceManager.Instance;
        _networkroommanager = GameObject.Find("NetworkRoomManager");
        _voiceNetworkManager = _networkroommanager.GetComponent<NetworkManagerHUDWBTB>();

        _voiceManager.OnUserLoggedInEvent += OnUserLoggedIn;    // user login event 구독
        _voiceManager.OnUserLoggedOutEvent += OnUserLoggedOut;  // user logout event 구독

        if (_voiceManager.loginState == LoginState.LoggedIn)    // login state가 loggedin인 경우
            OnUserLoggedIn();
        else
            OnUserLoggedOut();

        _voiceManager.AudioInputDevices.Muted = true;
        _voiceManager.setEchoCancelation(true);
        StartCoroutine(getPlayer());
    }
    private void Update()
    {
        if (_voiceManager.AudioInputDevices.Muted)
            voiceOnOffImg.sprite = voiceOffImg;
        else
            voiceOnOffImg.sprite = voiceOnImg;
    }

    IEnumerator getPlayer()
    {
        yield return new WaitUntil(() => PlayerIndex != -1 && _player != null);
        Debug.Log("getPlayer");
        foreach (var p in _player)
        {
            if (p.isLocalPlayer)
            {
                voiceOnOffBtn.onClick.AddListener(() => voiceChatBtnClicked(p));
                break;
            }
        }
    }


    public void voiceChatBtnClicked(NetworkRoomPlayerWBTB Player)
    {
        //Debug.Log("voiceChatBtnClicked");
        // local input device를 unmute시킴
        Player.isMuted = !Player.isMuted;
    }

    public void textChatBtnClicked()
    {
        textChatCanvas.sortingOrder = 5;
    }

    public void textChatCloseBtnClicked()
    {
        textChatCanvas.sortingOrder = 0;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Title")
        {
            LogoutOfVivoxService();
            //Destroy(gameObject);
        }
        else if(scene.name == "InGame")
        {
            _voiceManager.AudioInputDevices.Muted = true;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        //StartCoroutine(getPlayer());
        player = GameObject.FindGameObjectsWithTag("PlayerList");
        if (_player == null)
        {
            _player = new NetworkRoomPlayerWBTB[player.Length];
            int i = 0;
            foreach (var p in player)
            {
                _player[i] = p.GetComponent<NetworkRoomPlayerWBTB>();
                i++;
            }
        }
    }

    private void OnDestroy()
    {
        Debug.Log("voiceLogin ondestroy");
        voiceOnOffBtn.onClick.RemoveAllListeners();
        _voiceManager.OnUserLoggedInEvent -= OnUserLoggedIn;
        _voiceManager.OnUserLoggedInEvent -= OnUserLoggedOut;
        _voiceManager.OnParticipantAddedEvent -= VoiceManager_OnParticipantAddedEvent;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void JoinGameChannel()
    {
        //Debug.Log("JoinLobbyChannel");
        // participant add event가 알아서 할것
        _voiceManager.OnParticipantAddedEvent += VoiceManager_OnParticipantAddedEvent;  // 참가자 추가 event 구독
        _voiceManager.JoinChannel(_voiceManager.channelName, ChannelType.NonPositional, VoiceManager.ChatCapability.TextAndAudio);   // channel에 join
    }

    public void LogoutOfVivoxService() // vivox service 에서 log out
    {
        //Debug.Log("LogoutOfVivoxService");
        _voiceManager.DisconnectAllChannels();

        _voiceManager.LogOut();
    }

    private void VoiceManager_OnParticipantAddedEvent(string username, ChannelId channel, IParticipant participant)
    {
        Debug.Log("OnParticipantAdded voiceLogin");
        if (channel.Name == _voiceManager.channelName && participant.IsSelf)
            // lobby 채널에 join 했고 host가 아니면 호스트에게 초대를 요청해야함
            _voiceNetworkManager.SendLobbyUpdate(NetworkManagerHUDWBTB.MatchStatus.Seeking);

        player = GameObject.FindGameObjectsWithTag("PlayerList");
        _player = new NetworkRoomPlayerWBTB[player.Length];
        int i = 0;
        foreach (var p in player)
        {
            _player[i] = p.GetComponent<NetworkRoomPlayerWBTB>();
            i++;
        }
    }

    private void OnUserLoggedIn()
    {
        //Debug.Log("OnUserLoggedIn voiceUI");
        var lobbychannel = _voiceManager.ActiveChannels.FirstOrDefault(ac => ac.Channel.Name == _voiceManager.channelName);
        foreach(var channel in _voiceManager.ActiveChannels)
        {
            if (channel.Channel.Name == _voiceManager.channelName)
            {
                lobbychannel = channel;
                break;
            }

        }

        if ((_voiceManager && _voiceManager.ActiveChannels.Count == 0) || lobbychannel == null)
            JoinGameChannel();
        else
        {
            if (lobbychannel.AudioState == ConnectionState.Disconnected)
            {
                // host에게 나는 이미 채널에 들어가려고 하고 있으나 added가 trigger되지 않는다고 요청
                _voiceNetworkManager.SendLobbyUpdate(NetworkManagerHUDWBTB.MatchStatus.Seeking);

                lobbychannel.BeginSetAudioConnected(true, true, ar =>
                {
                    Debug.Log("Now transmitting into lobby channel");
                });
            }
        }
    }

    private void OnUserLoggedOut()
    {
        //Debug.Log("OnUserLoggedOut voiceUI");
        _voiceManager.DisconnectAllChannels();
    }

    public void StopButtons()
    {
        //Debug.Log("StopButton voiceUI");
        LogoutOfVivoxService();
    }

}
