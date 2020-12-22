using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinnerSoundPlay : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject.Find("GlobalSoundManager").GetComponent<AudioSource>().Stop();
    }
}
