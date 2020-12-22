using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class LobbySoundPlay : MonoBehaviour
{    
    GameObject SEbutton;
    AudioSource GlobalSoundManager;
    public AudioSource SEready;
    public AudioSource SEcount;
    public Button gameStartBtn;
    public Button voiceChat;
    public Button textChat;
    public Button backSpace;
    public UnityAction roomPlayerBtn;
    public UnityAction roomPlayerReadyBtn;
    // Start is called before the first frame update
    void Start()
    {
        SEbutton = GameObject.Find("SEButtonClicked");
        GlobalSoundManager = GameObject.FindGameObjectWithTag("SoundManager").GetComponent<AudioSource>();
        gameStartBtn.onClick.AddListener(SEButtonClickedPlay);
        voiceChat.onClick.AddListener(SEButtonClickedPlay);
        textChat.onClick.AddListener(SEButtonClickedPlay);
        backSpace.onClick.AddListener(SEButtonClickedPlay);
        roomPlayerBtn += SEButtonClickedPlay;
        roomPlayerReadyBtn += SEready.Play;
        if (!GlobalSoundManager.isPlaying)
            GlobalSoundManager.Play();

    }

    void SEButtonClickedPlay()
    {
        AudioSource SEButtonClicked = SEbutton.GetComponent<AudioSource>();
        SEButtonClicked.Play();
    }
}
