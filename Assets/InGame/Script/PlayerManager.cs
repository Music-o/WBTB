using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using VivoxUnity;

public class PlayerManager : NetworkBehaviour
{
    public int Population;
    //public int BonusPopulation;
    public bool isAlien;
    public int myColor;
    public GameManager GM;
    public VoiceManager _voiceManager;

    public Sprite alien_sprite;
    public Sprite[] color_sprite;
    Image alien_imageUI;
    public Image color_imageUI;
    //public Color[] color;

    Text PopulationText;

    [SyncVar]
    public int index;

    NetworkRoomPlayerWBTB _roomPlayer;
    public IParticipant participant;
    public Animator speakingAnimator;

    [SyncVar(hook = "OnIsSpeakingChanged")]
    private bool IsSpeaking;
    public bool isSpeaking
    {
        get { return IsSpeaking; }
        set
        {
            if (speakingAnimator)
            {
                 CmdSetIsSpeakingValue(value);
            }
        }
    }
    void OnIsSpeakingChanged(bool _, bool IsSpeaking)
    {
        //Debug.Log("set IsSpeaking");
        speakingAnimator.SetBool("isSpeaking", IsSpeaking);
    }

    [Command]
    void CmdSetIsSpeakingValue(bool value)
    {
        IsSpeaking = value;
    }

    public void setupParticipant(IParticipant Participant)
    {
        Debug.Log("setup Pariticipant");
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

    void Awake()
    {
        GM = GameObject.Find("GameManager").GetComponent<GameManager>();
        _voiceManager = VoiceManager.Instance;
        Population = 100;
        PopulationText = GameObject.Find("PopulationText").GetComponent<Text>();
        UpdatePopulationTextUI();
        //int i = 0;
        foreach (var p in GameObject.FindGameObjectsWithTag("PlayerList"))
        {
            if (p.GetComponent<NetworkRoomPlayerWBTB>().index == index)
            {
                _roomPlayer = p.GetComponent<NetworkRoomPlayerWBTB>();
                participant = _roomPlayer.participant;
                setupParticipant(participant);
                break;
            }
        }
        //CmdSyncIndex();
    }

    void Start()
    {
        GM.VoteResult += VoteResult;
        isAlien = GM.AlienDistribution[index];
        myColor = GM.ColorDistribution[index];

        if(isLocalPlayer)
        {
            color_imageUI = GameObject.Find("Character").GetComponent<Image>();
            alien_imageUI = GameObject.Find("PlayerDisposition").GetComponent<Image>();
            color_imageUI.sprite = color_sprite[myColor];
            if (isAlien)
                alien_imageUI.sprite = alien_sprite;
        }
        

      //  if (isAlien)
       //     BonusPopulation = 10;
      //  else
      //      BonusPopulation = 0;
    }

    public void UpdatePopulationTextUI()
    {
        PopulationText.text = Population.ToString();
    }

    void VoteResult(int picked)
    { 
        if(index == picked && isLocalPlayer)
        {
            Population *= 6;
            Population /= 10;
            UpdatePopulationTextUI();
        }
    }

    #region index 동기화

    //[Command]
    //void CmdSyncIndex()
    //{
    //    GameObject[] p = GameObject.FindGameObjectsWithTag("PlayerList");
    //    foreach (GameObject g in p)
    //    {
    //        if (g.GetComponent<NetworkRoomPlayerWBTB>().connectionToClient.connectionId == this.connectionToClient.connectionId)
    //            index = g.GetComponent<NetworkRoomPlayerWBTB>().index;
    //    }
    //}
    public override void OnStartServer()
    {
        //index = connectionToClient.connectionId;
        GameObject[] p = GameObject.FindGameObjectsWithTag("PlayerList");
        GameObject[] pl = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject pg in pl)
        {
            foreach (GameObject g in p)
            {
                if (g.GetComponent<NetworkRoomPlayerWBTB>().connectionToClient.connectionId == pg.GetComponent<PlayerManager>().connectionToClient.connectionId)
                    pg.GetComponent<PlayerManager>().index = g.GetComponent<NetworkRoomPlayerWBTB>().index;
            }
        }
    }

    #endregion
}
