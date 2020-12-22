using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using VivoxUnity;
using UnityEngine.SceneManagement;

public class NetworkRoomPlayerWBTB : NetworkRoomPlayer // 플레이어의 행동을 서버와 동기화하는 스크립트.
{
    [SerializeField] GameObject playernameP, readystateP; // 플레이어 이름과 레디 상태 UI.
    [SerializeField] GameObject banP; // 밴 버튼 UI.
    [SerializeField] Text playername, readystate; // 플레이어 이름 텍스트와 레디 상태 텍스트.
    RectTransform p1; // 첫번째 플레이어 UI의 위치.
    [SerializeField] GameObject GameStartBtn;
    UIfunc uifunc;

    NetworkRoomManagerWBTB roomManager;
    public SyncListString playerNameList;
    public SyncListInt roomslots;
    NetworkManagerHUDWBTB hUD;

    [SerializeField] Animator voiceStateAnimator;   
    //public Animator discussionSpeakingAnimator;
    //public GameObject voteCanvas;
    public VoiceManager _voiceManager;
    public voiceLogin _voiceLogin;
    public IParticipant participant;

    public GameObject LobbySoundManager;

    [SyncVar(hook = "OnIsMutedChanged")]
    private bool IsMuted = true;
    public bool isMuted
    {
        get { return IsMuted; }
        set
        {
            _voiceManager.AudioInputDevices.Muted = value;
            if (isServer)
            {
                IsMuted = value;
                voiceStateAnimator.SetBool("IsMuted", IsMuted);
            }
            else
            {
                CmdSetIsMutedValue(value);
            }
        }
    }

    void OnIsMutedChanged(bool _, bool IsMuted)
    {
        //Debug.Log("set IsMuted");
        voiceStateAnimator.SetBool("IsMuted", IsMuted);
    }

    [SyncVar(hook = "OnIsSpeakingChanged")]
    private bool IsSpeaking;
    public bool isSpeaking
    {
        get { return IsSpeaking; }
        set
        {
            if (voiceStateAnimator && !isMuted)
            {
                if (isServer)
                {
                    IsSpeaking = value;
                    //Debug.Log("set IsSpeaking");
                    voiceStateAnimator.SetBool("IsSpeaking", IsSpeaking);
                }
                else
                {
                    CmdSetIsSpeakingValue(value);
                }
            }
        }
    }

    void OnIsSpeakingChanged(bool _, bool IsSpeaking)
    {
        //Debug.Log("set IsSpeaking");
        voiceStateAnimator.SetBool("IsSpeaking", IsSpeaking);
        //if(discussionSpeakingAnimator != null)
        //{
        //    Debug.Log("discussion changed");
        //    discussionSpeakingAnimator.SetBool("isSpeaking", IsSpeaking);
        //}
    }


    private void Awake() // Lobby Scene으로 들어왔을때 제일 처음 실행.
    {
        //Debug.Log("room player awake");
        p1 = GameObject.Find("Player1Position").GetComponent<RectTransform>(); // 첫번째 플레이어 UI의 위치를 가져옴.
        _voiceManager = VoiceManager.Instance;
        _voiceLogin = GameObject.Find("voiceLogin").GetComponent<voiceLogin>();
        LobbySoundManager = GameObject.Find("LocalSoundManager");
        //playerNameList.Add(GameObject.Find("NetworkRoomManager").GetComponent<NetworkManagerHUDWBTB>().SetPlayerName);
        //CmdSetPlayerName(GameObject.Find("NetworkRoomManager").GetComponent<NetworkManagerHUDWBTB>().SetPlayerName);

        _voiceManager.OnParticipantAddedEvent += OnParticipantAdded;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        var RoomManagerObj = GameObject.Find("NetworkRoomManager");
        hUD = RoomManagerObj.GetComponent<NetworkManagerHUDWBTB>();
        roomManager = RoomManagerObj.GetComponent<NetworkRoomManagerWBTB>();
        //CmdSetPlayerName(hUD.SetPlayerName);
        //CmdSyncIndex();
        StartCoroutine(UpdatePlayerNameList());
        //CmdSyncIndex();
        //LobbySoundManager = GameObject.Find("LocalSoundManager");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        var RoomManagerObj = GameObject.Find("NetworkRoomManager");
        hUD = RoomManagerObj.GetComponent<NetworkManagerHUDWBTB>();
        roomManager = RoomManagerObj.GetComponent<NetworkRoomManagerWBTB>();
        for(int i = 0; i < 6; i++)
        {
            roomslots.Add(0);
        }
        //playerNameList[index] = hUD.SetPlayerName;
        //LobbySoundManager = GameObject.Find("LocalSoundManager");
        uifunc = GameObject.Find("UIfunction").GetComponent<UIfunc>();
        GameStartBtn.GetComponent<Button>().onClick.AddListener(uifunc.GameStartButton);
        if (isServer)
        {
            StartCoroutine(CreateMappingInvoke());
        }
    }

