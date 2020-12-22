using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class NetworkRoomManagerWBTB : NetworkRoomManager // 서버를 총괄하는 스크립트.
{
    string ErrorMessage;

    private VoiceManager _voiceManager;

    [Header("ErrorHandle")]
    [SerializeField] GameObject errorPanel;

    [Header("AfterGame")] // Inspector 창의 정리를 위한 구문.
    [Scene]
    public string winnerScene; // 결과창 Scene.
    public List<Sprite> characterList;

    public override void OnRoomServerPlayersReady() 
    {
        // 모두 준비가 되면 자동실행되는 부분.
        // 우리는 모두 준비가 되어도 호스트가 GameStart 버튼 UI를 눌러야 실행이 되게 할 것임.
        // 따라서 필요없으나 상속하고 있는 NetworkRoomManager가 이를 기본적으로 정의하고 있음.
        // 그러므로 override 후 빈 함수로 만들어서 아무것도 안하게 만든 것.
    }

    public override void OnGUI()
    {
        // OnGUI 환경에서 UI를 만들 경우 필요한 부분.
        // 우리는 Unity Canvas 및 UI를 이용하여 UI를 구성함.
        // 따라서 필요없으나 위와 같은 문제로 똑같이 해결함.
    }

    // Host가 게임을 종료했을 때, 모든 Client의 연결이 끊기며 Title Scene으로 돌아갔을때 NetworkManager 중복 방지를 위한 함수.
    // 또는 Title에서 방참여시 입력한 IP주소가 Hosting 또는 Server 역할을 하지 않고 있는 등의 이유로 연결이 거절될 시,
    // 방만들기나 방참여를 하지 못하는 현상의 방지를 위한 함수.

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        if (IsSceneActive(RoomScene))
        {
            if (roomSlots.Count == maxConnections)
                return;

            allPlayersReady = false;

            //if (logger.LogEnabled()) logger.LogFormat(LogType.Log, "NetworkRoomManager.OnServerAddPlayer playerPrefab:{0}", roomPlayerPrefab.name);

            GameObject newRoomGameObject = OnRoomServerCreateRoomPlayer(conn);
            if (newRoomGameObject == null)
                newRoomGameObject = Instantiate(roomPlayerPrefab.gameObject, Vector3.zero, Quaternion.identity);

            NetworkServer.AddPlayerForConnection(conn, newRoomGameObject);
        }
        else if(IsSceneActive(winnerScene))
        {
            return;
        }
        else
            OnRoomServerAddPlayer(conn);
    }

    public override void OnClientDisconnect(NetworkConnection conn) // Client가 연결이 안되거나 끊길 경우,
    {
        if(IsSceneActive(RoomScene) || IsSceneActive(GameplayScene) || IsSceneActive(winnerScene)) // 현재 Scene이 Lobby라면,
            Destroy(this.gameObject); // NetworkManager를 파괴하여 Title로 돌아갔을 경우 NetworkManager가 중복 되는 것을 방지.
        if(IsSceneActive(offlineScene)) // 현재 Scene이 Title이라면,
        {
            _voiceManager = VoiceManager.Instance;

            _voiceManager.DisconnectAllChannels();
            //_voiceManager.LogOut(); // vivox에서 logout함

            ErrorMessage = "서버에 연결을 실패하였습니다.";
            errorPanel.SetActive(true); // 에러 패널을 띄우고,
            GameObject.Find("ErrorMessage").GetComponent<Text>().text = ErrorMessage; // 에러 메세지를 표시.
        }
        base.OnClientDisconnect(conn); // 나머지 상황은 기본과 똑같이 돌아감.
    }

    public void GameStart() // GameStart 버튼 UI를 누를 경우,
    {
        if (allPlayersReady) // 모든 플레이어가 Ready 상태라면,
        {
            //StartCoroutine(Wait5Sec());
            ServerChangeScene(GameplayScene); // 서버가 모든 플레이어의 화면을 InGame Scene으로 일괄 전환.
        }
    }

    //IEnumerator Wait5Sec()
    //{
    //    yield return new WaitForSeconds(5);
        
    //}

    public void FinishGame() // 임시로 만든 ToWinnerScene 버튼을 누를 경우,
    {
        if (NetworkServer.active && IsSceneActive(GameplayScene)) // 서버가 잘 돌아가고 있고, 현재 Scene이 InGame Scene이라면,
        {
            ServerChangeScene(winnerScene); // 서버가 모든 플레이어의 화면을 Winner Scene으로 일괄 전환.
        }
    }

    public void ReturntoTitle() // Winner Scene에서 아무 곳이나 클릭할 경우,
    {
        if (NetworkServer.active && IsSceneActive(winnerScene)) // 서버가 잘 돌아가고 있고, 현재 Scene이 Winner Scene이라면,
        {
            NetworkServer.DisconnectAllConnections();
            StopHost();
        }
    }
}


