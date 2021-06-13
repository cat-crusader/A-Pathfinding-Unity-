using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using CodeMonkey.Utils;

public class PathfindingMultithreaded : MonoBehaviour
{

    public static PathfindingMultithreaded instance;
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 10;

    public bool[,] isWalkableGrid;

    public void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
            //float startTime = Time.realtimeSinceStartup;
            //int findPathJobCount=50;
            //NativeArray<JobHandle> jobHandleArray = new NativeArray<JobHandle>(findPathJobCount, Allocator.TempJob);
            // bool[,] OldGrid = isWalkableGrid;
            //for (int i = 0; i < findPathJobCount; i++)
            //{
            //FindPathJob findPathJob = new FindPathJob {
            //    startPosition = new int2(0, 0),
            //    endPosition = new int2(19, 19),
            //};
            //    jobHandleArray[i] = findPathJob.Schedule();
            //    //FindPath(new int2(0, 0), new int2(19, 19));
            //}

        //JobHandle.CompleteAll(jobHandleArray);
        //jobHandleArray.Dispose();

        //Debug.Log("Multithread Time " + ((Time.realtimeSinceStartup - startTime) * 1000f));


    }
    public bool[,] copyBoolArray(bool[,]arr)
    {
        return arr;
    }
    public void FindPathMultithreaded(int startX,int startY,int endX,int endY, bool[,]grid)
    {
        int findPathJobCount = 50;
        NativeArray<JobHandle> jobHandleArray = new NativeArray<JobHandle>(findPathJobCount, Allocator.TempJob);
        bool[,] OldGrid = isWalkableGrid;
        for (int i = 0; i < findPathJobCount; i++)
        {
            FindPathJob findPathJob = new FindPathJob
            {
                startPosition = new int2(0, 0),
                endPosition = new int2(19, 19)
            };
            jobHandleArray[i] = findPathJob.Schedule();
            //FindPath(new int2(0, 0), new int2(19, 19));
        }

        JobHandle.CompleteAll(jobHandleArray);
        jobHandleArray.Dispose();
    }
    [BurstCompile]
    private struct FindPathJob : IJob 
    {

        public int2 startPosition;
        public int2 endPosition;
        public void Execute()
        {
            int2 gridSize = new int2(20, 20);
            NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.Temp);

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    PathNode pathNode = new PathNode();
                    pathNode.x = x;
                    pathNode.y = y;
                    pathNode.index = CalculateIndex(x, y, gridSize.x);

                    pathNode.gCost = int.MaxValue;
                    pathNode.hCost = CalculateDistanceCost(new int2(x, y), endPosition);
                    pathNode.CalculateFCost();

                    pathNode.isWalkable = true;
                    pathNode.cameFromNodeIndex = -1;

                    pathNodeArray[pathNode.index] = pathNode;

                }
            }
            //{//test//placing walls
            //    PathNode walkablePathNode = pathNodeArray[CalculateIndex(1,0,gridSize.x)];
            //    walkablePathNode.SetIsWalkable(false);
            //    pathNodeArray[CalculateIndex(1, 0, gridSize.x)].SetIsWalkable(false);

            //}
            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Unity.Collections.Allocator.Temp);

            neighbourOffsetArray[0] = new int2(-1, 0);//left
            neighbourOffsetArray[1] = new int2(+1, 0);//right
            neighbourOffsetArray[2] = new int2(0, +1);//up
            neighbourOffsetArray[3] = new int2(0, -1);//down

            neighbourOffsetArray[4] = new int2(-1, -1);//left down
            neighbourOffsetArray[5] = new int2(-1, +1);//left up
            neighbourOffsetArray[6] = new int2(+1, -1);//right down
            neighbourOffsetArray[7] = new int2(+1, +1);//right    



            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

            PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);
            while (openList.Length > 0)
            {
                int currentNodeIndex = GetLowestFCostNodeIndex(openList, pathNodeArray);
                PathNode currentNode = pathNodeArray[currentNodeIndex];
                if (currentNodeIndex == endNodeIndex)
                {
                    //reached our destination
                    break;
                }

                for (int i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }
                closedList.Add(currentNodeIndex);

                for (int i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    int2 neigbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.x + neigbourOffset.x, currentNode.y + neigbourOffset.y);

                    if (!isPositionInsideGrid(neighbourPosition, gridSize))
                    {
                        continue;
                    }

                    int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.y);
                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        continue;
                    }

                    PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable)
                    {
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);

                    int tentaiveGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
                    if (tentaiveGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentaiveGCost;
                        neighbourNode.CalculateFCost();
                        pathNodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.index))
                        {
                            openList.Add(neighbourNode.index);
                        }
                    }
                }
            }
            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1)
            {//not find
                Debug.Log("Didnt find the path");
            }
            else
            {//find
                NativeList<int2> path = CalculatePath(pathNodeArray, endNode);
                //foreach (int2 pathPosition in path)
                //{
                //    Debug.Log(pathPosition);
                //}
                path.Dispose();
            }

            pathNodeArray.Dispose();
            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();

        }
        private NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
        {
            if (endNode.cameFromNodeIndex == -1)
            {
                return new NativeList<int2>(Allocator.Temp);
            }
            else
            {
                NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
                path.Add(new int2(endNode.x, endNode.y));

                PathNode currentNode = endNode;
                while (currentNode.cameFromNodeIndex != -1)
                {
                    PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                    path.Add(new int2(cameFromNode.x, cameFromNode.y));
                    currentNode = cameFromNode;
                }
                return path;
            }
        }
        private bool isPositionInsideGrid(int2 gridPosition, int2 gridSize)
        {
            return gridPosition.x >= 0 &&
                   gridPosition.y >= 0 &&
                   gridPosition.x < gridSize.x &&
                   gridPosition.y < gridSize.y;
        }
        private int CalculateIndex(int x, int y, int gridWidth)
        {
            return x + y * gridWidth;

        }
        private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
        {
            int xDistance = math.abs(aPosition.x - bPosition.x);
            int yDistance = math.abs(aPosition.y - bPosition.y);
            int remaining = math.abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }
        private int GetLowestFCostNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
        {
            PathNode lowestCostPathNode = pathNodeArray[openList[0]];
            for (int i = 1; i < openList.Length; i++)
            {
                PathNode testPathNode = pathNodeArray[openList[i]];
                if (testPathNode.fCost < lowestCostPathNode.fCost)
                {
                    lowestCostPathNode = testPathNode;
                }
            }
            return lowestCostPathNode.index;

        }
    }
    private void FindPath(int2 startPosition, int2 endPosition)
    {
        int2 gridSize = new int2(20, 20);
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.Temp);

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                PathNode pathNode = new PathNode();
                pathNode.x = x;
                pathNode.y = y;
                pathNode.index = CalculateIndex(x, y, gridSize.x);

                pathNode.gCost = int.MaxValue;
                pathNode.hCost = CalculateDistanceCost(new int2(x, y), endPosition);
                pathNode.CalculateFCost();

                pathNode.isWalkable = true;
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.index] = pathNode;

            }
        }
        //{//test//placing walls
        //    PathNode walkablePathNode = pathNodeArray[CalculateIndex(1,0,gridSize.x)];
        //    walkablePathNode.SetIsWalkable(false);
        //    pathNodeArray[CalculateIndex(1, 0, gridSize.x)].SetIsWalkable(false);

        //}
        NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(new int2[] {

        new int2(-1,0),//left
        new int2(+1,0),//right
        new int2(0,+1),//up
        new int2(0,-1),//down

        new int2(-1,-1),//left down
        new int2(-1,+1),//left up
        new int2(+1,-1),//right down
        new int2(+1,+1),//right    

        }, Allocator.Temp);

        int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

        PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
        startNode.gCost = 0;
        startNode.CalculateFCost();
        pathNodeArray[startNode.index] = startNode;

        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

        openList.Add(startNode.index);
        while (openList.Length > 0)
        {
            int currentNodeIndex = GetLowestFCostNodeIndex(openList, pathNodeArray);
            PathNode currentNode = pathNodeArray[currentNodeIndex];
            if (currentNodeIndex == endNodeIndex)
            {
                //reached our destination
                break;
            }

            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i] == currentNodeIndex)
                {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }
            closedList.Add(currentNodeIndex);

            for (int i = 0; i < neighbourOffsetArray.Length; i++)
            {
                int2 neigbourOffset = neighbourOffsetArray[i];
                int2 neighbourPosition = new int2(currentNode.x + neigbourOffset.x, currentNode.y + neigbourOffset.y);

                if (!isPositionInsideGrid(neighbourPosition, gridSize))
                {
                    continue;
                }

                int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.y);
                if (closedList.Contains(neighbourNodeIndex))
                {
                    continue;
                }

                PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                if (!neighbourNode.isWalkable)
                {
                    continue;
                }

                int2 currentNodePosition = new int2(currentNode.x, currentNode.y);

                int tentaiveGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
                if (tentaiveGCost < neighbourNode.gCost)
                {
                    neighbourNode.cameFromNodeIndex = currentNodeIndex;
                    neighbourNode.gCost = tentaiveGCost;
                    neighbourNode.CalculateFCost();
                    pathNodeArray[neighbourNodeIndex] = neighbourNode;

                    if (!openList.Contains(neighbourNode.index))
                    {
                        openList.Add(neighbourNode.index);
                    }
                }
            }
        }
        PathNode endNode = pathNodeArray[endNodeIndex];
        if (endNode.cameFromNodeIndex == -1)
        {//not find
            Debug.Log("Didnt find the path");
        }
        else
        {//find
            NativeList<int2> path = CalculatePath(pathNodeArray, endNode);
            //foreach (int2 pathPosition in path)
            //{
            //    Debug.Log(pathPosition);
            //}
            path.Dispose();
        }

        pathNodeArray.Dispose();
        neighbourOffsetArray.Dispose();
        openList.Dispose();
        closedList.Dispose();

    }
    private NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            return new NativeList<int2>(Allocator.Temp);
        }
        else
        {
            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
            path.Add(new int2(endNode.x, endNode.y));

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1)
            {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(new int2(cameFromNode.x, cameFromNode.y));
                currentNode = cameFromNode;
            }
            return path;
        }
    }
    private bool isPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return gridPosition.x >= 0 &&
               gridPosition.y >= 0 &&
               gridPosition.x < gridSize.x &&
               gridPosition.y < gridSize.y;
    }
    private int CalculateIndex(int x, int y, int gridWidth)
    {
        return x + y * gridWidth;

    }
    private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
    {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }
    private int GetLowestFCostNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
    {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++)
        {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;

    }
    private struct PathNode 
    {

        //private Grid<PathNode> grid;
        public int x;
        public int y;

        public int index;

        public int gCost;
        public int hCost;
        public int fCost;

        public bool isWalkable;

        public int cameFromNodeIndex;

        public void SetIsWalkable(bool IsWalkable)
        {
            this.isWalkable = IsWalkable;
        }
        public void CalculateFCost()
        {
            fCost = gCost+hCost;
        }
    }

}
