using UnityEngine;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour
{
    public MeshFilter cave;                                 //渲染表层。
    public MeshFilter walls;                                //渲染墙的网格。

    public MeshCollider wallCollider;                       //墙体的Mesh Collider。
    public int tileAmount = 10;                             //渲染瓦片数量。

    public bool is2D;                                       //是否使用2D模式。

    //表层的洞穴渲染。
    public SquareGrid squareGrid;
    List<Vector3> vertices = new List<Vector3>();           //所有点的位置。
    List<int> triangles = new List<int>();                  //所有三角形，每连续三个点为一个三角形。

    //Key是顶点，Value所有含有这个顶点的三角形。
    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();

    //所有外边，每一条外边是由一堆点组成一个闭合圈（第一个和最后一个点相同）。
    List<List<int>> outlines = new List<List<int>>();

    HashSet<int> checkedVertices = new HashSet<int>();          //存放已经检查过的点。

    public void GenerateMesh(int[,] map, float squareSize)
    {
        //清空所有队列，字典，哈希表。因为每次生成新地图都要清空。
        #region Clear All List & Dictionary & HashSet
        vertices.Clear();
        triangles.Clear();
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();
        #endregion

        squareGrid = new SquareGrid(map, squareSize);

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
                TriangulateSquare(squareGrid.squares[x, y]);    //把所有立方体重新组成比较流畅的多边体。

        SetCaveMesh(map.GetLength(0) * squareSize);             //给Cave添加mesh。

        CalculateMeshOutlines();                                //计算所有需要渲染的外边。

        AddBorderLine();                                        //添加最外边。

        if (is2D)
            Generate2DColliders();                              //生成2D轮廓碰撞框。
        else
            CreateWallMesh();                                   //渲染墙。
    }

    //更新新的Mesh到Cave.mesh上。
    void SetCaveMesh(float meshSize)
    {
        cave.mesh = new Mesh();
        cave.mesh.vertices = vertices.ToArray();
        cave.mesh.triangles = triangles.ToArray();
        cave.mesh.RecalculateNormals();                         //重新计算法线。

        Vector2[] uvs = new Vector2[vertices.Count];            //渲染坐标。
        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-meshSize / 2, meshSize / 2, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-meshSize / 2, meshSize / 2, vertices[i].z) * tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        cave.mesh.uv = uvs;
    }

    //生成2D碰撞框架。
    void Generate2DColliders()
    {
        EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; i++)
            Destroy(currentColliders[i]);

        foreach (List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++)
                edgePoints[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].z);
            edgeCollider.points = edgePoints;
        }
    }

    //创建墙网格。
    void CreateWallMesh()
    {

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5;

        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;

                //一片墙的四个点。
                wallVertices.Add(vertices[outline[i]]);                                 // left
                wallVertices.Add(vertices[outline[i + 1]]);                             // right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);       // bottom left
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight);   // bottom right

                //一片墙的两个三角形。
                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;

        wallCollider.sharedMesh = wallMesh;
    }

    //把立方体们划成一堆三角形。
    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            // 1 points:
            case 1:
                MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                break;

            // 2 points:
            case 3:
                MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 6:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                break;
            case 5:
                MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // 3 point:
            case 7:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    //把一组点划成一堆三角形。
    void MeshFromPoints(params Node[] points)
    {
        //添加到新点到顶点列表（不重复）。
        for (int i = 0; i < points.Length; i++)
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);
    }

    //创建三角形，一个是包含所有三角形点的队列（triangles），还有是添加到字典三角形（triangle）。
    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    //添加（三角形顶点：Triangle结构）到字典里。
    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    //计算出所有外边。
    void CalculateMeshOutlines()
    {
        //计算房间们的外边。
        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            if (!checkedVertices.Contains(vertexIndex))                     //检测过的点就跳过
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)                                 //获取连接的下一个点
                {
                    checkedVertices.Add(vertexIndex);                       //设置为检测过

                    List<int> newOutline = new List<int>();                 //创建一条外边
                    newOutline.Add(vertexIndex);                            //添加原点到这个新外边

                    outlines.Add(newOutline);                               //所有外边集合里面加上这条新外边

                    FollowOutline(newOutlineVertex, outlines.Count - 1);    //根据这个新点继续查下一个外边点，里面就是递归了

                    outlines[outlines.Count - 1].Add(vertexIndex);          //最后把原点加上去，形成一个闭合的外边
                }
            }
    }

    //以vertex继续找下一个连接点。
    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);                            //把新点加到新边里面

        checkedVertices.Add(vertexIndex);                                   //标记为检测过

        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);       //继续找下一个外边点

        if (nextVertexIndex != -1)
            FollowOutline(nextVertexIndex, outlineIndex);                   //找到了下一个点就递归了
    }


    //如果找到外边，返回值是 原点（vertexIndex）连接到的下一个点，找不到返回-1。
    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex]; //获取所有含有原点三角形

        for (int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];
            for (int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))   //获取三角形内除了自己而且没检查过的点
                    if (IsOutlineEdge(vertexIndex, vertexB))                //判断能否组成外边
                        return vertexB;
            }
        }

        return -1;
    }

    //判断两个点是否能组成外边，原理就是这条边只被一个三角形占有。
    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];    //获取包含这个点的所有三角形
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))            //如果三角形同时包含这两个点
            {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1)                                //多于一个三角形同时包含这两个点，就不是外边了    
                {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

    //添加最外层边框线。
    void AddBorderLine()
    {
        int verticeIndex = vertices.Count;
        vertices.Add(squareGrid.squares[0, 0].bottomLeft.position);
        vertices.Add(squareGrid.squares[0, squareGrid.squares.GetLength(1) - 1].topLeft.position);
        vertices.Add(squareGrid.squares[squareGrid.squares.GetLength(0) - 1, squareGrid.squares.GetLength(1) - 1].topRight.position);
        vertices.Add(squareGrid.squares[squareGrid.squares.GetLength(0) - 1, 0].bottomRight.position);

        List<int> borderline = new List<int>();
        for (int i = 0; i < 4; ++i)
            borderline.Add(verticeIndex + i);
        borderline.Add(verticeIndex);
        outlines.Add(borderline);
    }

    //void OnDrawGizmos()
    //{
    //    Color wallColor = new Color(1, 181f / 255f, 21f / 255f);
    //    Color emptyColor = wallColor;
    //    //Color emptyColor = Color.white;
    //    Color nodeColor = new Color(82f / 255f, 120f / 255f, 252f / 255f);
    //    Color squareColor = new Color(0, 76f / 255f, 26f / 255f);

    //    if (squareGrid != null)
    //    {
    //        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
    //        {
    //            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
    //            {
    //                Gizmos.color = squareColor;
    //                Gizmos.DrawWireCube((squareGrid.squares[x, y].centreLeft.position + squareGrid.squares[x, y].centreRight.position) / 2, Vector3.one);

    //                Gizmos.color = (squareGrid.squares[x, y].topLeft.active) ? wallColor : emptyColor;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].topRight.active) ? wallColor : emptyColor;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].bottomRight.active) ? wallColor : emptyColor;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].bottomLeft.active) ? wallColor : emptyColor;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * .4f);


    //                Gizmos.color = nodeColor;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centreTop.position, Vector3.one * .15f);
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centreRight.position, Vector3.one * .15f);
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centreBottom.position, Vector3.one * .15f);
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centreLeft.position, Vector3.one * .15f);


    //            }
    //        }
    //    }
    //}

}