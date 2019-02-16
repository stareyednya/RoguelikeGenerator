using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MWRPooling
{
    // Grid pos to spawn pos for use with covering up dead ends.
    private Dictionary<Vector2, Vector2> spawns = new Dictionary<Vector2, Vector2>();
    // List of all cells found to be dead ends, i.e, 3 walls are active. 
    private List<Cell> deadEnds = new List<Cell>();
    private List<Cell> walls = new List<Cell>();
    private List<Cell> rooms = new List<Cell>();

    // How many cells TALL the maze will be.
    public int mazeRows;

    // how many cells WIDE the maze will be. 
    public int mazeColumns;

    // Cell size to determine how far apart to place cells during generation. 
    private float cellSize;
    

    // List of unvisited cells.
    private List<Cell> unvisited = new List<Cell>();

    // List to store cells being checked during generation for Recursive Backtracking: the 'stack'.
    private List<Cell> stack = new List<Cell>();

    // Holds the current cell and the next cell being checked.
    private Cell currentCell;
    private Cell checkCell;

    public MWRPooling(int gridRows, int gridColumns)
    {
        GenerateMaze(gridRows, gridColumns);
        PlacePlayerSpawn();
    }

    private void GenerateMaze(int rows, int columns)
    {
        mazeRows = rows;
        mazeColumns = columns;
        CreateLayout();
    }

    // Create the grid of cells. 
    public void CreateLayout()
    {
        // Determine the size of the cells to place from the tile we're using. 
        cellSize = RoguelikeGenerator.instance.cellPrefab.transform.localScale.x;

        // Set the starting point of our maze somewhere in the middle. 
        Vector2 startPos = new Vector2(-(cellSize * (mazeColumns / 2)) + (cellSize / 2), -(cellSize * (mazeRows / 2)) + (cellSize / 2));
        Vector2 spawnPos = startPos;

        for (int x = 0; x < mazeColumns; x++)
        {
            for (int y = 0; y < mazeRows; y++)
            {
                Vector2 gridPos = new Vector2(x, y);
                GenerateCell(spawnPos, gridPos);
                spawns[gridPos] = spawnPos;

                spawnPos.y += cellSize;
            }


            // Reset spawn position and move up a row. 
            spawnPos.y = startPos.y;
            spawnPos.x += cellSize;
        }

        // Choose a random cell to start from 
        int xStart = Random.Range(0, mazeColumns);
        int yStart = Random.Range(0, mazeRows);
        currentCell = RoguelikeGenerator.instance.cellsP[new Vector2(xStart, yStart)];

        // Mark our starting cell as visited by removing it from the unvisited list. 
        unvisited.Remove(currentCell);

        RoguelikeGenerator.instance.gridGenTime = Time.realtimeSinceStartup;
        RoguelikeGenerator.instance.totalGenTime = RoguelikeGenerator.instance.gridGenTime;

       
        RecursiveBacktracking();
        RoguelikeGenerator.instance.mazeGenTime = Time.realtimeSinceStartup - RoguelikeGenerator.instance.totalGenTime;
        RoguelikeGenerator.instance.totalGenTime += RoguelikeGenerator.instance.mazeGenTime;
        // Fill in the dead ends with some walls.
        FindDeadEnds();
        FillInEnds();
        RoguelikeGenerator.instance.sparseTime = Time.realtimeSinceStartup - RoguelikeGenerator.instance.totalGenTime;
        RoguelikeGenerator.instance.totalGenTime += RoguelikeGenerator.instance.sparseTime;
        // Remove some dead ends by making the maze imperfect.
        RemoveLoops();
        RoguelikeGenerator.instance.loopTime = Time.realtimeSinceStartup - RoguelikeGenerator.instance.totalGenTime;
        RoguelikeGenerator.instance.totalGenTime += RoguelikeGenerator.instance.loopTime;

        // Place rooms.
        PlaceRooms();
        RoguelikeGenerator.instance.roomTime = Time.realtimeSinceStartup - RoguelikeGenerator.instance.totalGenTime;
        RoguelikeGenerator.instance.totalGenTime += RoguelikeGenerator.instance.roomTime;
    }

    // Instantiate a cell based on the given position. 
    public void GenerateCell(Vector2 pos, Vector2 keyPos)
    {
        Cell newCell = new Cell();

        // Store a reference to this position in the grid.
        newCell.gridPos = keyPos;
        // Set and instantiate this cell's GameObject.
        newCell.cellObject = GameObject.Instantiate(RoguelikeGenerator.instance.cellPrefab, pos, RoguelikeGenerator.instance.cellPrefab.transform.rotation);
        // Child the new cell to the maze parent. 
        newCell.cellObject.transform.parent = RoguelikeGenerator.instance.mazeParent.transform;
        // Set the name of this cellObject.
        newCell.cellObject.name = "Cell - X:" + keyPos.x + " Y:" + keyPos.y;
        // Get reference to attached cell script.
        newCell.cScript = newCell.cellObject.GetComponent<CellScript>();

        // Add this cell to our lists. 
        RoguelikeGenerator.instance.cellsP[keyPos] = newCell;
        unvisited.Add(newCell);

        // POOLING ADDITIONS

        // Make a matching wall object and deactivate it. 
        Cell newWall = new Cell();
        newWall.gridPos = keyPos;
        // Set and instantiate this cell's GameObject.
        newWall.cellObject = GameObject.Instantiate(RoguelikeGenerator.instance.wallPrefab, pos, RoguelikeGenerator.instance.wallPrefab.transform.rotation);
        // Child the new cell to the maze parent. 
        newWall.cellObject.transform.parent = RoguelikeGenerator.instance.wallParent.transform;
        // Set the name of this cellObject.
        newWall.cellObject.name = "Wall - X:" + keyPos.x + " Y:" + keyPos.y;
        // Get reference to attached cell script.
        newWall.cScript = newWall.cellObject.GetComponent<CellScript>();
        newWall.cellObject.SetActive(false);
        RoguelikeGenerator.instance.wallsP[keyPos] = newWall;

        // Make a matching room object and deactivate it. 
        Cell newRoom = new Cell();
        newRoom.gridPos = keyPos;
        // Set and instantiate this cell's GameObject.
        newRoom.cellObject = GameObject.Instantiate(RoguelikeGenerator.instance.roomPrefab, pos, RoguelikeGenerator.instance.roomPrefab.transform.rotation);
        // Get reference to attached cell script.
        newRoom.cScript = newRoom.cellObject.GetComponent<CellScript>();
        newRoom.cellObject.SetActive(false);
        RoguelikeGenerator.instance.roomsP[keyPos] = newRoom;
    }

    public void RecursiveBacktracking()
    {
        // Loop while there are still cells in the grid we haven't visited. 
        while (unvisited.Count > 0)
        {
            List<Cell> unvisitedNeighbours = MazeUtils.GetUnvisitedNeighbours(currentCell, ref RoguelikeGenerator.instance.cellsP, ref unvisited);
            if (unvisitedNeighbours.Count > 0)
            {
                // Choose a random unvisited neighbour. 
                checkCell = unvisitedNeighbours[Random.Range(0, unvisitedNeighbours.Count)];
                // Add current cell to stack.
                stack.Add(currentCell);
                // Compare and remove walls if needed.
                MazeUtils.CompareWallsInPool(currentCell, checkCell);
                // Make our new current cell the neighbour we were checking. 
                currentCell = checkCell;
                // Mark our new current cell as visited. 
                unvisited.Remove(currentCell);
            }
            else if (stack.Count > 0)
            {
                // Make our current cell the most recently added cell from the stack.
                currentCell = stack[stack.Count - 1];
                // Remove it from the stack.
                stack.Remove(currentCell);
            }
        }
    }

    public void FindDeadEnds()
    {
        for (int x = 0; x < mazeColumns; x++)
        {
            for (int y = 0; y < mazeRows; y++)
            {
                Vector2 pos = new Vector2(x, y);
                if (RoguelikeGenerator.instance.cellsP.ContainsKey(pos))
                {
                    Cell testCell = RoguelikeGenerator.instance.cellsP[pos];
                    if (MazeUtils.NoOfWalls(testCell) >= 3 && !walls.Contains(testCell))
                    {
                        deadEnds.Add(testCell);
                    }
                }
            }
        }
    }

    public void FillInEnds()
    {
        // Keep a list of positions of cells to deactivate and replace with walls at the end- MUST be done at the end as maze generation relies on previous cells being present
        List<Vector2> posToReplace = new List<Vector2>();
        RoguelikeGenerator.instance.wallParent = new GameObject();
        RoguelikeGenerator.instance.wallParent.transform.position = Vector2.zero;
        RoguelikeGenerator.instance.wallParent.name = "Walls";
        for (int i = 0; i < RoguelikeGenerator.instance.sparseness; i++)
        {
            for (int j = 0; j < deadEnds.Count; j++)
            {
                Cell currentEnd = deadEnds[j];

                Vector2 direction = new Vector2(0, 0); // The direction vector this passage moves in. 

                // Find the open direction of this cell- the direction this passage is going in. 
                if (!currentEnd.cScript.wallD.activeInHierarchy)
                {
                    direction.y -= 1;
                }

                if (!currentEnd.cScript.wallL.activeInHierarchy)
                {
                    direction.x -= 1;
                }

                if (!currentEnd.cScript.wallR.activeInHierarchy)
                {
                    direction.x += 1;
                }

                if (!currentEnd.cScript.wallU.activeInHierarchy)
                {
                    direction.y += 1;
                }
                // Mark this cell as one to replace later 
                posToReplace.Add(currentEnd.gridPos);

                Vector2 nextPos = deadEnds[j].gridPos + direction; // The position of the next cell we're going to be moving to check. 

                // Follow along ths passageway until we are no longer filling in dead ends in this direction i.e current cell does not have 3 walls.
                while (MazeUtils.NoOfWalls(RoguelikeGenerator.instance.cellsP[nextPos]) == 3 && nextPos.x < mazeRows - 1 && nextPos.y < mazeColumns - 1 && nextPos.x > 0 && nextPos.y > 0)
                {
                    // Fill in the current cell if it hasn't been already.
                    if (!posToReplace.Contains(nextPos + direction))
                    {
                        posToReplace.Add(RoguelikeGenerator.instance.cellsP[nextPos + direction].gridPos);

                    }
                    // Move along the passageway.
                    nextPos += direction;
                }


                // Add a wall to the opposite direction on our current non-dead end cell- the way back into the passage we just filled in. 
                if (direction.y == -1)
                {
                    RoguelikeGenerator.instance.cellsP[nextPos].cScript.wallU.SetActive(true);
                }
                else if (direction.y == 1)
                {
                    RoguelikeGenerator.instance.cellsP[nextPos].cScript.wallD.SetActive(true);
                }
                else if (direction.x == -1)
                {
                    RoguelikeGenerator.instance.cellsP[nextPos].cScript.wallR.SetActive(true);
                }
                else if (direction.x == 1)
                {
                    RoguelikeGenerator.instance.cellsP[nextPos].cScript.wallL.SetActive(true);
                }

                // If our current cell now has 3 walls, it's a dead end, so update our dead end list.
                // else we're done with this dead end, so remove it.
                if (MazeUtils.NoOfWalls(RoguelikeGenerator.instance.cellsP[nextPos]) == 3)
                {
                    deadEnds[j] = RoguelikeGenerator.instance.cellsP[nextPos];
                }
                else
                {
                    deadEnds.RemoveAt(j);
                }
            }
        }
        
        // Go through list of positions to mark and activate all walls on that cell, then deactivate it, activate wall in that position
        foreach (Vector2 p in posToReplace)
        {
            RoguelikeGenerator.instance.cellsP[p].cScript.wallD.SetActive(true);
            RoguelikeGenerator.instance.cellsP[p].cScript.wallU.SetActive(true);
            RoguelikeGenerator.instance.cellsP[p].cScript.wallL.SetActive(true);
            RoguelikeGenerator.instance.cellsP[p].cScript.wallR.SetActive(true);

            RoguelikeGenerator.instance.cellsP[p].cellObject.SetActive(false);
            RoguelikeGenerator.instance.wallsP[p].cellObject.SetActive(true);
        }
    }

    public void RemoveLoops()
    {

        // Refind our dead ends since the passages have been filled in.
        deadEnds.Clear();
        FindDeadEnds();

        for (int i = 0; i < deadEnds.Count; i++)
        {
            bool hitCorridor = false;
            // Roll a number to determine if we're removing this dead end. 
            if (Random.Range(1, 101) <= RoguelikeGenerator.instance.removalChance)
            {
                Cell currentDeadEnd = deadEnds[i];
                // Find current dead end's valid neighbours- cells that are on the grid. 
                while (!hitCorridor)
                {
                    List<Cell> neighbours = new List<Cell>();
                    Vector2 currentPos = currentDeadEnd.gridPos;
                    // Try to not double back over the cell we just came from, as then will hit a corridor immediately and still have a dead end. 
                    int indexToSkip = 0;
                    if (!currentDeadEnd.cScript.wallD.activeInHierarchy)
                    {
                        indexToSkip = 3;
                    }
                    else if (!currentDeadEnd.cScript.wallL.activeInHierarchy)
                    {
                        indexToSkip = 0;
                    }

                    else if (!currentDeadEnd.cScript.wallR.activeInHierarchy)
                    {
                        indexToSkip = 1;
                    }

                    else if (!currentDeadEnd.cScript.wallU.activeInHierarchy)
                    {
                        indexToSkip = 2;
                    }

                    for (int j = 0; j < MazeUtils.possibleNeighbours.Length; j++)
                    {
                        if (j != indexToSkip)
                        {
                            // Find the position of a neighbour on the grid, relative to the current cell. 
                            Vector2 nPos = currentPos + MazeUtils.possibleNeighbours[j];
                            // Check the neighbouring cell exists. 
                            if (RoguelikeGenerator.instance.cellsP.ContainsKey(nPos))
                                neighbours.Add(RoguelikeGenerator.instance.cellsP[nPos]);
                        }
                    }

                    // Choose a random direction i.e. a random neighbour.
                    checkCell = neighbours[Random.Range(0, neighbours.Count)];
                    
                    // See if this spot has its wall tile active.
                    // If so, deactivate and compare the walls to remove it. 
                    if (RoguelikeGenerator.instance.wallsP[checkCell.gridPos].cellObject.activeInHierarchy)
                    {
                        RoguelikeGenerator.instance.wallsP[checkCell.gridPos].cellObject.SetActive(false);
                        RoguelikeGenerator.instance.cellsP[checkCell.gridPos].cellObject.SetActive(true);
                        // Remove walls between chosen neighbour and current dead end. 
                        MazeUtils.CompareWallsInPool(currentDeadEnd, checkCell);


                        currentDeadEnd = checkCell;
                    }
                    else
                    {
                        // Remove walls between chosen neighbour and current dead end. 
                        MazeUtils.CompareWallsInPool(currentDeadEnd, checkCell);
                        hitCorridor = true;
                    }
                }
            }
        }



    }

    public void PlaceRooms()
    {

        for (int i = 0; i < RoguelikeGenerator.instance.noOfRooms; i++)
        {
            Room r = new Room();
            r.roomNo = i + 1;

            // Set our best score to an arbitarily large number.
            int bestScore = int.MaxValue;
            Vector2 bestPos = new Vector2(0, 0);
            // Generate a random room based on our dimensions to place. 
            int roomWidth = Random.Range(RoguelikeGenerator.instance.minWidth, RoguelikeGenerator.instance.maxWidth);
            int roomHeight = Random.Range(RoguelikeGenerator.instance.minHeight, RoguelikeGenerator.instance.maxHeight);
            //Debug.Log (string.Format ("Width: {0}, height: {1} for room {2}", roomWidth, roomHeight, i + 1));
            // For each cell C in the dungeon.
            foreach (KeyValuePair<Vector2, Cell> c in RoguelikeGenerator.instance.cellsP)
            {
                // Put bottom left room cell at C.
                Vector2 bottomLeft = c.Key;
                // Calculate the other four corners to use in position checking. 
                Vector2 bottomRight = bottomLeft + new Vector2(roomWidth, 0);
                Vector2 upperLeft = bottomLeft + new Vector2(0, roomHeight);
                Vector2 upperRight = upperLeft + new Vector2(roomWidth, 0);



                //Debug.Log (string.Format ("Upper left: {0}, {1}  Upper right: {2}, {3}   Bottom left: {4}, {5}   Bottom right: {6}, {7}", upperLeft.x, upperLeft.y, upperRight.x, upperRight.y, bottomLeft.x, bottomLeft.y, bottomRight.x, bottomRight.y));
                // Set our current score to 0.
                int currentScore = 0;
                //Debug.Log (string.Format ("Testing bottom left {0},{1}", bottomLeft.x, bottomLeft.y));

                // Only check room position if this position wouldn't take the room completely off the grid. 
                Vector2 cCell = new Vector2(0, 0);
                if (upperLeft.y < mazeRows && upperRight.x < mazeColumns)
                {
                    // Check every cell that would be covered by the room in this position. 
                    // Cover each cell by looping through each x, for each y. 
                    for (int x = (int)bottomLeft.x; x < (int)bottomRight.x; x++)
                    {
                        for (int y = (int)bottomLeft.y; y < (int)upperLeft.y; y++)
                        {
                            cCell = new Vector2(x, y);
                            // For each cell adjacent to a corridor, add 1 to current score
                            // Adjacent = cell is a wall, but is next to at least one floor tile. 

                            // Check the current cell's neighbours. 
                            Vector2 currentPos = cCell;
                            bool isAdjacentC = false;
                            for (int j = 0; j < MazeUtils.possibleNeighbours.Length; j++)
                            {
                                // Find the position of a neighbour on the grid, relative to the current cell. 
                                Vector2 nPos = currentPos + MazeUtils.possibleNeighbours[j];
                                // Check the neighbouring cell exists. 
                                if (RoguelikeGenerator.instance.cellsP.ContainsKey(nPos) && RoguelikeGenerator.instance.cellsP[nPos].cellObject.activeInHierarchy)
                                {
                                    isAdjacentC = true;
                                    RoguelikeGenerator.instance.roomsP[currentPos].isAdjacentC = true;
                                }
                                    
                            }
                            // For each cell overlapping a room, add 100 to score.
                            if (RoguelikeGenerator.instance.roomsP[cCell].cellObject.activeInHierarchy)
                            {
                                //Debug.Log (string.Format ("Adding 100 to score."));

                                currentScore += 100;
                            }
                            if (RoguelikeGenerator.instance.wallsP[cCell].cellObject.activeInHierarchy && isAdjacentC)
                            {
                                currentScore += 1;
                            }

                            // For each cell overlapping a corridor, add 3 to the score
                            if (!RoguelikeGenerator.instance.wallsP[cCell].cellObject.activeInHierarchy)
                            {
                                currentScore += 3;
                            }

                        }

                    }
                }
                

               // If this resulting score for the room is better than our current best, replace it. 
                if (currentScore < bestScore && currentScore > 0)
                {
                    bestScore = currentScore;
                    bestPos = bottomLeft;
                }

            }
            // Place the room in the upper left position where the best score was. 
            for (int x = (int)bestPos.x; x < (int)bestPos.x + roomWidth; x++)
            {
                for (int y = (int)bestPos.y; y < (int)bestPos.y + roomHeight; y++)
                {
                   
                    Vector2 pos = new Vector2(x, y);
                    if (RoguelikeGenerator.instance.roomsP[pos].isAdjacentC)
                        RoguelikeGenerator.instance.adjCellsInstances.Add(RoguelikeGenerator.instance.roomsP[pos]);

                    RoguelikeGenerator.instance.roomsP[pos].roomNo = i + 1;


                    if (RoguelikeGenerator.instance.cellsP[pos].cellObject.activeInHierarchy)
                    {
                        RoguelikeGenerator.instance.cellsP[pos].cellObject.SetActive(false);
                        // Remove all walls
                        RoguelikeGenerator.instance.cellsP[pos].cScript.wallD.SetActive(false);
                        RoguelikeGenerator.instance.cellsP[pos].cScript.wallL.SetActive(false);
                        RoguelikeGenerator.instance.cellsP[pos].cScript.wallR.SetActive(false);
                        RoguelikeGenerator.instance.cellsP[pos].cScript.wallU.SetActive(false);
                    }
                    else
                    {
                        RoguelikeGenerator.instance.wallsP[pos].cellObject.SetActive(false);
                    }

                    RoguelikeGenerator.instance.roomsP[pos].cellObject.SetActive(true);
                    CellScript cs = RoguelikeGenerator.instance.roomsP[pos].cScript;
                    // Only need to do special wall check for left and below cells due to the room cells being instantiated upwards column by column.
                    cs.wallL.SetActive(false); 
                    cs.wallR.SetActive(false); 
                    cs.wallU.SetActive(false); 
                    cs.wallD.SetActive(false); 
                    Vector2 nPos = pos;
                    // Left neighbour
                    nPos = pos + MazeUtils.possibleNeighbours[0];
                    if (RoguelikeGenerator.instance.cellsP.ContainsKey(nPos))
                    {
                        Cell n = RoguelikeGenerator.instance.cellsP[nPos];
                        if (RoguelikeGenerator.instance.wallsP.ContainsKey(n.gridPos) && RoguelikeGenerator.instance.wallsP[n.gridPos].cellObject.activeInHierarchy || (RoguelikeGenerator.instance.roomsP.ContainsKey(n.gridPos) && RoguelikeGenerator.instance.roomsP[n.gridPos].cellObject.activeInHierarchy && RoguelikeGenerator.instance.roomsP[pos].roomNo != RoguelikeGenerator.instance.roomsP[nPos].roomNo))
                        {
                            cs.wallL.SetActive(true);
                        }
                        if (RoguelikeGenerator.instance.roomsP.ContainsKey(n.gridPos) && RoguelikeGenerator.instance.roomsP[pos].roomNo == RoguelikeGenerator.instance.roomsP[nPos].roomNo)
                        {
                            cs.wallR.SetActive(false);
                        }
                    }

                    // Right neighbour 
                    nPos = pos + MazeUtils.possibleNeighbours[1];
                    if (RoguelikeGenerator.instance.cellsP.ContainsKey(nPos))
                    {
                        Cell n = RoguelikeGenerator.instance.cellsP[nPos];
                        if (RoguelikeGenerator.instance.wallsP.ContainsKey(n.gridPos) || (RoguelikeGenerator.instance.roomsP.ContainsKey(n.gridPos) && RoguelikeGenerator.instance.roomsP[pos].roomNo != RoguelikeGenerator.instance.roomsP[nPos].roomNo))
                        {
                            cs.wallR.SetActive(true);
                        }
                    }

                    // Above neighbour
                    nPos = pos + MazeUtils.possibleNeighbours[2];
                    if (RoguelikeGenerator.instance.cellsP.ContainsKey(nPos))
                    {
                        Cell n = RoguelikeGenerator.instance.cellsP[nPos];
                        if (RoguelikeGenerator.instance.wallsP.ContainsKey(n.gridPos) || (RoguelikeGenerator.instance.roomsP.ContainsKey(n.gridPos) && RoguelikeGenerator.instance.roomsP[pos].roomNo != RoguelikeGenerator.instance.roomsP[nPos].roomNo))
                        {
                            cs.wallU.SetActive(true);
                        }
                    }

                    // Below neighbour
                    nPos = pos + MazeUtils.possibleNeighbours[3];
                    if (RoguelikeGenerator.instance.cellsP.ContainsKey(nPos))
                    {
                        Cell n = RoguelikeGenerator.instance.cellsP[nPos];
                        if (RoguelikeGenerator.instance.wallsP.ContainsKey(n.gridPos) || (RoguelikeGenerator.instance.roomsP.ContainsKey(n.gridPos) && RoguelikeGenerator.instance.roomsP[pos].roomNo != RoguelikeGenerator.instance.roomsP[nPos].roomNo))
                        {
                            cs.wallD.SetActive(true);
                        }
                        if (RoguelikeGenerator.instance.roomsP.ContainsKey(n.gridPos) && RoguelikeGenerator.instance.roomsP[pos].roomNo == RoguelikeGenerator.instance.roomsP[nPos].roomNo)
                        {
                            cs.wallU.SetActive(false);
                        }
                    }

                    r.roomCellsInstantiated.Add(RoguelikeGenerator.instance.roomsP[pos]);
                }
            }

            RoguelikeGenerator.instance.rooms.Add(r);

        }

    }

    // Done in here to swap out a tile instead of passing the lists everywhere.
    public void PlacePlayerSpawn()
    {
        // Choose a random point in a corridor and change that cell's type to the player's spawn. 
        bool validSpawn = false;
        Cell spawnTile = null;
        while (!validSpawn)
        {
            Vector2 pos = new Vector2(Random.Range(0, mazeRows), Random.Range(0, mazeColumns));
            spawnTile = RoguelikeGenerator.instance.cellsP[pos];
            if (RoguelikeGenerator.instance.cellsP.ContainsKey(pos) && RoguelikeGenerator.instance.cellsP[pos].cellObject.activeInHierarchy)
            {
                validSpawn = true;
            }
        }

        // Deactivate the cell here and instantiate a player spawn at its place. 
        spawnTile.cellObject.SetActive(false);
        GameObject.Instantiate(RoguelikeGenerator.instance.spawnPrefab, spawns[spawnTile.gridPos], Quaternion.identity);

        RoguelikeGenerator.instance.playerSpawnInstance = spawnTile;
    }
}
