using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using VivoxUnity;

using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    public int MaxLandNum;
    [SyncVar]
    public int StartLand;
    [SyncVar]
    public int LandNum;
    [SyncVar]
    public int Dice;
    [SyncVar]
    public int Turn;
    [SyncVar]
    public int WhoTurn;
    [SyncVar]
    public bool TurnEnded;
    [SyncVar]
    public int MilitaryPower;
    public ControlPanel CP;

    public BuildingStatus[] Buildings;
    public LogManager LM;
    [SerializeField] GameObject Marker;
    [SerializeField] GameObject MilitaryText;
    public GameObject TurnPointText;
    public NetworkManagerHUDWBTB hUD;
    public NetworkRoomManagerWBTB RoomManager;

    public SyncListBool AlienDistribution;
    public SyncListInt ColorDistribution;
    public SyncListInt vote;
    public SyncListInt invest;
    public SyncListBool waiting;
    public SyncListInt TurnOrderDistribution;

    public UnityAction<int> VoteResult;
    public UnityAction GotoNextTurn;

    //public UnityAction<string> OnLog;
    public event Action<string> OnLog;

    public InGameSoundPlay InGameSoundManager;

    public Sprite[] Character;
    public Image CharacterImage;
    public Color[] color;

    [SerializeField] Sprite[] DiceNumber_image;
    [SerializeField] Image DiceResult_image;

    public VoiceManager _voiceManger;
    public IChannelSession _gameChannel;
    public string[] playerNameList = new string[6];

    void Awake()
    {
        _voiceManger = VoiceManager.Instance;
        foreach (var channel in _voiceManger.ActiveChannels) {
            if (channel.Channel.Name == _voiceManger.channelName) {
                _gameChannel = channel;
                break;
            }
        }
        RoomManager = GameObject.Find("NetworkRoomManager").GetComponent<NetworkRoomManagerWBTB>();

        hUD = GameObject.Find("NetworkRoomManager").GetComponent<NetworkManagerHUDWBTB>();
        int tmp = 0;
        foreach(string s in hUD.PlayerNameList_)
        {
            playerNameList[tmp] = s;
            tmp++;
        }
        //NetworkRoomPlayerWBTB[] _roomPlayer = new NetworkRoomPlayerWBTB[6];
        //int tmp = 0;
        //foreach (var p in GameObject.FindGameObjectsWithTag("PlayerList"))
        //{
        //    _roomPlayer[tmp] = p.GetComponent<NetworkRoomPlayerWBTB>();
        //    tmp++;
        //}
        //tmp = 0;
        //foreach (var p in _roomPlayer)
        //{
        //    playerNameList[tmp] = _roomPlayer[tmp].playerNameList[_roomPlayer[tmp].index];
        //    tmp++;
        //}

        MaxLandNum = 20;
        Turn = 1;
        //if (isServer)
        //    RpcLog("--------------1턴------------");
        //    _voiceManger.SendTextMessage("--------------1턴------------", _gameChannel.Key);
        MilitaryPower = 0;
        LandNum = 0;
        WhoTurn = 0;

        Buildings = new BuildingStatus[MaxLandNum];

        for (int i = 0; i < Buildings.Length; i++)
        {
            Buildings[i] = GameObject.Find("Building (" + (i + 1).ToString() + ")").GetComponent<BuildingStatus>();
        }

        CP = GetComponent<ControlPanel>();
        MilitaryText.GetComponent<Text>().text = MilitaryPower.ToString();
        TurnPointText.GetComponent<Text>().text = Turn.ToString();

        for (int i = 0; i < 6; i++)
        {
            vote.Add(6); // 0~5: Player1~6, 6: 투표기본, 7: Skip, 8: 투표안함
            invest.Add(0);
            waiting.Add(false);
            AlienDistribution.Add(false);
            ColorDistribution.Add(-1);
            TurnOrderDistribution.Add(-1);
        }

        DistributionAlien();
        DistributionColor();
        DistributionTurnOrder();

        hUD.alienDisList = AlienDistribution;
        hUD.colorDisList = ColorDistribution;
    }

    void Start()
    {
        var templist0 = new List<int> { 1, 2, 3, 4 };
        var templist1 = new List<int> { 0, 3, 5 };
        var templist2 = new List<int> { 2, 5 };
        var templist3 = new List<int> { 1, 2, 4 };
        var templist4 = new List<int> { 1, 4 };
        var templist5 = new List<int> { 0, 1 };
        var templist6 = new List<int> { 0, 3 };
        var templist7 = new List<int> { 1, 3, 5 };
        var templist8 = new List<int> { 2, 4 };
        var templist9 = new List<int> { 1, 4 };
        var templist10 = new List<int> { 0, 2, 3, 5 };
        var templist11 = new List<int> { 0, 1, 2, 3, 4, 5 };
        var templist12 = new List<int> { 1, 4, 5 };
        var templist13 = new List<int> { 2, 3 };
        var templist14 = new List<int> { 0, 1, 4, 5 };
        var templist15 = new List<int> { 3, 5 };
        var templist16 = new List<int> { 0, 2, 3 };
        var templist17 = new List<int> { 0, 1, 2, 3, 4, 5 };
        var templist18 = new List<int> { 0, 5 };
        var templist19 = new List<int> { 0, 2, 4 };

        Buildings[0].building.setNeedPlayerList(templist0);
        Buildings[1].building.setNeedPlayerList(templist1);
        Buildings[2].building.setNeedPlayerList(templist2);
        Buildings[3].building.setNeedPlayerList(templist3);
        Buildings[4].building.setNeedPlayerList(templist4);
        Buildings[5].building.setNeedPlayerList(templist5);
        Buildings[6].building.setNeedPlayerList(templist6);
        Buildings[7].building.setNeedPlayerList(templist7);
        Buildings[8].building.setNeedPlayerList(templist8);
        Buildings[9].building.setNeedPlayerList(templist9);
        Buildings[10].building.setNeedPlayerList(templist10);
        Buildings[11].building.setNeedPlayerList(templist11);
        Buildings[12].building.setNeedPlayerList(templist12);
        Buildings[13].building.setNeedPlayerList(templist13);
        Buildings[14].building.setNeedPlayerList(templist14);
        Buildings[15].building.setNeedPlayerList(templist15);
        Buildings[16].building.setNeedPlayerList(templist16);
        Buildings[17].building.setNeedPlayerList(templist17);
        Buildings[18].building.setNeedPlayerList(templist18);
        Buildings[19].building.setNeedPlayerList(templist19);


    }

    public void DistributionAlien()
    {
        int count = 0;

        while (count < 2)
        {
            int j = Random.Range(0, 6);
            if (AlienDistribution[j] == true)
            {
                continue;
            }    
            else
            {
                AlienDistribution[j] = true;
                count++;
            }
        }
    }

    public void DistributionColor()
    {
        for (int i = 0; i < 6; i++)
        {
            var random_num = Random.Range(0, 6);
            if (ColorDistribution.Contains(random_num))
            {
                i--;
            }
            else
            {
                ColorDistribution[i] = random_num;
            }
                
        }
    }

    public void DistributionTurnOrder()
    {
        for (int i = 0; i < 6; i++)
        {
            var random_num = Random.Range(0, 6);
            if (TurnOrderDistribution.Contains(random_num))
            {
                i--;
            }
            else
                TurnOrderDistribution[i] = random_num;
        }
    }

    public void DiceResult()
    {
        StartLand = LandNum + 1;
        //LandNum += Dice;
        UpdateDiceResultimage(Dice);

        //if (LandNum > MaxLandNum)
        //    LandNum = (LandNum % MaxLandNum) + 1;

        InGameSoundManager.SEcar.Play();
        InGameSoundManager.SEcar.loop = true;
        InGameSoundManager.SEBuildSoundStop();
        Marker.GetComponent<MarkerMove>().PathMerge();
    }

    void UpdateDiceResultimage(int num)
    {
        if (!DiceResult_image.gameObject.activeInHierarchy)
            DiceResult_image.gameObject.SetActive(true);
        DiceResult_image.sprite = DiceNumber_image[num - 1];
    }

    public void UpdateTurn()
    {
        //RpcUpdateBuildingState();
        TurnPointText.GetComponent<Text>().text = Turn.ToString();
        if (Turn > 3 && Turn <= 6)
        {
            InGameSoundManager.BGM.clip = InGameSoundManager.BGM_phase2;
            if (!InGameSoundManager.BGM.isPlaying)
                InGameSoundManager.BGM.Play();
        }
        else if (Turn > 6 && Turn <= 9)
        {
            InGameSoundManager.BGM.clip = InGameSoundManager.BGM_phase3;
            if (!InGameSoundManager.BGM.isPlaying)
                InGameSoundManager.BGM.Play();
        }
        else if (Turn > 9 && MilitaryPower < 10000)
        {
            if (isServer)
                RpcWhosWin(true);
            else
                CmdWhosWin(true);
            RoomManager.FinishGame();
        }

        if(MilitaryPower >= 10000)
        {
            if (isServer)
                RpcWhosWin(false);
            else
                CmdWhosWin(false);
            RoomManager.FinishGame();
        }

            

        if (isServer)
            RpcLog($"--------------{Turn}턴------------");
            //LM.CmdOnLogReceivedEvent($"--------------{Turn}턴------------");
    }

    public void UpdateVoteResult(int index, int votenum)
    {
        if (isServer)
        {
            vote[index] = votenum;
        }

        if (WhoTurn > 5 && !vote.Contains(6))
        {
            var picked = 6;
            var templist = new List<int>();
            var count = 0;

            for (int idx = 0; idx < 9; idx++)
            {
                templist = vote.FindAll(x => x == idx);
                if (count < templist.Count)
                {
                    count = templist.Count;
                    picked = idx;
                }
            }

            if (picked < 6)
            {
                RpcVoteResult(picked);
                if (isServer)
                    RpcLog($"\'<color=#{ColorUtility.ToHtmlStringRGBA(color[ColorDistribution[picked]])}>{playerNameList[picked]}</color>\'님이 외계인으로 의심받아 40% 자원 패널티를 입었습니다.");
                //LM.CmdOnLogReceivedEvent($"\'<color=#{ColorUtility.ToHtmlStringRGBA(color[ColorDistribution[picked]])}>{playerNameList[picked]}</color>\'님이 외계인으로 의심받아 n% 자원 패널티를 입었습니다.");
            }
            else
            {
                if (isServer)
                    RpcLog($"외계인으로 의심받은 사람이 없어 자원 패널티를 아무도 받지 않았습니다.");
            }
                
                    //LM.CmdOnLogReceivedEvent($"외계인으로 의심받은 사람이 없어 자원 패널티를 아무도 받지 않았습니다.");


            for (int i = 0; i < 6; i++)
            {
                vote[i] = 6;
                //////테스트////
                //vote[i] = 5;
                /////
            }
            //vote[0] = 6;

            GotoNextTurn.Invoke();

            //TurnEnded = true;
        }
    }

    #region 투표결과반영

    [ClientRpc]
    public void RpcVoteResult(int picked)
    {
        VoteResult.Invoke(picked);
    }

    #endregion

    #region 건물상태 및 점수 갱신

    [ClientRpc]
    public void RpcUpdateBuildingState()
    {
        PlayerAction p = null;
        foreach (GameObject _p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (_p.GetComponent<PlayerAction>().index == TurnOrderDistribution[WhoTurn])
            {
                p = _p.GetComponent<PlayerAction>();
                break;
            }
        }

        p.CompletedText.text = "";
        p.DamagedText.text = "";
        p.DestroyedText.text = "";

        foreach (BuildingStatus buildingStatus in Buildings)
        {
            var completeTurn = buildingStatus.building.getCompleteTurn() - 1;
            Debug.Log("이번에 호출됨.");
            buildingStatus.CompleteTurnImage.GetComponentInChildren<TextMesh>().text = completeTurn.ToString();

            if (buildingStatus.building.getState() == State.Constructing || buildingStatus.building.getState() == State.Repair)
            {
                if (completeTurn <= 0)
                {
                    if ((int)buildingStatus.building.getHuman() * 0.4 < buildingStatus.building.getAlien())
                    {
                        if (buildingStatus.building.getState() == State.Constructing)
                        {
                            buildingStatus.building.setState(State.Damaged);
                            buildingStatus.ShowBuildingStateImage();
                            //OnLogAddedEvent.Invoke($"[부지n]의 건물이 공격 당했습니다.");
                            if (isServer)
                                RpcLog($"[{buildingStatus.name}]의 건물이 공격 당했습니다.");
                               // LM.CmdOnLogReceivedEvent($"[{buildingStatus.name}]의 건물이 공격 당했습니다.");
                            p.DamagedText.text += $"{buildingStatus.name} - {buildingStatus.BuildingType}\n";

                        }
                        else
                        {
                            buildingStatus.building.setState(State.Destroyed);
                            buildingStatus.ShowBuildingStateImage();
                            //OnLogAddedEvent.Invoke($"[부지n]의 건물이 파괴되었습니다.");
                            if (isServer)
                                RpcLog($"[{buildingStatus.name}]의 건물이 파괴되었습니다.");
                                //LM.CmdOnLogReceivedEvent($"[{buildingStatus.name}]의 건물이 파괴되었습니다.");
                            p.DestroyedText.text += $"{buildingStatus.name} - {buildingStatus.BuildingType}\n";
                        }

                    }
                    else
                    {
                        buildingStatus.building.setState(State.Complete);
                        UpdateScore(buildingStatus.building.getScore());
                        buildingStatus.ShowBuildingStateImage();
                        //OnLogAddedEvent.Invoke($"[부지n]의 건물이 완공되었습니다.");
                        if (isServer)
                            RpcLog($"[{buildingStatus.name}]의 건물이 완공되었습니다.");
                           // LM.CmdOnLogReceivedEvent($"[{buildingStatus.name}]의 건물이 완공되었습니다.");
                        p.CompletedText.text += $"{buildingStatus.name} - {buildingStatus.BuildingType}\n";
                    }
                    buildingStatus.building.setCompleteTurn(0);
                }
                else if (completeTurn > 0)
                {
                    buildingStatus.building.setCompleteTurn(completeTurn);
                }
            }
        }
    }

    void UpdateScore(int score)
    {
        MilitaryPower += score;
        MilitaryText.GetComponent<Text>().text = MilitaryPower.ToString();
    }

    #endregion

    #region 로그 동기화

    [Command]
    public void CmdLog(string log)
    {
        Debug.Log("cmdlog실행됨");
        RpcLog(log);
    }

    [ClientRpc]
    public void RpcLog(string log)
    {
        Debug.Log("rpclog실행됨");
        OnLog.Invoke(log);
    }

    #endregion

    #region 승리씬준비
    [Command]
    void CmdWhosWin(bool b)
    {
        RpcWhosWin(b);
    }

    [ClientRpc]
    void RpcWhosWin(bool b)
    {
        hUD.isAlienWin = b;
    }
    #endregion
}
