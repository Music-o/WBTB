using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Mirror;

public class ControlPanel : NetworkBehaviour
{
    public GameManager GM;
    
    public GameObject UICanvas;
    public GameObject LogCanvas;
    public GameObject PlayerActionCanvas;
    public GameObject BuildingStatusCanvas;
    public GameObject ReturnInGameCanvas;
    public GameObject InvestCanvas;
    public GameObject VoteCanvas;

    public NetworkRoomPlayerWBTB roomPlayer;
    public Image voiceOnOffImg;
    public Sprite voiceOnImg;
    public Sprite voiceOffImg;
    bool voiceOnOffClicked = false;

    public GameObject ActionBtn;
    public Text PlayerActionTimer;
    public Text NeedPointText, MinPointText, ScorePointText, BuildingNameText;

    public GameObject building, Info, Dust, Hammer;
    public GameObject[] Subbuilding, Fire, character;

    VoiceManager _voiceManager;

    GameObject instance_PlayerActionCanvas, instance_InvestCanvas, instance_VoteCanvas;
    GameObject[] Player;

    public List<int> list;
    ////public UnityAction<int> VoteAction;
    //public delegate void VoteAction();
    //public VoteAction voteAction;

    public InGameSoundPlay InGameSoundManager;

    bool isPlayerAction;
    public void ShowUICanvas() { UICanvas.GetComponent<Canvas>().sortingLayerName = "Canvas"; }

    public void HideUICanvas() { UICanvas.GetComponent<Canvas>().sortingLayerName = "Default"; }

    public void ShowLogCanvas() 
    {
        LogCanvas.GetComponent<Canvas>().sortingLayerName = "LogCanvas";
        HideUICanvas();
    }

    public void HideLogCanvas()
    {
        LogCanvas.GetComponent<Canvas>().sortingLayerName = "Default";
        ShowUICanvas();
    }

    public void ShowPlayerActionCanvas(BuildingStatus buildingStatus)
    {
        isPlayerAction = true;
        Camera.main.GetComponent<CameraMove>().ReturnToInGame();
        HideReturnInGameCanvas();
        HideStatusCanvas();
        HideLogCanvas();
        HideUICanvas();
        instance_PlayerActionCanvas = Instantiate(PlayerActionCanvas, new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 10), transform.rotation);
        instance_PlayerActionCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        instance_PlayerActionCanvas.GetComponent<Canvas>().worldCamera = Camera.main.GetComponent<Camera>();
        instance_PlayerActionCanvas.GetComponent<Canvas>().sortingLayerName = "Canvas";

        GameObject panel = instance_PlayerActionCanvas.transform.Find("Panel").gameObject;
        GameObject InfoBtn = panel.transform.Find("BuildingInfoOnButton").gameObject;
        InfoBtn.GetComponent<Button>().onClick.AddListener(delegate { GM.gameObject.GetComponent<ControlPanel>().ShowStatusCanvas(buildingStatus); });
        ActionBtn = GameObject.Find("ActionText");
        GameObject BuildingImage = ActionBtn.transform.Find("BuildingImage").gameObject;
        GameObject building = BuildingImage.transform.Find("Building").gameObject;
        GameObject[] Subbuilding = new GameObject[2];
        GameObject[] Fire = new GameObject[3];

        building.GetComponent<SpriteRenderer>().sprite = buildingStatus.Building.GetComponent<SpriteRenderer>().sprite;
        building.transform.localScale = buildingStatus.Building.transform.localScale;

        for (int i = 0; i < Subbuilding.Length; i++)
        {
            Subbuilding[i] = BuildingImage.transform.Find("SubBuilding (" + (i + 1).ToString() + ")").gameObject;
            Subbuilding[i].SetActive(buildingStatus.SubBuilding[i].activeSelf);
            Subbuilding[i].GetComponent<SpriteRenderer>().sprite = buildingStatus.SubBuilding[i].GetComponent<SpriteRenderer>().sprite;
            Subbuilding[i].transform.localPosition = buildingStatus.SubBuilding[i].transform.localPosition;
        }

        for (int i = 0; i < Fire.Length; i++)
        {
            Fire[i] = BuildingImage.transform.Find("Fire (" + (i + 1).ToString() + ")").gameObject;
            Fire[i].SetActive(buildingStatus.Fire[i].activeSelf);
        }

        if (buildingStatus.building.getState() == State.Idle)
        {
            ActionBtn.GetComponent<Text>().text = "건물 건설";
        }

        else if (buildingStatus.building.getState() == State.Damaged)
        {
            ActionBtn.GetComponent<Text>().text = "건물 수리";
        }

