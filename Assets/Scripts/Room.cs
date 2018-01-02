using System;
using System.Collections.Generic;

class Room : IComparable<Room>
{
    public List<Coord> tiles;                           //所有坐标。
    public List<Coord> edgeTiles = new List<Coord>();   //靠边的坐标。
    public List<Room> connectedRooms;                   //与其直接相连的房间。
    public int roomSize;                                //就是tiles.Count。
    public bool isAccessibleFromMainRoom;               //是否能连接到主房间。
    public bool isMainRoom;                             //是否主房间（最大的房间）。

    readonly int[,] upDownLeftRight = new int[4, 2] { { 0, 1 }, { 0, -1 }, { -1, 0 }, { 1, 0 } };

    public Room() { }

    public Room(List<Coord> roomTiles, TileType[,] map)
    {
        tiles = roomTiles;
        roomSize = tiles.Count;
        connectedRooms = new List<Room>();
        UpdateEdgeTiles(map);
    }

    // 更新房间边缘瓦片集
    public void UpdateEdgeTiles(TileType[,] map)
    {
        edgeTiles.Clear();

        // 遍历上下左右四格，判断是否有墙
        foreach (Coord tile in tiles)
            for (int i = 0; i < 4; i++)
            {
                int x = tile.tileX + upDownLeftRight[i, 0];
                int y = tile.tileY + upDownLeftRight[i, 1];
                if (map[x, y] == TileType.Wall)
                {
                    edgeTiles.Add(tile);
                    continue;
                }
            }
    }

    //如果本身能连到主房间，标记其他相连的房间也能相连到主房间。
    public void MarkAccessibleFromMainRoom()
    {
        if (!isAccessibleFromMainRoom)
        {
            isAccessibleFromMainRoom = true;
            foreach (Room connectedRoom in connectedRooms)      //和他连一起的房间都能连到主房间。
                connectedRoom.MarkAccessibleFromMainRoom();
        }
    }

    // 连接房间
    public static void ConnectRooms(Room roomA, Room roomB)
    {
        //任何一个房间如果能连接到主房间，那另一个房间也能连到。
        if (roomA.isAccessibleFromMainRoom)
            roomB.MarkAccessibleFromMainRoom();
        else if (roomB.isAccessibleFromMainRoom)
            roomA.MarkAccessibleFromMainRoom();

        roomA.connectedRooms.Add(roomB);
        roomB.connectedRooms.Add(roomA);
    }

    // 是否连接另一个房间
    public bool IsConnected(Room otherRoom)
    {
        return connectedRooms.Contains(otherRoom);
    }

    // 比较房间大小
    public int CompareTo(Room otherRoom)
    {
        return otherRoom.roomSize.CompareTo(roomSize);
    }
}
