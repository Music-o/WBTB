using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetFullScreen : MonoBehaviour
{
    Toggle mtoggle;
    // Start is called before the first frame update
    void Start()
    {
        mtoggle = GetComponent<Toggle>();
        mtoggle.onValueChanged.AddListener(delegate
        {
            ToggleValueChanged(mtoggle);
        });
        
        if(Screen.fullScreen)
        {
            mtoggle.isOn = true;
        }
        else
        {
            mtoggle.isOn = false;
        }
    }

    //private void Awake()
    //{
    //    if(Screen.fullScreen)
    //    {
    //        mtoggle.isOn = true;
    //    }
    //    else
    //    {
    //        mtoggle.isOn = false;
    //    }
    //}

    public void ToggleValueChanged(Toggle change)
    {
        if (mtoggle.isOn)
            Screen.fullScreen = true;
        else
            Screen.fullScreen = false;
    }
}