        else GameObject.Find("Build/RepairBtn").SetActive(false);
    }

    public void HidePlayerActionCanvas() 
    {
        isPlayerAction = false;
        Destroy(instance_PlayerActionCanvas);
        ShowUICanvas();
    }

    public void ShowStatusCanvas(BuildingStatus buildingStatus)
    {
        for (int i = 0; i < character.Length; i++)
            character[i].GetComponent<Image>().sprite = buildingStatus.sprite[4];

        building.GetComponent<SpriteRenderer>().sprite = buildingStatus.Building.GetComponent<SpriteRenderer>().sprite;
        building.GetComponent<SpriteRenderer>().color = buildingStatus.Building.GetComponent<SpriteRenderer>().color;
        building.transform.localScale = buildingStatus.Building.transform.localScale;
        building.GetComponent<SpriteRenderer>().sortingLayerName = "CanvasBuilding";

        for (int i = 0; i < Subbuilding.Length; i++) 
        {
            Subbuilding[i].SetActive(buildingStatus.SubBuilding[i].activeSelf);
            Subbuilding[i].GetComponent<SpriteRenderer>().sprite = buildingStatus.SubBuilding[i].GetComponent<SpriteRenderer>().sprite;
            Subbuilding[i].GetComponent<SpriteRenderer>().color = buildingStatus.SubBuilding[i].GetComponent<SpriteRenderer>().color;
            Subbuilding[i].transform.localPosition = buildingStatus.SubBuilding[i].transform.localPosition;
            Subbuilding[i].GetComponent<SpriteRenderer>().sortingLayerName = "CanvasBuilding";
        }

        Info.SetActive(buildingStatus.INFO.activeSelf);
        Info.GetComponent<SpriteRenderer>().sortingLayerName = "CanvasBuilding";
        Dust.SetActive(buildingStatus.Dust.activeSelf);
        Dust.GetComponent<SpriteRenderer>().sortingLayerName = "CanvasBuilding";

        for (int i = 0; i < Fire.Length; i++)
        {
            Fire[i].SetActive(buildingStatus.Fire[i].activeSelf);
            Fire[i].GetComponent<SpriteRenderer>().sortingLayerName = "CanvasBuilding";
        }
            

        if (buildingStatus.Hammer.activeSelf)
        {
            Hammer.SetActive(true);
            Hammer.GetComponent<SpriteRenderer>().sortingLayerName = "CanvasBuilding";
            MoveHammer();
        }
        else 
        {
            OffHammer();
        }

        NeedPointText.text = buildingStatus.building.getNeedPopulation().ToString();
        MinPointText.text = buildingStatus.building.getMinPopulation().ToString();
        ScorePointText.text = buildingStatus.building.getScore().ToString();
        BuildingNameText.text = buildingStatus.gameObject.name;

        list = buildingStatus.building.getNeedPlayerList();

        for (int i = 0; i < list.Count; i++) 
        {
            character[i].GetComponent<Image>().sprite = GM.Character[list[i]];    
        }

        BuildingStatusCanvas.GetComponent<Canvas>().sortingLayerName = "Canvas";
        
        if(ReturnInGameCanvas.activeSelf)
            ReturnInGameCanvas.GetComponent<Canvas>().sortingLayerName = "Default";
     
        HideUICanvas();
    }

    void MoveHammer() { Invoke("RotateCW", 1); }

    void RotateCW()
    {
        Hammer.transform.rotation = Quaternion.Euler(0, 0, -45);
        Invoke("RotateCCW", 1);
    }

    void RotateCCW()
    {
        Hammer.transform.rotation = Quaternion.Euler(0, 0, 0);
        Invoke("RotateCW", 1);
    }

    void OffHammer()
    {
        CancelInvoke("RotateCW");
        CancelInvoke("RotateCCW");
        Hammer.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        Hammer.SetActive(false);
    }

    public void HideStatusCanvas()
    {
        BuildingStatusCanvas.GetComponent<Canvas>().sortingLayerName = "Default";
        building.GetComponent<SpriteRenderer>().sortingLayerName = "Default";
        for(int i = 0; i < Subbuilding.Length; i++)
            Subbuilding[i].GetComponent<SpriteRenderer>().sortingLayerName = "Default";
        for(int i = 0; i < Fire.Length;i++)
            Fire[i].GetComponent<SpriteRenderer>().sortingLayerName = "Default";
        Hammer.GetComponent<SpriteRenderer>().sortingLayerName = "Default";
        Dust.GetComponent<SpriteRenderer>().sortingLayerName = "Default";
        Info.GetComponent<SpriteRenderer>().sortingLayerName = "Default";

        if (ReturnInGameCanvas.activeSelf)
            ReturnInGameCanvas.GetComponent<Canvas>().sortingLayerName = "Canvas";
        else
            if(!isPlayerAction)
                ShowUICanvas();
    }

    public void ShowReturnInGameCanvas() 
    {
        HideUICanvas();
        ReturnInGameCanvas.SetActive(true); 
    }

    public void HideReturnInGameCanvas() 
    {
        ReturnInGameCanvas.SetActive(false);
        ShowUICanvas();
    }

    public void ShowInvestCanvas(BuildingStatus buildingStatus) 
    {
        Camera.main.GetComponent<CameraMove>().ReturnToInGame();
        HideReturnInGameCanvas();
        HideStatusCanvas();
        HideLogCanvas();
        HideUICanvas();
        instance_InvestCanvas = Instantiate(InvestCanvas, new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 10), transform.rotation);
        instance_InvestCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        instance_InvestCanvas.GetComponent<Canvas>().worldCamera = Camera.main.GetComponent<Camera>();
        instance_InvestCanvas.GetComponent<Canvas>().sortingLayerName = "Canvas";
        Player = GameObject.FindGameObjectsWithTag("Player");

       
        foreach (GameObject p in Player)
        {
            p.GetComponent<PlayerAction>().TimerReset();
            p.GetComponent<PlayerAction>().timerPhase = TimerPhase.InvestTime;
            p.GetComponent<PlayerAction>().TimerStart();

            GameObject ActivePanel = instance_InvestCanvas.transform.Find("ActivePanel").gameObject;
            GameObject WaitingPanel = instance_InvestCanvas.transform.Find("WaitingPanel").gameObject;
            GameObject PlayersPanel = ActivePanel.transform.Find("PlayersPanel").gameObject;
            GameObject BuildingInfoPanel = ActivePanel.transform.Find("BuildingInfoPanel").gameObject;

            var BuildingName = BuildingInfoPanel.transform.Find("BuildingName").GetComponent<Text>();
            var NeedPoint = BuildingInfoPanel.transform.Find("NeedPoint").GetComponent<Text>();
            var MinPoint = BuildingInfoPanel.transform.Find("MinPoint").GetComponent<Text>();
            var ScorePoint = BuildingInfoPanel.transform.Find("ScorePoint").GetComponent<Text>();
            GameObject[] images = new GameObject[6];
            GameObject BuildingImage = BuildingInfoPanel.transform.Find("BuildingImage").gameObject;
            GameObject[] Subbuilding = new GameObject[2];
            GameObject building = BuildingImage.transform.Find("Building").gameObject;

            GameObject[] Fire = new GameObject[3];

            building.GetComponent<SpriteRenderer>().sprite = buildingStatus.Building.GetComponent<SpriteRenderer>().sprite;
            building.transform.localScale = buildingStatus.Building.transform.localScale;

            for (int i = 0; i < Subbuilding.Length; i++)
            {
                Subbuilding[i] = BuildingImage.transform.Find("SubBuilding (" + (i + 1).ToString() + ")").gameObject;
                Subbuilding[i].SetActive(buildingStatus.SubBuilding[i].activeSelf);
                Subbuilding[i].GetComponent<SpriteRenderer>().sprite = buildingStatus.SubBuilding[i].GetComponent<SpriteRenderer>().sprite;
                Subbuilding[i].transform.localPosition = buildingStatus.SubBuilding[i].transform.localPosition;
            }

            for (int i = 0; i < Fire.Length; i++)
            {
                Fire[i] = BuildingImage.transform.Find("Fire (" + (i + 1).ToString() + ")").gameObject;
                Fire[i].SetActive(buildingStatus.Fire[i].activeSelf);
            }

            BuildingName.text = buildingStatus.gameObject.name;
            NeedPoint.text = buildingStatus.building.getNeedPopulation().ToString();
            MinPoint.text = buildingStatus.building.getMinPopulation().ToString();
            ScorePoint.text = buildingStatus.building.getScore().ToString();

            list = buildingStatus.building.getNeedPlayerList();

            for (int i = 0; i < images.Length; i++)
                images[i] = PlayersPanel.transform.Find("Panel").Find("Image (" + (i + 1).ToString() + ")").gameObject;

            for (int i = 0; i < list.Count; i++)
                images[i].GetComponent<Image>().sprite = GM.Character[list[i]];


            if (buildingStatus.building.getNeedPlayerList().Contains(p.GetComponent<PlayerManager>().myColor) && p.GetComponent<PlayerManager>().isLocalPlayer)
            {
                var slider = ActivePanel.transform.Find("InvestPopulationSlider").GetComponent<Slider>();
                var inputField = ActivePanel.transform.Find("InvestPopulationInputField").GetComponent<InputField>();
                var currentPopulation = ActivePanel.transform.Find("CurrentPopulationBackground").Find("CurrentPopulation").GetComponent<Text>();
                var send = ActivePanel.transform.Find("SendInvest").GetComponent<Button>();
                var investPoint = p.GetComponent<PlayerAction>().investPoint;
               
                p.GetComponent<PlayerAction>().ActivePanel = instance_InvestCanvas.transform.Find("ActivePanel").gameObject;
                p.GetComponent<PlayerAction>().WaitingPanel = instance_InvestCanvas.transform.Find("WaitingPanel").gameObject;

                slider.maxValue = p.GetComponent<PlayerManager>().Population;
                currentPopulation.text = "현재 인원\n" + p.GetComponent<PlayerManager>().Population.ToString();
                slider.onValueChanged.AddListener(delegate { 
                    investPoint = (int)slider.value;
                    inputField.text = ((int)slider.value).ToString();
                });
                inputField.onValueChanged.AddListener(delegate { 
                    investPoint = int.Parse(inputField.text); 
                    slider.value = int.Parse(inputField.text);
                    if (investPoint > p.GetComponent<PlayerManager>().Population)
                    {
                        investPoint = p.GetComponent<PlayerManager>().Population;
                        inputField.text = p.GetComponent<PlayerManager>().Population.ToString();
                    }
                        
                });
                send.onClick.AddListener(delegate { 
                    p.GetComponent<PlayerAction>().CmdSendInvest(investPoint);
                    p.GetComponent<PlayerManager>().Population -= investPoint;
                    p.GetComponent<PlayerManager>().UpdatePopulationTextUI();
                    InGameSoundManager.SEbuttonPlay.Invoke();
                    ActivePanel.SetActive(false);
                    WaitingPanel.SetActive(true);
                    p.GetComponent<PlayerAction>().TimerReset();
                    if (p.GetComponent<PlayerAction>().GM.waiting.FindAll(x => x == true).Count == 5)
                    {
                        p.GetComponent<PlayerAction>().CmdNextTurn();
                        p.GetComponent<PlayerAction>().CmdHideInvestCanvas();
                        p.GetComponent<PlayerAction>().CmdTurnEnd();
                    }
                    else
                        p.GetComponent<PlayerAction>().CmdWaiting();
                });
            }
            else if (!buildingStatus.building.getNeedPlayerList().Contains(p.GetComponent<PlayerManager>().myColor) && p.GetComponent<PlayerManager>().isLocalPlayer)
            {
                ActivePanel.SetActive(false);
                WaitingPanel.SetActive(true);
                p.GetComponent<PlayerAction>().TimerReset();
                p.GetComponent<PlayerAction>().CmdWaiting();
            }
        }
    }

    public void HideInvestCanvas() 
    {
        Destroy(instance_InvestCanvas);
        ShowUICanvas();
    }

    public void ShowVoteCanvas()
    {
        _voiceManager = VoiceManager.Instance;
        //_voiceManager.AudioInputDevices.Muted = false;
        //_voiceManager.OnTextMessageLogReceivedEvent -= GM.LM.OnLogReceivedEvent;
        Camera.main.GetComponent<CameraMove>().ReturnToInGame();
        HideReturnInGameCanvas();
        HideLogCanvas();
        HideStatusCanvas();
        HideUICanvas();
        instance_VoteCanvas = Instantiate(VoteCanvas, new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, -5), transform.rotation);
        instance_VoteCanvas.transform.SetParent(Camera.main.transform);
        instance_VoteCanvas.GetComponent<Canvas>().worldCamera = Camera.main.GetComponent<Camera>();
        instance_VoteCanvas.GetComponent<Canvas>().sortingLayerName = "Canvas";
        instance_VoteCanvas.GetComponent<Canvas>().sortingOrder = 5;
        
        voiceOnOffImg = instance_VoteCanvas.transform.Find("Panel").Find("voiceChat").GetComponent<Image>();
        instance_VoteCanvas.transform.Find("Panel").Find("voiceChat").GetComponent<Button>().onClick.AddListener(() => voiceChatBtnClicked(roomPlayer));

        instance_VoteCanvas.transform.Find("Panel").Find("textChat").GetComponent<Button>().onClick.AddListener(() =>
            {
                instance_VoteCanvas.transform.Find("Panel").Find("ChattingPanel").localPosition = new Vector2(105f, -1.5f);
                instance_VoteCanvas.transform.Find("Panel").Find("CloseChatting").localPosition = new Vector2(-1f, -0.6f);
            }
        );

        instance_VoteCanvas.transform.Find("Panel").Find("CloseChatting").GetComponent<Button>().onClick.AddListener(() =>
        {
            instance_VoteCanvas.transform.Find("Panel").Find("ChattingPanel").localPosition = new Vector2(2274f, 223f);
            instance_VoteCanvas.transform.Find("Panel").Find("CloseChatting").localPosition = new Vector2(2245f, 223f);
        }
);

        Player = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in Player)
        {
            p.GetComponent<PlayerAction>().TimerReset();
            p.GetComponent<PlayerAction>().timerPhase = TimerPhase.VoteTime;
            p.GetComponent<PlayerAction>().TimerStart();
            if(InGameSoundManager.SEtimer.isPlaying)
                InGameSoundManager.SEtimer.Stop();

            PlayerManager _player = p.GetComponent<PlayerManager>();
            GameObject Panel = instance_VoteCanvas.transform.Find("Panel").gameObject;
            GameObject VotePlayer1 = Panel.transform.Find("VotePlayer1").gameObject;
            GameObject VotePlayer2 = Panel.transform.Find("VotePlayer2").gameObject;
            GameObject VotePlayer3 = Panel.transform.Find("VotePlayer3").gameObject;
            GameObject VotePlayer4 = Panel.transform.Find("VotePlayer4").gameObject;
            GameObject VotePlayer5 = Panel.transform.Find("VotePlayer5").gameObject;
            GameObject VotePlayer6 = Panel.transform.Find("VotePlayer6").gameObject;

            _player.speakingAnimator = Panel.transform.Find("VotePlayer" + (_player.index + 1).ToString()).GetComponentInChildren<Animator>();
            Image image = Panel.transform.Find("VotePlayer" + (_player.index + 1).ToString()).Find("Image").GetComponent<Image>();
            image.sprite = _player.color_sprite[_player.myColor];

            VotePlayer1.GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().SetVote0);
            VotePlayer2.GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().SetVote1);
            VotePlayer3.GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().SetVote2);
            VotePlayer4.GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().SetVote3);
            VotePlayer5.GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().SetVote4);
            VotePlayer6.GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().SetVote5);
            Panel.transform.Find("SkipVote").GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().SetVoteSkip);
        }
    }

    public void HideVoteCanvas()
    {
        Player = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in Player)
        {
            p.GetComponent<PlayerAction>().TimerReset();
        }
        _voiceManager = VoiceManager.Instance;
        //_voiceManager.OnTextMessageLogReceivedEvent += GM.LM.OnLogReceivedEvent;
        _voiceManager.AudioInputDevices.Muted = true;
        Destroy(instance_VoteCanvas);
        ShowUICanvas();
    }

    public void voiceChatBtnClicked(NetworkRoomPlayerWBTB Player)
    {
        //Debug.Log("voiceChatBtnClicked");
        // local input device를 unmute시킴
        //Player.isMuted = !Player.isMuted;
        _voiceManager.AudioInputDevices.Muted = !_voiceManager.AudioInputDevices.Muted;
        voiceOnOffClicked = !voiceOnOffClicked;
        if (voiceOnOffClicked)
        {
            voiceOnOffImg.sprite = voiceOnImg;
        }
        else
        {
            voiceOnOffImg.sprite = voiceOffImg;
        }

    }

    /*#region 투자 성공/실패 패널 띄우기

    [Command]
    public void CmdSuccessInvest()
    {
        RpcSuccessInvest();
    }

    [ClientRpc]
    public void RpcSuccessInvest()
    {
        InGameSoundManager.SEinvestSuccess.Play();

        instance_InvestCanvas.transform.Find("WaitingPanel").gameObject.SetActive(false);
        instance_InvestCanvas.transform.Find("InvestResultPanel").Find("InvestResultText").GetComponent<Text>().text = "투자에 성공했습니다!";
        instance_InvestCanvas.transform.Find("InvestResultPanel").gameObject.SetActive(true);
    }

    [Command]
    public void CmdFailInvest()
    {
        RpcFailInvest();
    }

    [ClientRpc]
    public void RpcFailInvest()
    {
        InGameSoundManager.SEinvestFail.Play();

        instance_InvestCanvas.transform.Find("WaitingPanel").gameObject.SetActive(false);
        instance_InvestCanvas.transform.Find("InvestResultPanel").Find("InvestResultText").GetComponent<Text>().text = "투자에 실패했습니다...";
        instance_InvestCanvas.transform.Find("InvestResultPanel").gameObject.SetActive(true);
    }
    #endregion*/
}
