using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockKeyPlacer
{
    // Index for which pair we're currently placing.
    private int currentLock = 0;

    public LockKeyPlacer(int noOfLocks)
    {
        for (int i = 0; i < noOfLocks; i++)
        {
            do
            {
                PlaceLocks();
            } while (!PathToSpawn());
            currentLock++;
        }
    }

    public LockKeyPlacer(int noOfLocks, bool b)
    {
        for (int i = 0; i < noOfLocks; i++)
        {
            PlaceLocksInPool();
            currentLock++;
        }
    }

    public void PlaceLocks()
    {
        // Choose a random tile that connects a room to a corridor and place the lock there. 
        CellS lockCell = RoguelikeGenerator.instance.adjCells[Random.Range(0, RoguelikeGenerator.instance.adjCells.Count)];
        // Remove this adjacent cell from consideration so locks dont overlap.
        RoguelikeGenerator.instance.adjCells.Remove(lockCell);


        // Choose a random room from those available that don't already hold a key, then a random cell in it. 
        Room spawnRoom;
        do
        {
            spawnRoom = RoguelikeGenerator.instance.rooms[Random.Range(0, RoguelikeGenerator.instance.rooms.Count)];
        } while (spawnRoom.hasKey);

        CellS keyCell = spawnRoom.roomCells[Random.Range(0, spawnRoom.roomCells.Count)];
        spawnRoom.hasKey = true;
        RoguelikeGenerator.instance.lockKeySpawns[currentLock] = new LockAndKey(lockCell, keyCell);
    }

    public void PlaceLocksInPool()
    {
        Cell lockCell;
        Cell keyCell;
        Room spawnRoom;
        do
        {
            // Choose a random tile that connects a room to a corridor and place the lock there. 
            lockCell = RoguelikeGenerator.instance.adjCellsInstances[Random.Range(0, RoguelikeGenerator.instance.adjCellsInstances.Count)];
            // Choose a random room from those available that don't already hold a key, then a random cell in it. 
            do
            {
                spawnRoom = RoguelikeGenerator.instance.rooms[Random.Range(0, RoguelikeGenerator.instance.rooms.Count)];
            } while (spawnRoom.hasKey);



            keyCell = spawnRoom.roomCellsInstantiated[Random.Range(0, spawnRoom.roomCellsInstantiated.Count)];
            

            RoguelikeGenerator.instance.lockKeySpawns[currentLock] = new LockAndKey(lockCell, keyCell);

        } while (!PathToSpawnInstance());
        // Remove this adjacent cell from consideration so locks dont overlap.
        spawnRoom.hasKey = true;
        RoguelikeGenerator.instance.adjCellsInstances.Remove(lockCell);
        GameObject.Instantiate(RoguelikeGenerator.instance.lockPrefabs[currentLock], lockCell.cellObject.transform.position, Quaternion.identity);
        GameObject.Instantiate(RoguelikeGenerator.instance.keyPrefabs[currentLock], keyCell.cellObject.transform.position, Quaternion.identity);
        

    }

    public bool PathToSpawn()
    {
        bool validKey = true;
        // Target position is the player's spawn - stored in playerSpawn already as a CellS 
        List<CellS> openList = new List<CellS>();
        List<CellS> closedList = new List<CellS>();
        // Add the starting position to the open list - the location of the key.
        CellS startCell = RoguelikeGenerator.instance.lockKeySpawns[currentLock].keySpawn;
        openList.Add(startCell);
        CellS currentCell = null;

        while (openList.Count > 0)
        {
            currentCell = Pathfinding.FindLowestFScore(openList);
            // Add the current cell to the closed list and remove it from the open list
            closedList.Add(currentCell);
            openList.Remove(currentCell);

            // Has the target been found? 
            if (closedList.Contains(RoguelikeGenerator.instance.playerSpawn))
            {
                break;
            }

            List<CellS> adjacentCells = Pathfinding.GetNeighbours(currentCell, currentLock);
            foreach (CellS c in adjacentCells)
            {
                // If this cell is already in the closed path, skip it because we know about it 
                if (closedList.Contains(c))
                    continue;

                // If this cell isn't in the open list, add it in for evaluation
                if (!openList.Contains(c))
                {
                    // Compute its g and h scores, set the parent
                    c.g = currentCell.g + 1;
                    c.h = (int)Pathfinding.ManhattanDistance(c, RoguelikeGenerator.instance.playerSpawn);
                    c.parent = currentCell;
                    openList.Add(c);
                }
                else if (c.h + currentCell.g + 1 < c.f)
                {
                    // If using the current g score make the f score lower, update the parent as its a better path.
                    c.parent = currentCell;
                }

            }

        }

        if (!closedList.Contains(RoguelikeGenerator.instance.playerSpawn))
        {
            Debug.Log("No valid path found. Replacing key");
            validKey = false;
        }


        return validKey;

    }

    public bool PathToSpawnInstance()
    {
        bool validKey = true;
        // Target position is the player's spawn - stored in playerSpawn already as a CellS 
        List<Cell> openList = new List<Cell>();
        List<Cell> closedList = new List<Cell>();
        // Add the starting position to the open list - the location of the key.
        Cell startCell = RoguelikeGenerator.instance.lockKeySpawns[currentLock].keyInstance;
        openList.Add(startCell);
        Cell currentCell = null;

        while (openList.Count > 0)
        {
            currentCell = Pathfinding.FindLowestFScore(openList);
            // Add the current cell to the closed list and remove it from the open list
            closedList.Add(currentCell);
            openList.Remove(currentCell);

            // Has the target been found? 
            if (closedList.Contains(RoguelikeGenerator.instance.playerSpawnInstance))
            {
                break;
            }

            List<Cell> adjacentCells = Pathfinding.GetNeighbours(currentCell, currentLock);
            foreach (Cell c in adjacentCells)
            {
                // If this cell is already in the closed path, skip it because we know about it 
                if (closedList.Contains(c))
                    continue;

                // If this cell isn't in the open list, add it in for evaluation
                if (!openList.Contains(c))
                {
                    // Compute its g and h scores, set the parent
                    c.g = currentCell.g + 1;
                    c.h = (int)Pathfinding.ManhattanDistance(c, RoguelikeGenerator.instance.playerSpawnInstance);
                    c.parent = currentCell;
                    openList.Add(c);
                }
                else if (c.h + currentCell.g + 1 < c.f)
                {
                    // If using the current g score make the f score lower, update the parent as its a better path.
                    c.parent = currentCell;
                }

            }

        }

        if (!closedList.Contains(RoguelikeGenerator.instance.playerSpawnInstance))
        {
            Debug.Log("No valid path found. Replacing key");
            validKey = false;
        }


        return validKey;

    }
}
