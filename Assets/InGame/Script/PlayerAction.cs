using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using VivoxUnity;

using Debug = UnityEngine.Debug;

public enum TimerPhase { None, TurnStartTime, RollDiceTime, PlayerActionTime, InvestTime, VoteTime }

public class PlayerAction : NetworkBehaviour
{
    PlayerManager PM;
    public GameManager GM;
    
    [SyncVar]
    public int index;

    [SerializeField] 
    GameObject RollDiceBtn;

    GameObject instance;

    [SerializeField] 
    GameObject TurnStart;
    [SerializeField] 
    GameObject TurnStartText;
    public Text CompletedText;
    public Text DamagedText;
    public Text DestroyedText;

    public bool TurnStarted;
    public bool DiceRolled;
    public bool isDiceCreated;

    public int investPoint;

    public InGameSoundPlay InGameSoundManager;

    public Text timer;
    public Stopwatch sw;
    public bool DisplayTimer;
    public TimerPhase timerPhase;
    public GameObject ActivePanel;
    public GameObject WaitingPanel;
    int tempturnfortimeoverinvest;
    [SyncVar]
    public bool updateBuildingStateInvoked;

    public VoiceManager _voiceManager;

    void Awake()
    {
        _voiceManager = VoiceManager.Instance;
        GM = GameObject.Find("GameManager").GetComponent<GameManager>();
        PM = this.gameObject.GetComponent<PlayerManager>();
        TurnStarted = false;
        DiceRolled = false;
        isDiceCreated = false;
        DisplayTimer = false;
        tempturnfortimeoverinvest = 0;
        sw = new Stopwatch();
        timerPhase = TimerPhase.None;
        updateBuildingStateInvoked = false;
        //UpdateTurnStartUI();
    }

    private void Start()
    {
        InGameSoundManager = GameObject.Find("LocalSoundManager").GetComponent<InGameSoundPlay>();
        timer = GameObject.Find("Timer").GetComponent<Text>();
        //SyncIndex();
        //GM.LM.AssignAuth(this.GetComponent<NetworkIdentity>().connectionToClient);
        GM.GotoNextTurn += GotoTurnNext;
    }

    private void FixedUpdate()
    {
        if(!TurnStarted)
        {
            GM.TurnEnded = false;
            UpdateTurnStartUI();
            if (isServer)
                RpcShowTurnStartUI();
            updateBuildingStateInvoked = false;
            isDiceCreated = false;
            TurnStarted = true;
        }
        if(GM.TurnEnded)
        {
            TurnStarted = false;
            DiceRolled = false;
        }
        if(GM.WhoTurn < 6 && index == GM.TurnOrderDistribution[GM.WhoTurn] && isLocalPlayer && !isDiceCreated)
        {
            TurnStart.SetActive(true);
        }
        if(DisplayTimer && isLocalPlayer)
        {
            switch(timerPhase)
            {
                case TimerPhase.TurnStartTime:
                    if (sw.ElapsedMilliseconds > 3000)
                        CreateRollDiceBtn();
                    break;

                case TimerPhase.RollDiceTime:
                    timer.text = "제한시간 " + (10 - (sw.ElapsedMilliseconds / 1000)).ToString() + "초";
                    if (sw.ElapsedMilliseconds > 10000)
                    {
                        TimeOverRollDice();
                    }
                    break;

                case TimerPhase.PlayerActionTime:
                    timer.text = "제한시간 " + (10 - (sw.ElapsedMilliseconds / 1000)).ToString() + "초";
                    if (sw.ElapsedMilliseconds > 10000)
                    {
                        TimeOverPlayerAction();
                    }
                    break;

                case TimerPhase.InvestTime:
                    timer.text = "제한시간 " + (20 - (sw.ElapsedMilliseconds / 1000)).ToString() + "초";
                    if (sw.ElapsedMilliseconds > 20000)
                    {
                        TimeOverInvest();
                    }
                    break;

                case TimerPhase.VoteTime:
                    timer.text = "제한시간 " + (90 - (sw.ElapsedMilliseconds / 1000)).ToString() + "초";
                    if (sw.ElapsedMilliseconds > 80000)
                    {
                        if (!InGameSoundManager.SEtimer.isPlaying)
                            InGameSoundManager.SEtimer.Play();
                    }
                    if (sw.ElapsedMilliseconds > 90000)
                    {
                        TimeOverVote();
                    }
                    break;
            }
            
        }
    }

