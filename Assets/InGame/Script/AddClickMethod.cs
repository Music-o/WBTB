using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AddClickMethod : MonoBehaviour
{
    GameObject[] Player;

    void Awake()
    {
        Player = GameObject.FindGameObjectsWithTag("Player");
        if (this.gameObject.name == "GetResourceBtn")
        {
            foreach (GameObject p in Player)
            {
                if(p.GetComponent<PlayerAction>().DiceRolled)
                    this.gameObject.GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().GetResource);     
            }
        }
        if (this.gameObject.name == "Build/RepairBtn")
        {
            foreach (GameObject p in Player)
            {
                this.gameObject.GetComponent<Button>().onClick.AddListener(p.GetComponent<PlayerAction>().Invest);
            }
        }

    }

}