    public override void OnClientEnterRoom()
    {
        base.OnClientEnterRoom();
        //CmdSyncIndex();
        CmdSetPlayerName(hUD.SetPlayerName);
    }

    IEnumerator CreateMappingInvoke()
    {
        while(true)
        {
            yield return new WaitForSeconds(5);
            CreatePortMapping.Create();
            if (!(SceneManager.GetActiveScene().name == "Lobby"))
                break;
        }
    }

    IEnumerator UpdatePlayerNameList()
    {
        while (true)
        {
            yield return new WaitForSeconds((float)0.1);
            if(isClient && hasAuthority)
            {
                CmdSetPlayerName(hUD.SetPlayerName);
                CmdSyncIndex();
            }
                
            if (!(SceneManager.GetActiveScene().name == "Lobby"))
                break;
        }

    }

    void OnParticipantAdded(string userName, ChannelId channel, IParticipant participant)
    {
        //Debug.Log("OnParticipantAdded : " + userName);
        if (participant.IsSelf) {
            _voiceLogin.PlayerIndex = index;
            this.participant = participant;
            setupParticipant(participant);
        }
    }

    private void OnEnable()
    {
        if (participant != null && participant.IsSelf)
        {
            _voiceLogin.PlayerIndex = index;
            setupParticipant(participant);
        }
    }

