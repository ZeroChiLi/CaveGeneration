using System;
using System.Collections.Generic;

class Room : IComparable<Room>
{
    public List<Coord> tiles;                           //所有坐标。
    public List<Coord> edgeTiles;                       //靠边的坐标。
    public List<Room> connectedRooms;                   //与其直接相连的房间。
    public int roomSize;                                //就是tiles.Count。
    public bool isAccessibleFromMainRoom;               //是否能连接到主房间。
    public bool isMainRoom;                             //是否主房间（最大的房间）。

    public Room() { }

    public Room(List<Coord> roomTiles, int[,] map)
    {
        tiles = roomTiles;
        roomSize = tiles.Count;
        connectedRooms = new List<Room>();

        edgeTiles = new List<Coord>();
        foreach (Coord tile in tiles)                   //遍历每个坐标上下左右，判断是否有墙
            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    if ((map[x, y] == 1) && (x == tile.tileX || y == tile.tileY))
                    {
                        edgeTiles.Add(tile);
                        continue;
                    }
    }

    //如果本身能连到主房间，设置其他相连的房间能相连到主房间。
    public void SetAccessibleFromMainRoom()
    {
        if (!isAccessibleFromMainRoom)
        {
            isAccessibleFromMainRoom = true;
            foreach (Room connectedRoom in connectedRooms)      //和他连一起的房间都能连到主房间。
                connectedRoom.SetAccessibleFromMainRoom();
        }
    }

    //连接房间。
    public static void ConnectRooms(Room roomA, Room roomB)
    {
        //任何一个房间如果能连接到主房间，那另一个房间也能连到。
        if (roomA.isAccessibleFromMainRoom)
            roomB.SetAccessibleFromMainRoom();
        else if (roomB.isAccessibleFromMainRoom)
            roomA.SetAccessibleFromMainRoom();

        roomA.connectedRooms.Add(roomB);
        roomB.connectedRooms.Add(roomA);
    }

    public bool IsConnected(Room otherRoom)
    {
        return connectedRooms.Contains(otherRoom);
    }

    //比较房间大小。
    public int CompareTo(Room otherRoom)
    {
        return otherRoom.roomSize.CompareTo(roomSize);
    }
}
