using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class InGameSoundPlay : MonoBehaviour
{
    GameObject SEbutton;

    [Header("AudioSource")]
    public AudioSource BGM;
    public AudioSource SEinvestSuccess;
    public AudioSource SEinvestFail;
    public AudioSource SEconstructing;
    public AudioSource SEdamaged;
    public AudioSource SEresource;
    public AudioSource SEdice;
    public AudioSource SEcar;
    public AudioSource SEdestroyed;
    public AudioSource SEafterDestroyed;
    public AudioSource SEcameraMove;
    public AudioSource SEtimer;
    public AudioSource SEcompleted;

    [Header("AudioClip")]
    public AudioClip BGM_phase2;
    public AudioClip BGM_phase3;

    [Header("UI")]
    public Button findMarkerBtn;
    public Button showMapBtn;
    public Button findAfterLandBtn;
    public Button findBeforeLandBtn;
    public Button showLogPanelBtn;
    public Button hideLogBtn;
    public Button hideStatusCanvasBtn;
    public Button returnInGameBtn;
    public UnityAction SEbuttonPlay;
    // Start is called before the first frame update
    void Start()
    {
        GameObject.Find("GlobalSoundManager").GetComponent<AudioSource>().Stop();
        SEbutton = GameObject.Find("SEButtonClicked");
        SEbuttonPlay += SEButtonClickedPlay;
        findMarkerBtn.onClick.AddListener(SEcameraMove.Play);
        showMapBtn.onClick.AddListener(SEButtonClickedPlay);
        findAfterLandBtn.onClick.AddListener(SEcameraMove.Play);
        findBeforeLandBtn.onClick.AddListener(SEcameraMove.Play);
        showLogPanelBtn.onClick.AddListener(SEButtonClickedPlay);
        hideLogBtn.onClick.AddListener(SEButtonClickedPlay);
        hideStatusCanvasBtn.onClick.AddListener(SEButtonClickedPlay);
        returnInGameBtn.onClick.AddListener(SEButtonClickedPlay);
    }

    void SEButtonClickedPlay()
    {
        AudioSource SEButtonClicked = SEbutton.GetComponent<AudioSource>();
        SEButtonClicked.Play();
    }

    public void SEBuildSoundStop()
    {
        if(SEconstructing.isPlaying)
        {
            SEconstructing.Stop();
        }
        if (SEdamaged.isPlaying)
        {
            SEdamaged.Stop();
        }
        if (SEdestroyed.isPlaying)
        {
            SEdestroyed.Stop();
        }
    }
}