    public void GetResource()
    {
        InGameSoundManager.SEresource.Play();
        TimerReset();

        if (!PM.isAlien) PM.Population += 350;     //인간일때
        else PM.Population = PM.Population + 200;  //외계인일때

        GM.CP.HidePlayerActionCanvas();

        PM.UpdatePopulationTextUI();
        //Destroy(instance);
        //GM.RpcLogAddedEventInvoke($"\'<color=green>{PM.playerNameList[index]}</color>\'님이 자원 획득을 했습니다.");
        //GM.LM.CmdOnLogReceivedEvent($"\'<color=#{ColorUtility.ToHtmlStringRGBA(GM.color[GM.ColorDistribution[index]])}>{GM.playerNameList[index]}</color>\'님이 자원 획득을 했습니다.");
        if(isServer)
            GM.RpcLog($"\'<color=#{ColorUtility.ToHtmlStringRGBA(GM.color[GM.ColorDistribution[index]])}>{GM.playerNameList[index]}</color>\'님이 자원 획득을 했습니다.");
        else
            CmdSyncLog($"\'<color=#{ColorUtility.ToHtmlStringRGBA(GM.color[GM.ColorDistribution[index]])}>{GM.playerNameList[index]}</color>\'님이 자원 획득을 했습니다.");
        CmdNextTurn();
        CmdTurnEnd();
    }

    public void TimeOverRollDice()
    {
        TimerReset();
        RollDice();
    }

    public void TimeOverPlayerAction()
    {
        TimerReset();
        GetResource();
    }

    public void TimeOverInvest()
    {
        TimerReset();
        CmdNextTurnForTimeOverInvest(tempturnfortimeoverinvest);
        CmdHideInvestCanvas();
        CmdTurnEnd();
    }

    public void TimeOverVote()
    {
        TimerReset();
        SetVoteNone();
    }

    public void Invest()
    {
        InGameSoundManager.SEbuttonPlay.Invoke();
        GM.CP.HidePlayerActionCanvas();
        CmdShowInvestCanvas();
        //Destroy(instance);
        if (isLocalPlayer)
            CmdSyncLog($"\'<color=#{ColorUtility.ToHtmlStringRGBA(GM.color[GM.ColorDistribution[index]])}>{GM.playerNameList[index]}</color>\'님이 건물 건설을 시도했습니다.");
            //GM.LM.CmdOnLogReceivedEvent($"\'<color=#{ColorUtility.ToHtmlStringRGBA(GM.color[GM.ColorDistribution[index]])}>{GM.playerNameList[index]}</color>\'님이 건물 건설을 시도했습니다.");
    }

    public void RollDice()
    {   
        if(DiceRolled)
        {
            Debug.Log("DiceRolled");
            return;
        }
        Destroy(instance);
        InGameSoundManager.SEdice.Play();
        TimerReset();

        DiceRolled = true;
        int Dicenum = Random.Range(1, 7);

        if (!isServer)
        {
            CmdRollDice(Dicenum);
        }
        else
        {
            RpcRollDice(Dicenum);
        }

    }

    public void CreateRollDiceBtn()
    {
        if (!(isLocalPlayer && index == GM.TurnOrderDistribution[GM.WhoTurn]))
        {
            TurnStart.SetActive(false);
            isDiceCreated = true;
            return;
        }
        
        instance = Instantiate(RollDiceBtn, transform.position, transform.rotation);
        instance.transform.SetParent(GameObject.Find("UICanvas").transform);
        instance.transform.localPosition = new Vector3(0f, -440f, 0f);
        instance.transform.localScale = new Vector3(1f, 1f, 1f);
        instance.GetComponent<Button>().onClick.AddListener(RollDice);
        isDiceCreated = true;
        TimerReset();
        timerPhase = TimerPhase.RollDiceTime;
        TimerStart();
        TurnStart.SetActive(false);
    }

