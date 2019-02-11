using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MWRCellStruct
{
    // Hold and locate all the cells in the maze. 
    private Dictionary<Vector2, CellS> mazeCells = new Dictionary<Vector2, CellS>();
    // Hold and locate all the cells in the dungeon (after doubling)
    private Dictionary<Vector2, CellS> dungeonCells = new Dictionary<Vector2, CellS>();

   
    // List of all cells found to be dead ends, i.e, 3 walls are active. 
    private List<CellS> deadEnds = new List<CellS>();

    // How many cells TALL the maze will be.
    public int mazeRows;
    // how many cells WIDE the maze will be. 
    public int mazeColumns;

    // How large the finished dungeon will be if doubled. Calculated from mazeRows and mazeColumns when solution is doubled to space it out.
    private int dungeonRows;
    private int dungeonColumns;

    // List to store cells being checked during generation for Recursive Backtracking: the 'stack'.
    private List<CellS> stack = new List<CellS>();
    // Counter of how many cells we still have to visit. Initialised to the number of cells in the grid - 1 (for the start cell).
    private int unvisited;
    // Holds the current cell and the next cell being checked.
    private CellS currentCell;
    private CellS checkCell;

    // Cell size to determine how far apart to place cells during generation. 
    private float cellSize;

   

    
    public MWRCellStruct(int gridRows, int gridColumns)
    {
        
        GenerateMaze(gridRows, gridColumns);
    }

    private void GenerateMaze(int rows, int columns)
    {
        mazeRows = rows;
        mazeColumns = columns;
        CreateLayout();
    }

    public void CreateLayout()
    {
        // Detemrine the size of the cells to place from the tile we're using. 
        cellSize = RoguelikeGenerator.instance.cellPrefab.transform.localScale.x;

        // Set the starting point of our maze somewhere in the middle. 
        Vector2 startPos = new Vector2(-(cellSize * (mazeColumns / 2)) + (cellSize / 2), -(cellSize * (mazeRows / 2)) + (cellSize / 2));
        Vector2 spawnPos = startPos;

        for (int x = 0; x < mazeColumns; x++)
        {
            for (int y = 0; y < mazeRows; y++)
            {
                Vector2 gridPos = new Vector2(x, y);
                mazeCells[gridPos] = MazeUtils.GenerateCell(spawnPos, gridPos);
                spawnPos.y += cellSize;
            }

            // Reset spawn position and move up a row. 
            spawnPos.y = startPos.y;
            spawnPos.x += cellSize;
        }

        // Choose a random cell to start from 
        int xStart = Random.Range(0, mazeColumns);
        int yStart = Random.Range(0, mazeRows);
        currentCell = mazeCells[new Vector2(xStart, yStart)];

        // Mark how long it took for the grid to be generated.
        RoguelikeGenerator.instance.gridGenTime = Time.realtimeSinceStartup;
        RoguelikeGenerator.instance.totalGenTime = RoguelikeGenerator.instance.gridGenTime;

        // Mark our starting cell as visited.
        currentCell.visited = true;
        unvisited = (mazeRows * mazeColumns) - 1;
        // Perform recursive backtracking to create a maze. 
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
        if (RoguelikeGenerator.instance.doubleDungeon)
        {
            DoubleDungeon();
            RoguelikeGenerator.instance.cells = dungeonCells;
            mazeRows = dungeonRows - 1;
            mazeColumns = dungeonColumns - 1;
        }
        else
        {
            RoguelikeGenerator.instance.cells = mazeCells;
        }


        PlaceRooms();

        RoguelikeGenerator.instance.roomTime = Time.realtimeSinceStartup - RoguelikeGenerator.instance.totalGenTime;
        RoguelikeGenerator.instance.totalGenTime += RoguelikeGenerator.instance.roomTime;

    }

    public void RecursiveBacktracking()
    {
        // Loop while there are still cells in the grid we haven't visited. 
        while (unvisited > 0)
        {
            List<CellS> unvisitedNeighbours = MazeUtils.GetUnvisitedNeighbours(currentCell, ref mazeCells);
            if (unvisitedNeighbours.Count > 0)
            {
                // Choose a random unvisited neighbour. 
                checkCell = unvisitedNeighbours[Random.Range(0, unvisitedNeighbours.Count)];
                // Add current cell to stack.
                stack.Add(currentCell);
                // Compare and remove walls if needed.
                MazeUtils.CompareWalls(currentCell, checkCell);
                // Make our new current cell the neighbour we were checking. 
                currentCell = checkCell;
                // Mark our new current cell as visited. 
                currentCell.visited = true;
                unvisited--;
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
                if (mazeCells.ContainsKey(pos))
                {
                    CellS testCell = mazeCells[pos];
                    if (MazeUtils.NoOfWalls(testCell) >= 3 && testCell.type != CellS.TileType.Wall)
                    {
                        deadEnds.Add(testCell);
                    }
                }
            }
        }
    }

    public void FillInEnds()
    {
        for (int i = 0; i < RoguelikeGenerator.instance.sparseness; i++)
        {
            for (int j = 0; j < deadEnds.Count; j++)
            {
                CellS currentEnd = deadEnds[j];

                Vector2 direction = new Vector2(0, 0); // The direction vector this passage moves in. 

                // Find the open direction of this cell- the direction this passage is going in. 
                if (!currentEnd.wallD)
                {
                    direction.y -= 1;
                }

                if (!currentEnd.wallL)
                {
                    direction.x -= 1;
                }

                if (!currentEnd.wallR)
                {
                    direction.x += 1;
                }

                if (!currentEnd.wallU)
                {
                    direction.y += 1;
                }

                // Mark this cell as a wall. 
                currentEnd.type = CellS.TileType.Wall;
                // The position of the next cell we're going to be moving to check. 
                Vector2 nextPos = deadEnds[j].gridPos + direction;

                // Follow along ths passageway until we are no longer filling in dead ends in this direction i.e current cell does not have 3 walls.
                while (MazeUtils.NoOfWalls(mazeCells[nextPos]) == 3 && nextPos.x < mazeRows - 1 && nextPos.y < mazeColumns - 1 && nextPos.x > 0 && nextPos.y > 0)
                {
                    // Mark the current cell as a wall if it hasn't been done already. 
                    if (mazeCells[nextPos + direction].type != CellS.TileType.Wall)
                    {
                        mazeCells[nextPos + direction].type = CellS.TileType.Wall;
                    }

                    // Move along the passageway.
                    nextPos += direction;
                }


                // Add a wall to the opposite direction on our current non-dead end cell- the way back into the passage we just filled in. 
                if (direction.y == -1)
                {
                    mazeCells[nextPos].wallU = true;
                }
                else if (direction.y == 1)
                {
                    mazeCells[nextPos].wallD = true;
                }
                else if (direction.x == -1)
                {
                    mazeCells[nextPos].wallR = true;
                }
                else if (direction.x == 1)
                {
                    mazeCells[nextPos].wallL = true;
                }

                // If our current cell now has 3 walls, it's a dead end, so update our dead end list.
                // else we're done with this dead end, so remove it.
                if (MazeUtils.NoOfWalls(mazeCells[nextPos]) == 3)
                {
                    deadEnds[j] = mazeCells[nextPos];
                }
                else
                {
                    deadEnds.RemoveAt(j);
                }

            }


        }
        // Set all tiles which are now walls tohave all walls active to avoid random gaps against walls and for later calculation.
        // All activated at the end to avoid confusing loop adding calculations.
        foreach (KeyValuePair<Vector2, CellS> c in mazeCells)
        {
            if (c.Value.type == CellS.TileType.Wall)
            {
                c.Value.wallD = true;
                c.Value.wallU = true;
                c.Value.wallL = true;
                c.Value.wallR = true;
            }
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
                CellS currentDeadEnd = deadEnds[i];
                // Find current dead end's valid neighbours- cells that are on the grid. 
                while (!hitCorridor)
                {
                    List<CellS> neighbours = new List<CellS>();
                    Vector2 currentPos = currentDeadEnd.gridPos;
                    // Try to not double back over the cell we just came from, as then will hit a corridor immediately and still have a dead end. 
                    int indexToSkip = 0;
                    if (!currentDeadEnd.wallD)
                    {
                        indexToSkip = 3;
                    }
                    else if (!currentDeadEnd.wallL)
                    {
                        indexToSkip = 0;
                    }

                    else if (!currentDeadEnd.wallR)
                    {
                        indexToSkip = 1;
                    }

                    else if (!currentDeadEnd.wallU)
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
                            if (mazeCells.ContainsKey(nPos))
                                neighbours.Add(mazeCells[nPos]);
                        }
                    }
                    // Choose a random direction i.e. a random neighbour.
                    checkCell = neighbours[Random.Range(0, neighbours.Count)];

                    // See if this tile is currently a wall. 
                    // If so, change it back to a corridor.
                    if (checkCell.type == CellS.TileType.Wall)
                    {
                        checkCell.type = CellS.TileType.Corridor;
                        // Remove walls between chosen neighbour and current dead end. 
                        MazeUtils.CompareWalls(currentDeadEnd, checkCell);

                        // Advance the dead end we're checking to this new spot.
                        currentDeadEnd = checkCell;
                    }
                    else
                    {
                        // Remove walls between chosen neighbour and current dead end. 
                        MazeUtils.CompareWalls(currentDeadEnd, checkCell);
                        hitCorridor = true;
                    }

                }
            }
        }
    }

    public void DoubleDungeon()
    {
        // Set the dimensions of our dungeon solutions. 
        dungeonRows = mazeRows * 2 + 1;
        dungeonColumns = mazeColumns * 2 + 1;

        // Update existing cells.
        for (int x = 0; x < mazeColumns; x++)
        {
            for (int y = 0; y < mazeRows; y++)
            {
                CellS mazeCell = mazeCells[new Vector2(x, y)];

                Vector2 oldPos = new Vector2(mazeCell.gridPos.x, mazeCell.gridPos.y);
                Vector2 oldSpawn = new Vector2(mazeCell.spawnPos.x, mazeCell.spawnPos.y);

                CellS dungeonCell = new CellS(mazeCell);

                dungeonCell.gridPos = mazeCell.gridPos * 2;
                dungeonCell.spawnPos = new Vector2(oldSpawn.x + cellSize * x, oldSpawn.y + cellSize * y);

                dungeonCells.Add(dungeonCell.gridPos, dungeonCell);

                // Add spacers for this doubled cell. 
                CellS spacerX = new CellS();
                spacerX.gridPos = new Vector2(dungeonCell.gridPos.x + 1, dungeonCell.gridPos.y);
                spacerX.spawnPos = new Vector2(dungeonCell.spawnPos.x + 1, dungeonCell.spawnPos.y);

                //Determine the type of this spacer cell. Default is wall.
                spacerX.type = CellS.TileType.Wall;
                // If the current cell is a corridor, spacer cell needs to connect to its old neighbours
                if (dungeonCell.type == CellS.TileType.Corridor)
                {
                    // Check each neighbour for if its a corridor and if the walls are connected between it and the current cell.
                    Vector2 nPos = mazeCell.gridPos;

                    //Right neighbour
                    nPos = mazeCell.gridPos + MazeUtils.possibleNeighbours[1];

                    if (mazeCells.ContainsKey(nPos))
                    {
                        CellS n = mazeCells[nPos];
                        if (n.type == CellS.TileType.Corridor && (!mazeCell.wallR && !n.wallL))
                        {
                            spacerX.type = CellS.TileType.Corridor;
                            spacerX.wallR = false;
                            spacerX.wallL = false;

                        }
                    }
                }
                dungeonCells.Add(spacerX.gridPos, spacerX);

                CellS spacerY = new CellS();
                spacerY.gridPos = new Vector2(dungeonCell.gridPos.x, dungeonCell.gridPos.y + 1);
                spacerY.spawnPos = new Vector2(dungeonCell.spawnPos.x, dungeonCell.spawnPos.y + 1);
                spacerY.type = CellS.TileType.Wall;
                if (dungeonCell.type == CellS.TileType.Corridor)
                {

                    //Check each neighbour for if its a corridor and if the walls are connected between it and the current cell.
                    Vector2 nPos = mazeCell.gridPos;

                    //Above neighbour
                    nPos = mazeCell.gridPos + MazeUtils.possibleNeighbours[2];
                    if (mazeCells.ContainsKey(nPos))
                    {

                        CellS n = mazeCells[nPos];
                        if (n.type == CellS.TileType.Corridor && (!mazeCell.wallU && !n.wallD))
                        {
                            spacerY.type = CellS.TileType.Corridor;
                            spacerY.wallU = false;
                            spacerY.wallD = false;
                        }
                    }


                }

                dungeonCells.Add(spacerY.gridPos, spacerY);

                // Diagonal spacer will always be a wall as the seperator between rows.
                CellS spacerXY = new CellS();
                spacerXY.gridPos = new Vector2(dungeonCell.gridPos.x + 1, dungeonCell.gridPos.y + 1);
                spacerXY.spawnPos = new Vector2(dungeonCell.spawnPos.x + 1, dungeonCell.spawnPos.y + 1);
                spacerXY.type = CellS.TileType.Wall;

                dungeonCells.Add(spacerXY.gridPos, spacerXY);
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

            foreach (KeyValuePair<Vector2, CellS> c in RoguelikeGenerator.instance.cells)
            {
                // Put bottom left room cell at C.
                Vector2 bottomLeft = c.Key;
                // Calculate the other four corners to use in position checking. 
                Vector2 bottomRight = bottomLeft + new Vector2(roomWidth, 0);
                Vector2 upperLeft = bottomLeft + new Vector2(0, roomHeight);
                Vector2 upperRight = upperLeft + new Vector2(roomWidth, 0);

                // Set our current score to 0.
                int currentScore = 0;
                //Debug.Log (string.Format ("Testing bottom left {0},{1}", bottomLeft.x, bottomLeft.y));
                // Only check room position if this position wouldn't take the room completely off the grid. 
                Vector2 cCell = new Vector2(0, 0);
                if (upperLeft.y < mazeRows && upperRight.x < mazeColumns)
                {
                    for (int x = (int)bottomLeft.x; x < (int)bottomRight.x; x++)
                    {
                        for (int y = (int)bottomLeft.y; y < (int)upperLeft.y; y++)
                        {
                            cCell = new Vector2(x, y);
                            // Check the current cell's neighbours. 
                            Vector2 currentPos = cCell;
                            bool isAdjacentC = false;
                            CellS current = RoguelikeGenerator.instance.cells[cCell];
                            for (int j = 0; j < MazeUtils.possibleNeighbours.Length; j++)
                            {
                                // Find the position of a neighbour on the grid, relative to the current cell. 
                                Vector2 nPos = currentPos + MazeUtils.possibleNeighbours[j];
                                // Check the neighbouring cell exists and whether it's a corridor.
                                if (RoguelikeGenerator.instance.cells.ContainsKey(nPos) && RoguelikeGenerator.instance.cells[nPos].type == CellS.TileType.Corridor)
                                {
                                    isAdjacentC = true;
                                    current.isAdjacentC = true;
                                }


                                // For each cell overlapping a room, add 100 to score.
                                if (current.type == CellS.TileType.Room)
                                {
                                    currentScore += 100;
                                }

                                // For each cell next to a corridor, add 1.
                                if (current.type == CellS.TileType.Wall && isAdjacentC)
                                {
                                    currentScore += 1;
                                }

                                // For each cell overlapping a corridor, add 3 to the score
                                if (current.type == CellS.TileType.Corridor)
                                {
                                    currentScore += 3;
                                }

                            }
                        }
                    }
                }


                // If this resulting score for the room is better than our current best, replace it. 
                if (currentScore < bestScore && currentScore > 0)
                {
                    bestScore = currentScore;
                    bestPos = bottomLeft;
                    //Debug.Log (string.Format ("New best score"));

                }

            }

            // Place the room in the upper left position where the best score was. 
            for (int x = (int)bestPos.x; x < (int)bestPos.x + roomWidth; x++)
            {
                for (int y = (int)bestPos.y; y < (int)bestPos.y + roomHeight; y++)
                {
                    Vector2 pos = new Vector2(x, y);
                    CellS c = RoguelikeGenerator.instance.cells[pos];
                    //cells [pos].type = CellS.TileType.Room;
                    c.type = CellS.TileType.Room;
                    c.roomNo = i + 1;
                    if (c.isAdjacentC)
                        RoguelikeGenerator.instance.adjCells.Add(c);

                    // Determine which walls on this room cell need to be active to block it off. 
                    // Deactivate all walls first. 
                    // Only need to do special wall check for left and below cells due to the room cells being instantiated upwards column by column.
                    c.wallL = false;
                    c.wallR = false;
                    c.wallU = false;
                    c.wallD = false;
                    Vector2 nPos = pos;
                    // Left neighbour
                    nPos = pos + MazeUtils.possibleNeighbours[0];
                    if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                    {
                        CellS n = RoguelikeGenerator.instance.cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallL = true;
                        }
                        if (n.type == CellS.TileType.Room && c.roomNo == n.roomNo)
                        {
                            n.wallR = false;
                        }
                    }

                    // Right neighbour 
                    nPos = pos + MazeUtils.possibleNeighbours[1];
                    if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                    {
                        CellS n = RoguelikeGenerator.instance.cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallR = true;
                        }
                    }

                    // Above neighbour
                    nPos = pos + MazeUtils.possibleNeighbours[2];
                    if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                    {
                        CellS n = RoguelikeGenerator.instance.cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallU = true;
                        }
                    }

                    // Below neighbour
                    nPos = pos + MazeUtils.possibleNeighbours[3];
                    if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                    {
                        CellS n = RoguelikeGenerator.instance.cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallD = true;
                        }
                        if (n.type == CellS.TileType.Room && c.roomNo == n.roomNo)
                        {
                            n.wallU = false;
                        }
                    }

                    r.roomCells.Add(c);
                }
            }

            RoguelikeGenerator.instance.rooms.Add(r);
        }
    }
    
}
