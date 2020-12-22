using Mirror;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.U2D.Path;
using UnityEngine;

[System.Serializable]
public class Node
{
    public Node(bool _isWall, int _x, int _y)
    {
        isWall = _isWall;
        x = _x;
        y = _y;
    }

    public bool isWall;
    public Node ParentNode;

    public int x, y, G, H;
    public int F { get { return G + H; } }
}

public class MarkerMove : MonoBehaviour
{
    public GameObject Marker;
    public GameManager Manager;

    public Vector3 StartLandPosition;
    public Vector3 TargetLandPosition;

    public float Speed;
    public bool allowDiagonal, dontCrossCorner;

    Vector2Int BottomLeft, TopRight;
    Vector2Int StartPos, TargetPos;

    int sizeX, sizeY;

    Node[,] NodeArray;
    Node StartNode, TargetNode, CurNode;
    List<Node> OpenList, ClosedList, FinalNodeList;
    public List<Node> DebugPath;    // Debug
    bool isMoving;
    int Pathindex;
    public List<Node> Path;
    int End;

    GameObject[] Player;

    public InGameSoundPlay InGameSoundManager;

    void Awake()
    {
        isMoving = false;
        Pathindex = 0;
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        if (Pathindex == Path.Count)
        {
            isMoving = false;
            Pathindex = 0;
            Path.Clear();
            Manager.LandNum = End;

            InGameSoundManager.SEcar.Stop();

            Player = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in Player)
            {
                if(p.GetComponent<PlayerAction>().DiceRolled)
                {
                    p.GetComponent<PlayerAction>().TimerReset();
                    p.GetComponent<PlayerAction>().timerPhase = TimerPhase.PlayerActionTime;
                    p.GetComponent<PlayerAction>().TimerStart();
                    Manager.CP.ShowPlayerActionCanvas(Manager.Buildings[Manager.LandNum]);
                    break;
                }
                else
                {
                    continue;
                }
            }
            
            return;
        }

        Vector3 target = new Vector3(Path[Pathindex].x, Path[Pathindex].y, Marker.transform.position.z);

        if (Marker.transform.position.x > target.x)
            Marker.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 90));

        else if (Marker.transform.position.x < target.x)
            Marker.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 270));

        else 
        {
            if(Marker.transform.position.y > target.y)
                Marker.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
            else if(Marker.transform.position.y < target.y)
                Marker.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        }

        Marker.transform.position = Vector3.MoveTowards(Marker.transform.position, target, Speed * Time.deltaTime);

        if (Marker.transform.position == target) Pathindex++;
    }
    
    void PathFinding(Vector2Int _StartPos, Vector2Int _TargetPos)
    {
        BottomLeft = new Vector2Int(_StartPos.x < _TargetPos.x ? _StartPos.x : _TargetPos.x,
                                _StartPos.y < _TargetPos.y ? _StartPos.y : _TargetPos.y);
        TopRight = new Vector2Int(_StartPos.x > _TargetPos.x ? _StartPos.x : _TargetPos.x,
                                _StartPos.y > _TargetPos.y ? _StartPos.y : _TargetPos.y);

        sizeX = TopRight.x - BottomLeft.x + 1;
        sizeY = TopRight.y - BottomLeft.y + 1;

        NodeArray = new Node[sizeX, sizeY];

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                bool Wall = false;
                foreach (Collider2D col in Physics2D.OverlapCircleAll(new Vector2(i + BottomLeft.x, j + BottomLeft.y), 0.4f))
                    if (col.gameObject.layer == LayerMask.NameToLayer("Wall")) Wall = true;

                NodeArray[i, j] = new Node(Wall, i + BottomLeft.x, j + BottomLeft.y);
            }
        }

        StartNode = NodeArray[_StartPos.x - BottomLeft.x, _StartPos.y - BottomLeft.y];
        TargetNode = NodeArray[_TargetPos.x - BottomLeft.x, _TargetPos.y - BottomLeft.y];

        OpenList = new List<Node>() { StartNode };
        ClosedList = new List<Node>();
        FinalNodeList = new List<Node>();

        while (OpenList.Count > 0)
        {
            CurNode = OpenList[0];
            for (int i = 1; i < OpenList.Count; i++)
                if (OpenList[i].F <= CurNode.F && OpenList[i].H < CurNode.H)
                    CurNode = OpenList[i];

            OpenList.Remove(CurNode);
            ClosedList.Add(CurNode);

            if (CurNode == TargetNode)
            {
                Node TargetCurNode = TargetNode;

                while (TargetCurNode != StartNode)
                {
                    FinalNodeList.Add(TargetCurNode);
                    TargetCurNode = TargetCurNode.ParentNode;
                }

                FinalNodeList.Add(StartNode);
                FinalNodeList.Reverse();

                return;
            }

            OpenListAdd(CurNode.x, CurNode.y + 1);
            OpenListAdd(CurNode.x + 1, CurNode.y);
            OpenListAdd(CurNode.x, CurNode.y - 1);
            OpenListAdd(CurNode.x - 1, CurNode.y);
        }
    }

    void OpenListAdd(int checkX, int checkY) 
    {
        if (checkX >= BottomLeft.x && checkX < TopRight.x + 1 && checkY >= BottomLeft.y 
            && checkY < TopRight.y + 1 && !NodeArray[checkX - BottomLeft.x, checkY - BottomLeft.y].isWall 
            && !ClosedList.Contains(NodeArray[checkX - BottomLeft.x, checkY - BottomLeft.y]))
        {
            Node NeighborNode = NodeArray[checkX - BottomLeft.x, checkY - BottomLeft.y];
            int MoveCost = CurNode.G + 10;

            if (MoveCost < NeighborNode.G || !OpenList.Contains(NeighborNode))
            {
                NeighborNode.G = MoveCost;
                NeighborNode.H = (Mathf.Abs(NeighborNode.x - TargetNode.x) + Mathf.Abs(NeighborNode.y - TargetNode.y)) * 10;
                NeighborNode.ParentNode = CurNode;

                OpenList.Add(NeighborNode);
            }
        }
    }

    public void PathMerge()
    {
        int StartNum = Manager.StartLand;
        isMoving = true;
        int Offset = 0;
        Path = new List<Node>();

        for (int index = 0; index < Manager.Dice; index++)
        {
            int Start = StartNum + Offset - 1;
            End = StartNum + Offset;

            if (Start >= Manager.MaxLandNum) Start %= Manager.MaxLandNum;
            if (End >= Manager.MaxLandNum) End %= Manager.MaxLandNum;

            StartLandPosition = Manager.Buildings[Start].transform.position;
            TargetLandPosition = Manager.Buildings[End].transform.position;

            StartPos = new Vector2Int((int)StartLandPosition.x, (int)StartLandPosition.y);
            TargetPos = new Vector2Int((int)TargetLandPosition.x, (int)TargetLandPosition.y);

            PathFinding(StartPos, TargetPos);
            Path.AddRange(FinalNodeList);
            FinalNodeList.Clear();
            Offset++;

            if (Manager.Buildings[End].building.getState() == State.Complete
                || Manager.Buildings[End].building.getState() == State.Destroyed)
                index--;
        }
        DebugPath = Path;
    }
}
