using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum State { Idle, Constructing, Complete, Damaged, Repair, Destroyed };

public class Building
{
    public Building(int _N, int _M, List<int> _L, int _H, int _A, int _S, int _C, State _St)
    {
        NeedPopulation = _N;
        MinPopulation = _M;
        NeedPlayerList = _L;
        Human = _H;
        Alien = _A;
        Score = _S;
        CompleteTurn = _C;
        state = _St;
    }

    private State state;

    private int NeedPopulation;
    private int MinPopulation;
    private int Human;
    private int Alien;
    private int Score;
    private int CompleteTurn;
    private List<int> NeedPlayerList;

    public State getState() { return state; }
    public int getNeedPopulation() { return NeedPopulation; }
    public int getMinPopulation() { return MinPopulation; }
    public List<int> getNeedPlayerList() { return NeedPlayerList; }
    public int getHuman() { return Human; }
    public int getAlien() { return Alien; }
    public int getScore() { return Score; }
    public int getCompleteTurn() { return CompleteTurn; }

    public void setState(State _S) { state = _S; }
    public void setNeedPopulation(int _N) { NeedPopulation = _N; }
    public void setMinPopulation(int _M) { MinPopulation = _M; }
    public void setNeedPlayerList(List<int> _L) { NeedPlayerList = _L; }
    public void setHuman(int _H) { Human = _H; }
    public void setAlien(int _A) { Alien = _A; }
    public void setScore(int _S) { Score = _S; }
    public void setCompleteTurn(int _C) { CompleteTurn = _C; }
}

public class BuildingStatus : MonoBehaviour
{
    

    public GameObject INFO;

    public Sprite[] sprite;

    public Sprite buildingB;
    public Sprite buildingC;

    public GameObject Building;
    public GameObject[] SubBuilding;
    public GameObject[] Fire;
    public GameObject Hammer;
    public GameObject Dust;
    public GameObject Ruin;

    public char BuildingType;

    public Building building;

    public GameObject CompleteTurnImage;

    public InGameSoundPlay InGameSoundManager;

    void Awake()
    {
        CompleteTurnImage.GetComponentInChildren<MeshRenderer>().sortingLayerName = "TurnText";

        switch (BuildingType)
        {
            case 'A':
                building = new Building(1400, 200, new List<int> { 0, 0 }, 0, 0, 500, 0, State.Idle);
                break;

            case 'B':
                building = new Building(6000, 450, new List<int> { 0, 0, 0 }, 0, 0, 1500, 0, State.Idle);
                break;  

            case 'C':
                building = new Building(16000, 800, new List<int> { 0, 0, 0, 0 }, 0, 0, 3500, 0, State.Idle);
                break;

            case 'D':
                building = new Building(60000, 1800, new List<int> { 0, 0, 0, 0, 0, 0 }, 0, 0, 7500, 0, State.Idle);
                break;

            default:
                break;
        }

        ShowBuildingStateImage();
    }

    public void ShowBuildingStateImage() 
    {
        ChangeBuildingAlpha();

        switch (building.getState()) 
        {
            case State.Idle:
                OnINFO();
                OnBuildingImage();
                OffDust();
                OffHammer();
                OffFire();
                OffTurn();
                OffRuin();
                break;

            case State.Constructing:
                InGameSoundManager.SEconstructing.Play();
                OffINFO();
                OnBuildingImage();
                OnDust();
                OnHammer();
                OffFire();
                OnTurn();
                OffRuin();
                break;

            case State.Complete:
                InGameSoundManager.SEcompleted.Play();
                if (InGameSoundManager.SEconstructing.isPlaying)
                    InGameSoundManager.SEconstructing.Stop();
                OffINFO();
                OnBuildingImage();
                OffDust();
                OffHammer();    
                OffFire();
                OffTurn();
                OffRuin();
                break;
         
            case State.Damaged:
                InGameSoundManager.SEdamaged.Play();
                if (InGameSoundManager.SEconstructing.isPlaying)
                    InGameSoundManager.SEconstructing.Stop();
                OffINFO();
                OnBuildingImage();
                OffDust();
                OffHammer();
                OnFire();
                OffTurn();
                OffRuin();
                break;

            case State.Repair:
                InGameSoundManager.SEconstructing.Play();
                OffINFO();
                OnBuildingImage();
                OnDust();
                OnHammer();
                OnFire();
                OnTurn();
                OffRuin();
                break;

            case State.Destroyed:
                InGameSoundManager.SEdestroyed.Play();
                InGameSoundManager.SEafterDestroyed.Play();
                InGameSoundManager.SEafterDestroyed.loop = true;
                if (InGameSoundManager.SEconstructing.isPlaying)
                    InGameSoundManager.SEconstructing.Stop();
                OffINFO();
                OffBuildingImage();
                OffDust();
                OffHammer();
                OffFire();
                OffTurn();
                OnRuin();
                break;

            default:
                break;
        }
    }

