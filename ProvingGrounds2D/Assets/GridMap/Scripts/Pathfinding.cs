using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding 
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    public Grid<PathNode> grid;
    private List<PathNode> openList;
    private List<PathNode> closedList;



    public Pathfinding(int width, int height)
    {
        grid = new Grid<PathNode>(width,height,10f,Vector3.zero,(Grid<PathNode>g,int x,int y)=>new PathNode(g,x,y));
    }

    public PathNode GetNode(int x,int y)
    {
        return grid.GetGridObject(x, y);
    }
    public List<PathNode> FindPath(int startX, int startY, int endX, int endY)
    {

        PathNode startNode = grid.GetGridObject(startX,startY);
        PathNode endNode = grid.GetGridObject(endX,endY);


        openList = new List<PathNode> {startNode };
        closedList = new List<PathNode>();

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                PathNode pathNode = grid.GetGridObject(x,y);
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode.cameFromNode = null;
            }
        }

        startNode.gCost = 0;
        startNode.hCost = CalculateDisctanceCost(startNode,endNode);
        startNode.CalculateFCost();

        while (openList.Count>0)
        {
            PathNode currentNode = GetLowestFCostNode(openList);
            if (currentNode == endNode)
            {
                return CalculatedPath(endNode);
            }
            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (PathNode neighbourNode in GetNeighbourList(currentNode))
            {
                if (closedList.Contains(neighbourNode)) continue;
                if (!neighbourNode.isWalkable)
                {
                    closedList.Add(neighbourNode);
                    continue;
                }
                int tentativeGCost = currentNode.gCost + CalculateDisctanceCost(currentNode,neighbourNode);
                if (tentativeGCost<neighbourNode.gCost)
                {
                    neighbourNode.cameFromNode = currentNode;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = CalculateDisctanceCost(neighbourNode,endNode);
                    neighbourNode.CalculateFCost();
                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }

        }
        //Out of nodes on the openList;
        //Cant find way(there are no way)
        return null;
    }

    private List<PathNode> GetNeighbourList(PathNode currentNode)
    {
        List<PathNode> neighbourList = new List<PathNode>();
        if (currentNode.x-1>=0)
        {
            neighbourList.Add(grid.GetGridObject(currentNode.x-1, currentNode.y));

            if (currentNode.y - 1 >= 0)neighbourList.Add(grid.GetGridObject(currentNode.x - 1, currentNode.y-1));

            if (currentNode.y + 1 <grid.GetHeight()) neighbourList.Add(grid.GetGridObject(currentNode.x - 1, currentNode.y + 1));
        }
        if (currentNode.x+1<grid.GetWidth())
        {
            neighbourList.Add(grid.GetGridObject(currentNode.x + 1, currentNode.y));

            if (currentNode.y - 1 >= 0) neighbourList.Add(grid.GetGridObject(currentNode.x + 1, currentNode.y - 1));

            if (currentNode.y + 1 < grid.GetHeight()) neighbourList.Add(grid.GetGridObject(currentNode.x + 1, currentNode.y + 1));
        }
        if (currentNode.y - 1 >= 0) neighbourList.Add(grid.GetGridObject(currentNode.x,currentNode.y-1));
        if (currentNode.y+1<grid.GetHeight()) neighbourList.Add(grid.GetGridObject(currentNode.x, currentNode.y + 1));
        return neighbourList;
    }
    private List<PathNode> CalculatedPath(PathNode endNode)
    {
        List<PathNode> path = new List<PathNode>();
        path.Add(endNode);
        PathNode currentNode = endNode;
        while (currentNode.cameFromNode!=null)
        {
            path.Add(currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }
        path.Reverse();
        return path;
    }
    private int CalculateDisctanceCost(PathNode a,PathNode b)
    {

        int xDistance = Mathf.Abs(a.x -b.x);
        int YDistance = Mathf.Abs(a.y-b.y);
        int remaining = Mathf.Abs(xDistance - YDistance);
        return MOVE_DIAGONAL_COST * Mathf.Min(xDistance,YDistance)+MOVE_STRAIGHT_COST*remaining;
    }

    private PathNode GetLowestFCostNode(List<PathNode>pathNodeList)
    {
        PathNode lowestFCostNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }

}
