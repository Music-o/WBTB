using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class TitleSoundPlay : MonoBehaviour
{
    GameObject SEbutton;
    [Header("AudioOption")]
    public AudioMixer audioMixer;
    public Slider audioSliderMaster;
    public Slider audioSliderBGM;
    public Slider audioSliderSE;

    [Header("UI")]
    public Button hostingBtn;
    public Button searchingBtn;
    public Button settingBtn;
    public InputField inputNameInputField;
    public Toggle setFullScreenToggle;
    public Button closeBtn;
    // Start is called before the first frame update
    void Start()
    {
        audioSliderMaster.value = GetMasterLevel();
        audioSliderBGM.value = GetBGMLevel();
        audioSliderSE.value = GetSELevel();
        audioSliderMaster.onValueChanged.AddListener(delegate
        {
            float sound = audioSliderMaster.value;
            audioMixer.SetFloat("Master", sound);
        });
        audioSliderBGM.onValueChanged.AddListener(delegate
        {
            float sound = audioSliderBGM.value;
            audioMixer.SetFloat("BGM", sound);
        });
        audioSliderSE.onValueChanged.AddListener(delegate
        {
            float sound = audioSliderSE.value;
            audioMixer.SetFloat("SE", sound);
        });

        SEbutton = GameObject.Find("SEButtonClicked");
        hostingBtn.onClick.AddListener(SEButtonClickedPlay);
        searchingBtn.onClick.AddListener(SEButtonClickedPlay);
        settingBtn.onClick.AddListener(SEButtonClickedPlay);
        inputNameInputField.onEndEdit.AddListener(delegate
        {
            SEButtonClickedPlay();
        });
        setFullScreenToggle.onValueChanged.AddListener(delegate
        {
            SEButtonClickedPlay();
        });
        closeBtn.onClick.AddListener(SEButtonClickedPlay);
    }

    void SEButtonClickedPlay()
    {
        AudioSource SEButtonClicked = SEbutton.GetComponent<AudioSource>();
        SEButtonClicked.Play();
    }

    float GetMasterLevel()
    {
        float value;
        audioMixer.GetFloat("Master", out value);
        return value;
    }

    float GetBGMLevel()
    {
        float value;
        audioMixer.GetFloat("BGM", out value);
        return value;
    }

    float GetSELevel()
    {
        float value;
        audioMixer.GetFloat("SE", out value);
        return value;
    }
}