    void OnINFO() 
    {
        INFO.transform.position = Building.transform.position;
        INFO.SetActive(true); 
    }

    void OffINFO() { INFO.SetActive(false); }

    void OnBuildingImage() 
    {
        switch (BuildingType) 
        {
            case 'A':
                Building.GetComponent<SpriteRenderer>().sprite = sprite[0];
                Building.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
                for (int i = 0; i < SubBuilding.Length; i++)
                    SubBuilding[i].SetActive(false);
                break;

            case 'B':
                Building.GetComponent<SpriteRenderer>().sprite = sprite[1];
                Building.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
                SubBuilding[1].GetComponent<SpriteRenderer>().sprite = buildingB;
                SubBuilding[1].transform.position = Building.transform.position + new Vector3(1f, 0.5f, 0f);
                SubBuilding[0].SetActive(false);
                SubBuilding[1].SetActive(true);
                break;

            case 'C':
                Building.GetComponent<SpriteRenderer>().sprite = sprite[2];
                Building.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
                SubBuilding[0].transform.position = Building.transform.position + new Vector3(-1.5f, -0.5f, 0f);
                SubBuilding[1].transform.position = Building.transform.position + new Vector3(1.5f, 0f, 0f);
                for (int i = 0; i < SubBuilding.Length; i++)
                {
                    SubBuilding[i].GetComponent<SpriteRenderer>().sprite = buildingC;
                    SubBuilding[i].SetActive(true);
                }
                break;

            case 'D':
                Building.GetComponent<SpriteRenderer>().sprite = sprite[3];
                Building.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
                for (int i = 0; i < SubBuilding.Length; i++)
                    SubBuilding[i].SetActive(false);
                break;

            default:
                break;
        }
    }

    void OffBuildingImage() 
    {
        Building.GetComponent<SpriteRenderer>().sprite = sprite[4];
        for (int i = 0; i < SubBuilding.Length; i++)
            SubBuilding[i].SetActive(false);
    }

    void ChangeBuildingAlpha() 
    {
        Color color = Building.GetComponent<SpriteRenderer>().color;

        if (building.getState() == State.Idle) { color.a = 0.5f; }
        else color.a = 1f;

        Building.GetComponent<SpriteRenderer>().color = color;
        for (int i = 0; i < SubBuilding.Length; i++)
            SubBuilding[i].GetComponent<SpriteRenderer>().color = color;
    }

    void OnFire() 
    {
        Fire[0].transform.position = Building.transform.position + new Vector3(-1f, 1f, 0f);
        Fire[1].transform.position = Building.transform.position;
        Fire[2].transform.position = Building.transform.position + new Vector3(1f, 1f, 0f);
        for (int i = 0; i < Fire.Length; i++)
            Fire[i].SetActive(true);
    }

    void OffFire() 
    {
        for (int i = 0; i < Fire.Length; i++)
            Fire[i].SetActive(false);
    }

    void OnHammer() 
    {
        Hammer.transform.position = Building.transform.position + new Vector3(-1.5f, 0f, 0f);
        Hammer.SetActive(true);
        MoveHammer();
    }

    void OffHammer() 
    {
        CancelInvoke("RotateCW");
        CancelInvoke("RotateCCW");
        Hammer.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        Hammer.SetActive(false); 
    }

    void OnDust() 
    {
        Dust.transform.position = Building.transform.position;
        Dust.SetActive(true); 
    }

    void OffDust() { Dust.SetActive(false); }

    void MoveHammer() { Invoke("RotateCW", 1); }

    void RotateCW() 
    { 
        Hammer.transform.rotation = Quaternion.Euler(0, 0, -45);
        Invoke("RotateCCW", 1);
    }
    
    void RotateCCW() 
    {
        Hammer.transform.rotation = Quaternion.Euler(0, 0, 0);
        Invoke("RotateCW", 1);
    }

    void OnTurn() 
    {
        CompleteTurnImage.transform.position = Building.transform.position + new Vector3(1.5f, 1.5f, 0f);
        CompleteTurnImage.SetActive(true);
        CompleteTurnImage.transform.Find("CompleteTurnText").GetComponent<TextMesh>().text = building.getCompleteTurn().ToString();
    }

    void OffTurn() { CompleteTurnImage.SetActive(false); }

    void OnRuin() 
    {
        Ruin.transform.position = Building.transform.position;
        Ruin.SetActive(true);
    }

    void OffRuin() { Ruin.SetActive(false); }
}

