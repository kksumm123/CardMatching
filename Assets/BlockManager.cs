using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum BlockType
{
    DontMove = 0,  //(장애물)
    Walkable = 1,  //걸을 수 있음.
    Card1 = 2,
    Card2 = 3,
    Card3 = 4,
    Card4 = 5,
    Card5 = 6,
    Card6 = 7,
    Card7 = 8,
    Card8 = 9,
    Card9 = 10,
    Card10 = 11,
    Card11 = 12,
    Card12 = 13,
    Card13 = 14,
    Card14 = 15,
    Card15 = 16,
    Card16 = 17,
}
public class BlockManager : MonoBehaviour
{
    static public BlockManager Instance;
    LineRenderer line;

    [System.Serializable]
    public class IntList
    {
        public List<int> items = new List<int>();
        public List<BlockInfo> blockInfos = new List<BlockInfo>();
        public override string ToString()
        {
            return items.Select(x => x.ToString()).Aggregate((current, next) => current + ", " + next);
        }
    }
    public List<IntList> map;
    void Awake()
    {
        Instance = this;
        GetMapInfo();
        line = GetComponent<LineRenderer>();
    }
    public TextMesh textMesh;
    private void GetMapInfo()
    {
        List<BlockInfo> allBlocks = new List<BlockInfo>(GetComponentsInChildren<BlockInfo>());

        int maxCountX = allBlocks.OrderBy(x => x.transform.position.x).Last().transform.position.ToVector2Int().x + 1;
        int maxCountY = allBlocks.OrderBy(x => x.transform.position.y).Last().transform.position.ToVector2Int().y + 1;
        map = new List<IntList>(maxCountX);
        for (int i = 0; i < maxCountX; i++)
        {
            map.Add(new IntList());
            for (int y = 0; y < maxCountY; y++)
            {
                map[i].items.Add(0);
                map[i].blockInfos.Add(null);
            }
        }
        foreach (var item in allBlocks)
        {
            item.name = item.transform.position.ToVector2Int().ToString();
            var newTextMesh = Instantiate(textMesh, item.transform);
            newTextMesh.text = item.name;
            newTextMesh.transform.localPosition = Vector3.zero;
            var pos = item.transform.position.ToVector2Int();
            map[pos.x].items[pos.y] = (int)item.blockType;
            map[pos.x].blockInfos[pos.y] = item;
        }
    }
    internal void FindPath(BlockInfo blockInfo)
    {
        StartCoroutine(FindPathCo(blockInfo));
    }
    IEnumerator FindPathCo(BlockInfo blockInfo)
    {
        var pos = blockInfo.transform.position.ToVector2Int();
        Pos result = new Pos();

        yield return StartCoroutine(BFS(new Pos() { x = pos.x, y = pos.y }, (int)blockInfo.blockType, map, result));
        print(result);
    }


    class Pos
    {
        public int x; // 행
        public int y; // 열
        public Direction dir; // ⭐방향⭐ (현재 향하고 있는 방향을 알아서 다음 위치를 큐에 삽입할 때 꺾어야할지를 알 수 있다.) 
        public override string ToString()
        {
            return $"x:{x}, y:{y}, {dir}";
        }
    }    
    enum Direction
    {
        None = -1,
        Left,Right, Down,Up
    }

    public float simulateSpeed = 0.3f;
    IEnumerator BFS(Pos start, int find, List<IntList> board, Pos result)
    {
        Dictionary<Direction, Vector2Int> directions = new Dictionary<Direction, Vector2Int>();
        directions[Direction.Left] = new Vector2Int(-1, 0);
        directions[Direction.Right] = new Vector2Int( 1, 0);
        directions[Direction.Down] = new Vector2Int( 0,-1);
        directions[Direction.Up] = new Vector2Int( 0, 1);

        int width = board.Count;
        int height = board[0].items.Count;

        // start 출발 위치, start_alpha 출발지 알파벳
        Queue<Pos> q = new Queue<Pos>();
        List<IntList> turn_and_check = new List<IntList>(); // 꺾은 횟수 저장

        //꺾은 횟수 임의의 아주 큰수로 설정
        for (int i = 0; i < width; i++)
        {
            turn_and_check.Add(new IntList());
            for (int y = 0; y < height; y++)
            {
                turn_and_check[i].items.Add(int.MaxValue);
            }
        }


        // 출발 지점 예약
        start.dir = Direction.None;
        q.Enqueue(start);
        turn_and_check[start.x].items[start.y] = 0;

        bool first = true; // 출발지의 알파벳과 동일한 위치에서 종료할건데 바로 출발지의 알파벳과 동일하다고 판정되어 종료되면 안되기 때문에 사용할 플래그

        while (q.Count > 0)
        {
            // 방문
            Pos now = q.Dequeue();

            board[now.x].blockInfos[now.y].SetActiveState();

            yield return new WaitForSeconds(simulateSpeed);

            // 짝꿍을 찾았다면! (출발지가 아니고!)
            if (first == false && board[now.x].items[now.y] == find)
            {
                result.x = now.x;
                result.y = now.y;
                yield break;
            }

            first = false; // 출발지 방문시에만 false 상태고 나머지 위치 방문시엔 모두 true 인 상태

            for (Direction i = 0; i <= Direction.Up; ++i)
            {
                var dir = directions[i];
                int nextX = now.x + dir.x;
                int nextY = now.y + dir.y;
                Direction nextDir = i;
                int cornetCount = turn_and_check[now.x].items[now.y]; // 현재 방문 위치까지 꺾은 횟수가 초기값
                if (now.dir != Direction.None && now.dir != nextDir) // 출발지가 아니고(출발지의 방향은 -1로 하였다. 출발지에서 예약되는 위치들은 꺾였다고 판단되지 않기 위해) 방향이 일치하지 않으면 꺾어야 한다. 꺾는 횟수를 1 증가시켜야 한다.
                    cornetCount++;

                // 다음 방문 후보 검사
                if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height) // 1. 범위 내에 있어야 함
                    continue;
                if (cornetCount >= 3) // 꺾은 횟수가 3 이상이 되면 그 위치는 탐색하지 않는다.⭐
                    continue;
                if (board[nextX].items[nextY] != 1 && board[nextX].items[nextY] != find) // 다른 숫자나 장애물(0) 이라면 갈 수 없음,(1은 갈 수 있음)
                    continue;
                if (turn_and_check[nextX].items[nextY] >= cornetCount)
                {
                    // 4. 기존에 찾은 꺾은 횟수 그 이하로 꺾을 수 있다면 더 적은 횟수로 꺾을 수 있는 가능성이 있는 탐색 경로가 되므로 또 삽입
                    q.Enqueue(new Pos() { x = nextX, y = nextY, dir = nextDir });
                    turn_and_check[nextX].items[nextY] = cornetCount; // 위치별 현재까지 꺾은 횟수 업데이트
                }
            }
        }
        result.x = -1;
        result.y = -1;

        //return new Pos() { x = -1, y = -1 }; // while문을 빠져나왔다면 짝꿍알파벳을 찾지 못한 것이다. 즉, 제거 불가능! 제거 불가능시에는 {-1, -1}를 리턴하기로 했다.
    }
}

static public class GroundExtention
{
    static public Vector2Int ToVector2Int(this Vector3 v3)
    {
        return new Vector2Int(Mathf.RoundToInt(v3.x)
            , Mathf.RoundToInt(v3.y));
    }
}