    public void UpdateTurnStartUI()
    {
        string playerName;
        if(GM.WhoTurn < 6)
        {
            tempturnfortimeoverinvest = GM.WhoTurn + 1;
            if (index == GM.TurnOrderDistribution[GM.WhoTurn])
            {
                playerName = GM.playerNameList[GM.TurnOrderDistribution[GM.WhoTurn]];
                //TurnStartText.GetComponent<Text>().text = playerName + "의 턴 입니다.";
                TurnStartText.GetComponent<Text>().text = $"{playerName} 의 턴 입니다.";
                if (index == GM.TurnOrderDistribution[GM.WhoTurn] && isLocalPlayer)
                {
                    TimerReset();
                    timerPhase = TimerPhase.TurnStartTime;
                    TimerStart();
                    if (InGameSoundManager.SEtimer.isPlaying)
                        InGameSoundManager.SEtimer.Stop();
                }
            }
                
        }
    }

    [ClientRpc]
    void RpcShowTurnStartUI()
    {
        if (GM.WhoTurn < 6 && index == GM.TurnOrderDistribution[GM.WhoTurn])
            TurnStart.SetActive(true);
        else
            TurnStart.SetActive(false);
    }

    void GotoTurnNext()
    {
        RpcHideVoteCanvas();
        CmdNextWholeTurn();
        RpcTurnEnd();
    }


    public void TimerStart()
    {
        InGameSoundManager.SEtimer.Play();
        sw.Start();
        DisplayTimer = true;
    }

    public void TimerReset()
    {
        timerPhase = TimerPhase.None;
        InGameSoundManager.SEtimer.Stop();
        sw.Stop();
        sw.Reset();
        timer.text = "";
        DisplayTimer = false;
    }


    #region 주사위 던지기의 서버동기화
    [Command]
    void CmdRollDice(int num)
    {
        RpcRollDice(num);
    }

    [ClientRpc]
    void RpcRollDice(int num)
    {
        GM.Dice = num;
        GM.DiceResult();
    }

    #endregion

    #region 턴 넘기기

    [Command]
    public void CmdNextTurn()
    {
        GM.WhoTurn++;
        RpcNextTurn(GM.WhoTurn);
    }

    [ClientRpc]
    public void RpcNextTurn(int netwhoturn)
    {
        GM.WhoTurn = netwhoturn;  
        if (GM.WhoTurn > 5)
        {
            RpcShowVoteCanvas();
            //GM.RpcUpdateBuildingState();
        }
        if(!updateBuildingStateInvoked)
        {
            GM.RpcUpdateBuildingState();
            RpcUpdateBuildingStateInvoked();
        }
    }

    [Command]
    void CmdNextTurnForTimeOverInvest(int value)
    {
        GM.WhoTurn = value;
        RpcNextTurnForTimeOverInvest(GM.WhoTurn);
    }

    [ClientRpc]
    void RpcNextTurnForTimeOverInvest(int value)
    {
        GM.WhoTurn = value;
        if (GM.WhoTurn > 5)
        {
            RpcShowVoteCanvas();
            //GM.RpcUpdateBuildingState();
        }
        if (!updateBuildingStateInvoked)
        {
            GM.RpcUpdateBuildingState();
            RpcUpdateBuildingStateInvoked();
        }
    }

    #endregion

    #region 턴 끝내기

    [Command]
    public void CmdTurnEnd()
    {
        RpcTurnEnd();
    }

    [ClientRpc]
    public void RpcTurnEnd()
    {
        GM.TurnEnded = true;
    }

    #endregion

    #region Invest 출력

    [Command]
    void CmdShowInvestCanvas()
    {
        RpcShowInvestCanvas();
    }

    [ClientRpc]
    void RpcShowInvestCanvas()
    {
        GM.CP.ShowInvestCanvas(GM.Buildings[GM.LandNum]);
    }

