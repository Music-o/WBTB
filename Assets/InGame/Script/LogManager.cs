using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using VivoxUnity;
using Mirror;

public class LogManager : NetworkBehaviour
{
    public GameManager GM;
    public Text LogHistory;
    public Scrollbar scrollbar;

    public void Awake()
    {
        GM.OnLog += OnAddLog;
    }

    void OnAddLog(string log)
    {
        AppendLog(log);
    }

    void AppendLog(string log)
    {
        StartCoroutine(AppendAndScrollLog(log));
    }

    IEnumerator AppendAndScrollLog(string log)
    {
        LogHistory.text += log + "\n";

        yield return null;
        yield return null;

        scrollbar.value = 0;
    }
}
