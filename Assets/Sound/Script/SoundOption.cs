using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundOption : MonoBehaviour
{

    private void Awake()
    {
        GameObject[] soundmanager = GameObject.FindGameObjectsWithTag("SoundManager");
        if (soundmanager.Length > 1)
            Destroy(this.gameObject);
        DontDestroyOnLoad(this.gameObject);
    }
}