    private void OnDestroy()
    {
        _voiceManager.OnParticipantAddedEvent -= OnParticipantAdded;
        //SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void setupParticipant(IParticipant Participant)
    {
        isMuted = _voiceManager.AudioInputDevices.Muted;
        isSpeaking = Participant.SpeechDetected;
        Participant.PropertyChanged += (obj, args) =>
        {
            switch (args.PropertyName)
            {
                case "SpeechDetected":
                    isSpeaking = Participant.SpeechDetected;
                    break;
            }
        };
    }

    [Command]
    void CmdSetIsMutedValue(bool value)
    {
        IsMuted = value;
    }

    [Command]
    void CmdSetIsSpeakingValue(bool value)
    {
        IsSpeaking = value;
    }

    private void Update() // 상시 실행.
    {
        // Lobby에서 연결이 끊기면 위의 Component들이 null상태로 돌아감.
        // 이 때 아래와 같이 변수를 할당하거나 참조하려고 하면 오류가 발생.
        // 따라서 이를 막기 위한 조건문.
        if (p1 != null &&
            playernameP != null &&
            readystateP != null &&
            banP != null &&
            playername != null &&
            readystate != null &&
            p1 != null)
        {
            playername.text = hUD.PlayerNameList_[index]; // 플레이어 이름을 설정: p0, p1, p2 ...
            // 플레이어 이름, 레디 상태, 밴 버튼 UI들의 위치를 첫번째 플레이어 UI의 위치를 이용하여 배치.
            playernameP.GetComponent<RectTransform>().anchoredPosition = new Vector2(p1.anchoredPosition.x, p1.anchoredPosition.y - (index * 100));
            readystateP.GetComponent<RectTransform>().anchoredPosition = new Vector2(p1.anchoredPosition.x + 275, p1.anchoredPosition.y - (index * 100));
            banP.GetComponent<RectTransform>().anchoredPosition = new Vector2(p1.anchoredPosition.x + 450, p1.anchoredPosition.y - (index * 100));
        }

        if (readyToBegin) // 레디가 됐다면,
            readystate.text = "Ready"; // 레디 상태 UI의 텍스트를 Ready로 변경.
        else // 레디가 안됐다면,
            readystate.text = "Not Ready"; // 레디 상태 UI의 텍스트를 Not Ready로 변경.

        if (NetworkManager.IsSceneActive("Lobby")) // 현재 Scene이 Lobby라면,
        {
            // 플레이어 이름, 레디 상태 UI를 활성화.
            playernameP.SetActive(true);
            readystateP.SetActive(true);
            if (isServer) // 호스트 플레이어라면,
            {
                banP.SetActive(true); // 밴 버튼 UI도 활성화.
                GameStartBtn.SetActive(true);
            }
        }

        else // 현재 Scene이 Lobby가 아니라면,
        {
            // 플레이어 이름, 레디 상태 UI를 비활성화.
            playernameP.SetActive(false);
            readystateP.SetActive(false);
            if(isServer) // 호스트 플레이어라면,
            {
                banP.SetActive(false); // 밴 버튼 UI도 비활성화.
                GameStartBtn.SetActive(false);
            }
        }
    }
    
    public void ReadyButton() // 레디 상태 UI를 클릭하면,
    {
        if (NetworkClient.active && isLocalPlayer) // Client가 서버에 잘 연결돼있고, 그리고 레디 버튼을 누른게 해당 플레이어 본인일 경우,
        {
            if (readyToBegin) // 레디 상태였다면,
            {
                    CmdChangeReadyState(false); // Not Ready 상태로 바꾸길 서버에 요청.
            }
            else // 레디 상태가 아니었다면,
            {
                    CmdChangeReadyState(true); // Ready 상태로 바꾸길 서버에 요청.
            }
        }
        LobbySoundManager.GetComponent<LobbySoundPlay>().roomPlayerReadyBtn.Invoke();
    }

    public void BanButton() // 밴 버튼 UI를 클릭할 경우,
    {
        if ((isServer && index > 0) || isServerOnly) // 호스트 플레이어라면,
        {
            GetComponent<NetworkIdentity>().connectionToClient.Disconnect(); // 해당 Client의 연결을 끊음.
        }
        LobbySoundManager.GetComponent<LobbySoundPlay>().roomPlayerBtn.Invoke();
    }

    [Command]
    public void CmdSetPlayerName(string name)
    {
        //Manager.PlayerNameList_[index] = name;
        RpcSetPlayerName(name);
    }

    [ClientRpc]
    public void RpcSetPlayerName(string name)
    {
        hUD.PlayerNameList_[index] = name;
    }

    [Command]
    void CmdSyncIndex()
    {
        //RpcSyncIndex();
        int tmp = 0;
        foreach(NetworkRoomPlayer p in roomManager.roomSlots)
        {
            roomslots[tmp] = (int)p.netId;
            tmp++;
        }
        for(int i = tmp; i < 6; i++)
        {
            roomslots[i] = 0;
        }
        RpcSyncIndex();
        
    }

    [ClientRpc]
    void RpcSyncIndex()
    {
        SyncIndex();
    }

    void SyncIndex()
    {
        var allplayer = GameObject.FindGameObjectsWithTag("PlayerList");

        int tmp = 0;
        foreach(int i in roomslots)
        {
            for(int j = 0; j < allplayer.Length; j++)
            {
                if (allplayer[j].GetComponent<NetworkRoomPlayerWBTB>().netId == i)
                {
                    allplayer[j].GetComponent<NetworkRoomPlayerWBTB>().index = tmp;
                    break;
                }
            }
            tmp++;
        }
    }

}
