using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WinnerSceneManager : MonoBehaviour
{
    NetworkRoomManagerWBTB Manager;
    NetworkManagerHUDWBTB hUD;
    //public Image Player1;
    //public Image Player2;
    //public Image Player3;
    //public Image Player4;
    public Image[] WinnerPlayer;
    public Text[] WinnerPlayerName;
    //public List<int> alienList;
    //public List<int> humanList;

    // Start is called before the first frame update
    void Start()
    {
        GameObject NetworkRoomManagerObj = GameObject.Find("NetworkRoomManager");
        Manager = NetworkRoomManagerObj.GetComponent<NetworkRoomManagerWBTB>();
        hUD = NetworkRoomManagerObj.GetComponent<NetworkManagerHUDWBTB>();

        if (hUD.isAlienWin)
        {
            int tmp = 0;
            int j = 0;
            foreach (bool i in hUD.alienDisList)
            {
                if(i == true)
                {
                    WinnerPlayer[j].sprite = Manager.characterList[hUD.colorDisList[tmp]];
                    WinnerPlayerName[j].text = hUD.PlayerNameList_[tmp];
                    WinnerPlayer[j].gameObject.SetActive(true);
                    j++;
                }
                tmp++;
            }
        }
        else
        {
            int tmp = 0;
            int j = 0;
            foreach (bool i in hUD.alienDisList)
            {
                if(i == false)
                {
                    WinnerPlayer[j].sprite = Manager.characterList[hUD.colorDisList[tmp]];
                    WinnerPlayerName[j].text = hUD.PlayerNameList_[tmp];
                    WinnerPlayer[j].gameObject.SetActive(true);
                    j++;
                }
                tmp++;
            }
        }
    }
}
