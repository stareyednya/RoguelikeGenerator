using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockKeyPlacer
{
    // Index for which pair we're currently placing.
    private int currentLock = 0;

    CellS currentLockCell;
    CellS currentKeyCell;

    public LockKeyPlacer(int noOfLocks)
    {
        int currentRoomNo = 0;
        if (noOfLocks != 0)
        {
            // Place first lock and key randomly to start off the loop. 
          
            do
            {
                currentRoomNo = PlaceLocksInRoom();
                MazeUtils.OpenDoorway(currentLockCell);
            } while (!PathToSpawn());
            currentLock++;

            //List<CellS> bestPath = Pathfinding.BuildPath(RoguelikeGenerator.instance.playerSpawn);
            //foreach (CellS c in bestPath)
            //{
            //    Debug.Log(string.Format("{0}, {1}", c.gridPos.x, c.gridPos.y));
            //}
            //Debug.Log(string.Format("First lock no: {0}", currentLockCell.roomNo));
            //Debug.Log(string.Format("First key no: {0}", currentKeyCell.roomNo));
            RoguelikeGenerator.instance.adjCells.Remove(currentLockCell);
            //  Debug.Log(string.Format("Removing cell {0}, {1}", currentLockCell.gridPos.x, currentLockCell.gridPos.y));
            RoguelikeGenerator.instance.rooms[currentLockCell.roomNo - 1].hasLock = true;
            RoguelikeGenerator.instance.rooms[currentKeyCell.roomNo - 1].hasKey = true;
            // also mark the room with the first key as being locked to prevent it from becoming barred off. 
            RoguelikeGenerator.instance.rooms[currentKeyCell.roomNo - 1].hasLock = true;
        }
       

        // Open the adjacent wall to the corridor to make a doorway.
       // MazeUtils.OpenDoorway(currentLockCell);


        // Create a loop from then on.
        int nextRoomNo;
        for (int i = 0; i < noOfLocks - 1; i++)
        {
            do
            {
                nextRoomNo = PlaceLocksInGivenRoom(currentRoomNo);
                MazeUtils.OpenDoorway(currentLockCell);
            } while (!PathToSpawn());
            currentRoomNo = nextRoomNo;
            //Debug.Log(string.Format("Next lock no: {0}", currentLockCell.roomNo));
           // Debug.Log(string.Format("Next key no: {0}", currentKeyCell.roomNo));
            currentLock++;
            RoguelikeGenerator.instance.adjCells.Remove(currentLockCell);
            //Debug.Log(string.Format("Removing cell {0}, {1}", currentLockCell.gridPos.x, currentLockCell.gridPos.y));
            RoguelikeGenerator.instance.rooms[currentLockCell.roomNo - 1].hasLock = true;
            RoguelikeGenerator.instance.rooms[currentKeyCell.roomNo - 1].hasKey = true;
            MazeUtils.OpenDoorway(currentLockCell);
        }

        
        

        //for (int i = 0; i < noOfLocks; i++)
        //{
        //    do
        //    {
        //        PlaceLocks();
        //    } while (!PathToSpawn());
        //    currentLock++;
        //}
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
        //CellS lockCell;
        do
        {
            currentLockCell = RoguelikeGenerator.instance.adjCells[Random.Range(0, RoguelikeGenerator.instance.adjCells.Count)];
        } while (RoguelikeGenerator.instance.rooms[currentLockCell.roomNo - 1].hasLock);
        
        
        // Remove this adjacent cell from consideration so locks dont overlap.
        //RoguelikeGenerator.instance.adjCells.Remove(lockCell);
        //Debug.Log(string.Format("Removing cell {0}, {1}", lockCell.gridPos.x, lockCell.gridPos.y));
        //RoguelikeGenerator.instance.rooms[lockCell.roomNo - 1].hasLock = true;


        // Choose a random room from those available that don't already hold a key, then a random cell in it. 
        Room spawnRoom;
        do
        {
            spawnRoom = RoguelikeGenerator.instance.rooms[Random.Range(0, RoguelikeGenerator.instance.rooms.Count)];
        } while (spawnRoom.hasKey);

        CellS keyCell = spawnRoom.roomCells[Random.Range(0, spawnRoom.roomCells.Count)];
        spawnRoom.hasKey = true;
        RoguelikeGenerator.instance.lockKeySpawns[currentLock] = new LockAndKey(currentLockCell, keyCell);
    }

    // Test to return room number of the room the lock was placed in.
    public int PlaceLocksInRoom()
    {
        // Place first key in a random room. 

        Room spawnRoom;
        do
        {
            spawnRoom = RoguelikeGenerator.instance.rooms[Random.Range(0, RoguelikeGenerator.instance.rooms.Count)];
        } while (spawnRoom.hasKey);

        //CellS keyCell;
        do
        {
            currentKeyCell = spawnRoom.roomCells[Random.Range(0, spawnRoom.roomCells.Count)];
        } while (!validKeyCell(currentKeyCell));

        // Place first lock in a random room. 
        do
        {
            currentLockCell = RoguelikeGenerator.instance.adjCells[Random.Range(0, RoguelikeGenerator.instance.adjCells.Count)];
        } while (RoguelikeGenerator.instance.rooms[currentLockCell.roomNo - 1].hasLock || currentLockCell.roomNo == currentKeyCell.roomNo);



        // Choose a random tile that connects a room to a corridor and place the lock there. 
        // CellS lockCell;
        //do
        //{
        //    currentLockCell = RoguelikeGenerator.instance.adjCells[Random.Range(0, RoguelikeGenerator.instance.adjCells.Count)];
        //} while (RoguelikeGenerator.instance.rooms[currentLockCell.roomNo - 1].hasLock);


        //// Remove this adjacent cell from consideration so locks dont overlap.
        ////RoguelikeGenerator.instance.adjCells.Remove(lockCell);
        ////Debug.Log(string.Format("Removing cell {0}, {1}", lockCell.gridPos.x, lockCell.gridPos.y));
        ////RoguelikeGenerator.instance.rooms[lockCell.roomNo - 1].hasLock = true;


        //// Choose a random room from those available that don't already hold a key, then a random cell in it. 
        //Room spawnRoom;
        //do
        //{
        //    spawnRoom = RoguelikeGenerator.instance.rooms[Random.Range(0, RoguelikeGenerator.instance.rooms.Count)];
        //} while (spawnRoom.hasKey);

        ////CellS keyCell;
        //do
        //{
        //    currentKeyCell = spawnRoom.roomCells[Random.Range(0, spawnRoom.roomCells.Count)];
        //} while (!validKeyCell(currentKeyCell));
       
        //spawnRoom.hasKey = true;
        RoguelikeGenerator.instance.lockKeySpawns[currentLock] = new LockAndKey(currentLockCell, currentKeyCell);

        return currentLockCell.roomNo;
    }

    public int PlaceLocksInGivenRoom(int roomNo)
    {
        // Place this next key in the room of the previous lock. 

        Room keyRoom;
        keyRoom = RoguelikeGenerator.instance.rooms[roomNo - 1];
        do
        {
            currentKeyCell = keyRoom.roomCells[Random.Range(0, keyRoom.roomCells.Count)];
        } while (!validKeyCell(currentKeyCell));

        // Place the next lock in a random room that doesn't already have a lock or key. 
        do
        {
            currentLockCell = RoguelikeGenerator.instance.adjCells[Random.Range(0, RoguelikeGenerator.instance.adjCells.Count)];
        } while (RoguelikeGenerator.instance.rooms[currentLockCell.roomNo - 1].hasLock);


        // Choose a random tile that connects a room to a corridor and place the lock there. 
        //CellS lockCell;
       // do
       // {
       //     currentLockCell = RoguelikeGenerator.instance.adjCells[Random.Range(0, RoguelikeGenerator.instance.adjCells.Count)];
       // } while (RoguelikeGenerator.instance.rooms[currentLockCell.roomNo - 1].hasLock);


       // // Remove this adjacent cell from consideration so locks dont overlap.
       // //RoguelikeGenerator.instance.adjCells.Remove(lockCell);
       // //Debug.Log(string.Format("Removing cell {0}, {1}", lockCell.gridPos.x, lockCell.gridPos.y));
       // //RoguelikeGenerator.instance.rooms[lockCell.roomNo - 1].hasLock = true;

       // // Place the key at a random point in the given room. 
       //// Room keyRoom = RoguelikeGenerator.instance.rooms[roomNo - 1];
       // //CellS keyCell = keyRoom.roomCells[Random.Range(0, keyRoom.roomCells.Count)];

       // // Choose a random room from those available that don't already hold a key, then a random cell in it. 
       // Room spawnRoom;
       // do
       // {
       //     spawnRoom = RoguelikeGenerator.instance.rooms[Random.Range(0, RoguelikeGenerator.instance.rooms.Count)];
       // } while (spawnRoom.hasKey);

       // //CellS keyCell;
       // do
       // {
       //     currentKeyCell = spawnRoom.roomCells[Random.Range(0, spawnRoom.roomCells.Count)];
       // } while (!validKeyCell(currentKeyCell));

        //spawnRoom.hasKey = true;
        RoguelikeGenerator.instance.lockKeySpawns[currentLock] = new LockAndKey(currentLockCell, currentKeyCell);

        // Return the next room to place the key behind. 
        return currentLockCell.roomNo;
    }

    // Helper to determine if the chosen key cell already holds something.
    public bool validKeyCell(CellS keyCell)
    {
        foreach (LockAndKey lk in RoguelikeGenerator.instance.lockKeySpawns)
        {
            if (lk != null)
            {
                if (lk.lockSpawn == keyCell || RoguelikeGenerator.instance.adjCells.Contains(keyCell))
                {
                    return false;
                }
            }
           
        }
        return true;
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
            //Debug.Log("No valid path found. Replacing key");
            RoguelikeGenerator.instance.replacementCount++;
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