    [Command]
    public void CmdHideInvestCanvas()
    {
        RpcHideInvestCanvas();
    }

    [ClientRpc]
    public void RpcHideInvestCanvas()
    {
        CmdResultInvest();
        CmdClearWaiting();
        TimerReset();
        GM.CP.HideInvestCanvas();
    }

    [Command]
    public void CmdResultInvest()
    {
        RpcResultInvest();
    }

    [ClientRpc]
    public void RpcResultInvest()
    {
        var suminvest = 0;
        var curbuild = GM.Buildings[GM.LandNum];
        for (int i = 0; i < 6; i++)
        {
            suminvest += GM.invest[i];
        }

        if (suminvest >= curbuild.building.getMinPopulation())
        {
            InGameSoundManager.SEinvestSuccess.Play();
            if (curbuild.building.getState() == State.Idle)
            {
                curbuild.building.setState(State.Constructing);
                //GM.RpcLogAddedEventInvoke($"\t건물 건설이 성사되었습니다.");
                if (isServer)
                    GM.RpcLog($"[{curbuild.gameObject.name}]건물 건설이 성사되었습니다.");
                    //GM.LM.CmdOnLogReceivedEvent($"[{curbuild.gameObject.name}]건물 건설이 성사되었습니다.");
            }
            else if (curbuild.building.getState() == State.Damaged)
            {
                curbuild.building.setState(State.Repair);
                //GM.RpcLogAddedEventInvoke($"\t건물 건설이 무산되었습니다.");
                if (isServer)
                    GM.RpcLog($"[{curbuild.gameObject.name}]건물 수리가 성사되었습니다.");
                    //GM.LM.CmdOnLogReceivedEvent($"[{curbuild.gameObject.name}]건물 수리가 성사되었습니다.");
            }
            curbuild.building.setCompleteTurn((int)Mathf.Ceil(curbuild.building.getNeedPopulation() / suminvest));
            if(curbuild.building.getCompleteTurn() < 1)
            {
                curbuild.building.setCompleteTurn(1);
                Debug.Log("Turn을 1로 변경함.");
            }
                
            for (int i = 0; i < 6; i++)
            {
                if(GM.AlienDistribution[i] == true)
                {
                    curbuild.building.setAlien(curbuild.building.getAlien() + GM.invest[i]);
                }
                else
                {
                    curbuild.building.setHuman(curbuild.building.getHuman() + GM.invest[i]);
                }    
            }
            curbuild.ShowBuildingStateImage();
        }
        else
        {
            if (isServer)
                GM.RpcLog($"[{curbuild.gameObject.name}]건물 건설이 무산되었습니다.");
                //GM.LM.CmdOnLogReceivedEvent($"[{curbuild.gameObject.name}]건물 건설이 무산되었습니다.");
            InGameSoundManager.SEinvestFail.Play();
        }

        for (int i = 0; i < 6; i++)
        {
            GM.invest[i] = 0;
        }
    }

    [Command]
    void CmdUpdateBuildingStateInvoked()
    {
        RpcUpdateBuildingStateInvoked();
    }

    [ClientRpc]
    void RpcUpdateBuildingStateInvoked()
    {
        updateBuildingStateInvoked = true;
    }

    #endregion

    #region Invest 하기

    [Command]
    public void CmdSendInvest(int value)
    {
        RpcSendInvest(value);
    }

    [ClientRpc]
    public void RpcSendInvest(int value)
    {
        GM.invest[index] = value;
    }

    #endregion

    #region 투표

    public void SetVote0()
    {
        CmdVote(index, 0);
    }
    public void SetVote1()
    {
        CmdVote(index, 1);
    }
    public void SetVote2()
    {
        CmdVote(index, 2);
    }
    public void SetVote3()
    {
        CmdVote(index, 3);
    }
    public void SetVote4()
    {
        CmdVote(index, 4);
    }
    public void SetVote5()
    {
        CmdVote(index, 5);
    }
    public void SetVoteSkip()
    {
        CmdVote(index, 7);
    }
    public void SetVoteNone()
    {
        CmdVote(index, 8);
    }

