using UnityEngine;
using System.Collections.Generic;
using System;

public class MapGenerator : MonoBehaviour
{
    public int width = 64;
    public int height = 36;

    public string seed;                     //随机种子。
    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent = 45;      //随机填充百分比，越大洞越小。

    [Range(0, 10)]
    public int smoothLevel = 4;             //平滑程度。

    int[,] map;                             //地图集，0为空洞，1为实体墙。

    public int wallThresholdSize = 50;      //清除小墙体的阈值。
    public int roomThresholdSize = 50;      //清除小孔的的阈值。

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GenerateMap();
        }
    }

    //生成随机地图。
    void GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < smoothLevel; i++)
        {
            SmoothMap();
        }

        ProcessMap();

        #region Border
        //增加边界墙，暂时没卵用。
        //int borderSize = 1;
        //int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        //for (int x = 0; x < borderedMap.GetLength(0); x++)
        //{
        //    for (int y = 0; y < borderedMap.GetLength(1); y++)
        //    {
        //        if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
        //        {
        //            borderedMap[x, y] = map[x - borderSize, y - borderSize];
        //        }
        //        else
        //        {
        //            borderedMap[x, y] = 1;
        //        }
        //    }
        //}
        #endregion

        //平滑渲染地图。
        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(map, 1);
    }

    //加工地图，把那些小墙们都铲掉。可以优化，一次遍历，判断墙或空洞，分别存到队列，而不是遍历两次。
    void ProcessMap()
    {
        //获取墙区域
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;                //把小于阈值的都铲掉。
                }
            }
        }

        //获取空洞区域
        List<List<Coord>> roomRegions = GetRegions(0);

        List<Room> survivingRooms = new List<Room>();               //存放没被删掉的空洞房间。

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;                //把小于阈值的都填充。
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));      //添加到幸存房间列表里。
            }
        }

        //连接各个幸存房间。
        ConnectClosestRooms(survivingRooms);
    }

    //连接各个房间。每个房间两两比较，找到最近房间（相对前一个房间）连接之，对第二个房间来说不一定就是最近的。
    void ConnectClosestRooms(List<Room> allRooms)
    {
        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in allRooms)
        {
            possibleConnectionFound = false;

            foreach (Room roomB in allRooms)
            {
                if (roomA == roomB)
                    continue;
                if (roomA.IsConnected(roomB))
                {
                    possibleConnectionFound = false;
                    break;
                }

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        //如果找到更近的（相对roomA）房间，更新最短路径。
                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (possibleConnectionFound)                     //找到新的两个连接房间。创建通道。
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }
    }

    //创建两个房间的通道。
    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 100);
    }

    //把xy坐标转换成实际坐标。
    Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY);
    }

    //获取区域，0为空洞，1为墙
    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;   //把获得的新区域再遍历一遍，标记检测过。可以放下面函数优化一下。
                    }
                }
            }
        }

        return regions;
    }

    //从这个点开始获取区域，广度优先算法。
    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];               //标志是否走过。1为走过。
        int tileType = map[startX, startY];                     //把传进来的点的类型（墙或空洞）作为标准类型。

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();                       //弹出队列第一个，添加到要返回的列表里面。
            tiles.Add(tile);

            //二重循环加if，其实就是上下左右四格，而且没走过的，符合类型（墙或空洞）的。
            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    //判断坐标是否在地图里，不管墙还是洞。
    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    //随机填充地图。
    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    //平滑地图
    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > 4)             //周围大于四个实体墙，那自己也实体墙了。
                    map[x, y] = 1;
                else if (neighbourWallTiles < 4)        //周围大于四个为空洞，那自己也空洞了。
                    map[x, y] = 0;
                //还有如果四四开，那就保持不变。
            }
        }
    }

    //获取该点周围8个点为实体墙（map[x,y] == 1）的个数。
    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    //void OnDrawGizmos()
    //{
    //    if (map != null)
    //    {
    //        for (int x = 0; x < width; x++)
    //        {
    //            for (int y = 0; y < height; y++)
    //            {
    //                Gizmos.color = (map[x, y] == 1) ? Color.green : Color.blue;
    //                Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
    //                Gizmos.DrawCube(pos, Vector3.one);
    //            }
    //        }
    //    }
    //}

    //坐标

    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    class Room
    {
        public List<Coord> tiles;                           //所有坐标。
        public List<Coord> edgeTiles;                       //靠边的坐标。
        public List<Room> connectedRooms;                   //与其直接相连的房间。
        public int roomSize;

        public Room() { }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach (Coord tile in tiles)                   //遍历每个坐标上下左右，判断是否有墙
            {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (map[x, y] == 1)
                            {
                                edgeTiles.Add(tile);
                                continue;
                            }
                        }
                    }
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }
    }

}