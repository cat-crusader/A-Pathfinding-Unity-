

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class Testing : MonoBehaviour 
{
    Pathfinding pathfinding;
    bool[,] grid;
    public bool isMultithreaded;
    private void Start()
    {
        pathfinding = new Pathfinding(20, 20);
        grid = new bool[20, 20];
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                grid[x, y] = true;
            }
        }

            //float startTime = Time.realtimeSinceStartup;
            //for (int i = 0; i < 50; i++)
            //{
            //    pathfinding.FindPath(0, 0, 19, 19);
            //}

            //Debug.Log("Singlethread Time " + ((Time.realtimeSinceStartup - startTime) * 1000f));

    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
           
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            pathfinding.grid.GetXY(mouseWorldPosition, out int x, out int y);
            if (!isMultithreaded)
            {             
                float debugTimeStart = Time.realtimeSinceStartup;
                List<PathNode> path=new List<PathNode>();
                for (int i = 0; i < 50; i++)
                {
                     path = pathfinding.FindPath(0, 0, x, y);
                }
                float debugTimeTaken = (Time.realtimeSinceStartup - debugTimeStart) * 1000f;
                Debug.Log("Singlethread Duration in milliseconds: " + debugTimeTaken);
                if (path != null)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Debug.DrawLine(new Vector3(path[i].x, path[i].y) * 10f + Vector3.one * 5f, new Vector3(path[i + 1].x, path[i + 1].y) * 10f + Vector3.one * 5f, Color.green, 5f);
                    }
                }
            }
            else 
            {
                float debugTimeStart = Time.realtimeSinceStartup;
                PathfindingMultithreaded.instance.FindPathMultithreaded(0,0,x,y,grid);
                float debugTimeTaken = (Time.realtimeSinceStartup - debugTimeStart) * 1000f;
                Debug.Log("Multithread Duration in milliseconds: " + debugTimeTaken);
            }
        }
        //if (Input.GetMouseButtonDown(1))
        //{
        //    Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
        //    pathfinding.grid.GetXY(mouseWorldPosition, out int x, out int y);
        //    pathfinding.GetNode(x, y).SetIsWalkable(!pathfinding.GetNode(x, y).isWalkable);
        //    Debug.DrawLine(pathfinding.grid.GetWorldPosition(x, y), pathfinding.grid.GetWorldPosition(x+1, y+1), Color.red, 1000f);
        //    Debug.DrawLine(pathfinding.grid.GetWorldPosition(x+1, y), pathfinding.grid.GetWorldPosition(x, y + 1), Color.red, 1000f);
        //    //Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

        //}
    }
}
