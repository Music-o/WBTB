using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    GameObject Target;
    Camera MainCamera;

    public GameManager Manager;

    public float MoveSpeed;
    public int BuildingNum;
    bool isFollowing;
    bool isShowing;

    Vector3 TargetPosition;
    Vector3 ReturnPosition;

    public InGameSoundPlay InGameSoundManager;

    void Awake()
    {
        isFollowing = true;
        isShowing = false;

        Target = GameObject.FindGameObjectWithTag("Marker");

        MainCamera = GetComponent<Camera>();
    }

    void Update()
    {
        if (!isShowing)
        {
            if (isFollowing) SetLand();

            TargetPosition.Set(Target.transform.position.x, Target.transform.position.y, this.transform.position.z);

            this.transform.position = Vector3.Lerp(this.transform.position, TargetPosition, MoveSpeed * Time.deltaTime);
        }
    }

    void SetLand() 
    {
        BuildingNum = Manager.LandNum;
    }

    public void FindMarker() 
    {
        isFollowing = true;
        Target = GameObject.FindGameObjectWithTag("Marker");
    }

    public void ShowMap()
    {
        isShowing = true;

        MainCamera.transform.position = new Vector3(43, 35, -10);
        MainCamera.orthographicSize = 42f;

        Manager.CP.HideUICanvas();
        Manager.CP.ShowReturnInGameCanvas();

        InGameSoundManager.SEbuttonPlay.Invoke();
    }

    public void ReturnToInGame()
    {
        isShowing = false;
        MainCamera.orthographicSize = 10f;

        Manager.CP.ShowUICanvas();
        Manager.CP.HideReturnInGameCanvas();

        InGameSoundManager.SEbuttonPlay.Invoke();
    }

    public void FindBeforeLand()
    {
        isFollowing = false;

        BuildingNum -= 1;
        if (BuildingNum == 0) BuildingNum = Manager.MaxLandNum;
        Target = Manager.Buildings[BuildingNum - 1].gameObject;
    }

    public void FindAfterLand()
    {
        isFollowing = false;

        BuildingNum += 1;
        if (BuildingNum > Manager.MaxLandNum) BuildingNum = 1;
        Target = Manager.Buildings[BuildingNum - 1].gameObject;
    }
}
