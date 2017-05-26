using UnityEngine;
using System.Collections;
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

        //平滑渲染地图。
        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(map, 1);
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

}