    [Command]
    void CmdVote(int index, int votenum)
    {
        InGameSoundManager.SEbuttonPlay.Invoke();
        RpcVote(index, votenum);
    }

    [ClientRpc]
    void RpcVote(int index, int votenum)
    {
        GM.UpdateVoteResult(index, votenum);
        Button tmp = GameObject.Find("VotePlayer" + (index + 1).ToString()).GetComponent<Button>();
        Color newcolor = Color.white;
        ColorBlock cb = tmp.colors;
        switch(votenum)
        {
            case 0:
                newcolor = GM.color[GM.ColorDistribution[0]];
                break;

            case 1:
                newcolor = GM.color[GM.ColorDistribution[1]];
                break;

            case 2:
                newcolor = GM.color[GM.ColorDistribution[2]];
                break;

            case 3:
                newcolor = GM.color[GM.ColorDistribution[3]];
                break;

            case 4:
                newcolor = GM.color[GM.ColorDistribution[4]];
                break;

            case 5:
                newcolor = GM.color[GM.ColorDistribution[5]];
                break;

            case 7:
                newcolor = Color.gray;
                break;

            case 8:
                newcolor = Color.white;
                break;
        }

        cb.normalColor = newcolor;
        cb.highlightedColor = newcolor;
        cb.pressedColor = newcolor;
        cb.selectedColor = newcolor;
        cb.disabledColor = newcolor;
        tmp.colors = cb;
    }

    [ClientRpc]
    void RpcShowVoteCanvas()
    {
        GM.CP.ShowVoteCanvas();
    }

    [Command]
    public void CmdHideVoteCanvas()
    {
        RpcHideVoteCanvas();
    }

    [ClientRpc]
    void RpcHideVoteCanvas()
    {
        GM.CP.HideVoteCanvas();
    }

    [Command]
    public void CmdNextWholeTurn()
    {
        RpcNextWholeTurn();
    }

    [ClientRpc]
    void RpcNextWholeTurn()
    {
        GM.WhoTurn = 0;
        GM.Turn += 1;
        GM.RpcUpdateBuildingState();
        GM.UpdateTurn();
    }

    #endregion

    #region 투자 후 대기상태

    [Command]
    public void CmdWaiting()
    {
        RpcWaiting();
    }

    [ClientRpc]
    public void RpcWaiting()
    {
        GM.waiting[index] = true;
        if(GM.waiting.FindAll(x => x == true).Count == 6)
        {
            StartCoroutine(WaitingCheck(GM.waiting.FindAll(x => x == true)));
        }
    }

    IEnumerator WaitingCheck(List<bool> b)
    {
        yield return new WaitUntil(() => b.Count >= 6);
        CmdNextTurn();
        CmdHideInvestCanvas();
        CmdTurnEnd();
    }

    [Command]
    void CmdClearWaiting()
    {
        RpcClearWaiting();
    }

    [ClientRpc]
    void RpcClearWaiting()
    {
        for(int i = 0; i < 6; i++)
        {
            GM.waiting[i] = false;
        }
    }
    #endregion

    #region index 동기화

    public override void OnStartServer()
    {
        //index = connectionToClient.connectionId;
        GameObject[] p = GameObject.FindGameObjectsWithTag("PlayerList");
        GameObject[] pl = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject pg in pl)
        {

            foreach (GameObject g in p)
            {
                if (g.GetComponent<NetworkRoomPlayerWBTB>().connectionToClient.connectionId == pg.GetComponent<PlayerAction>().connectionToClient.connectionId)
                    pg.GetComponent<PlayerAction>().index = g.GetComponent<NetworkRoomPlayerWBTB>().index;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(this.index == 5)
            CmdAskServer();
    }

    [Command]
    void CmdAskServer()
    {
        OnStartServer();
    }

    #endregion

    #region 로그

    [Command]
    public void CmdSyncLog(string s)
    {
        GM.RpcLog(s);
    }

    #endregion
}
