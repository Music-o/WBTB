using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class UIfunc : NetworkBehaviour // 각 Scene별 UI 기능을 서버와 동기화하는 스크립트.
{
    GameObject networkroommanager; // 서버를 관리하는 NetworkRoomManager를 가져오기 위한 변수.
    NetworkManager networkmanager; // NetworkRoomManager의 NetworkManager를 가져오기 위한 변수.
    //NetworkManagerHUDWBTB tmp;
    [SerializeField] Text hostcode; // 현재 연결된 서버IP를 표시하기 위한 hostcode UI의 Text 변수.

    public Text countText;
    public GameObject LobbySoundManager;

    private void Awake() // 각 Scene에 들어왔을때 가장 먼저 실행.
    {
        Debug.Log("UIfuncAwake");
        networkroommanager = GameObject.Find("NetworkRoomManager"); // NetworkRoomManager를 가져옴.
        networkmanager = networkroommanager.GetComponent<NetworkManager>(); // NetworkRoomManager의 NetworkManager를 가져옴.
        //tmp = networkroommanager.GetComponent<NetworkManagerHUDWBTB>();
        if (NetworkManager.IsSceneActive("Lobby")) // 현재 Scene이 Lobby라면,
            hostcode.text = networkmanager.networkAddress; //  현재 연결된 서버IP를 받아 hostcode UI의 Text를 이로 변경.
        //tmp.SendLobbyUpdate(NetworkManagerHUDWBTB.MatchStatus.Open);

    }

    // NetworkRoomManager에 정의된 기능들을 각 Scene의 버튼 UI와 동기화.
    public void StopButtons()
    {
        networkroommanager.GetComponent<NetworkManagerHUDWBTB>().StopButtons();
        Destroy(networkroommanager); // Client의 연결이 끊기며 Title Scene으로 돌아갔을때 NetworkManager 중복 방지.
    }

    public void GameStartButton()
    {
        if (!networkroommanager.GetComponent<NetworkRoomManagerWBTB>().allPlayersReady)
            return;
        RpcSyncCount();
        //networkroommanager.GetComponent<NetworkRoomManagerWBTB>().GameStart();
    }
    
    public void FinishGame()
    {
        networkroommanager.GetComponent<NetworkRoomManagerWBTB>().FinishGame();
    }

    IEnumerator CountGameStart()
    {
        for(int i = 5; i > 0; i--)
        {
            countText.text = $"{i}";
            LobbySoundManager.GetComponent<LobbySoundPlay>().SEcount.Play();
            yield return new WaitForSeconds(1);
        }
        if(isServer)
            networkroommanager.GetComponent<NetworkRoomManagerWBTB>().GameStart();
    }

    public void ReturntoTitle()
    {
        networkroommanager.GetComponent<NetworkRoomManagerWBTB>().ReturntoTitle();
    }

    [ClientRpc]
    void RpcSyncCount()
    {
        countText.gameObject.SetActive(true);
        StartCoroutine(CountGameStart());
    }

}
