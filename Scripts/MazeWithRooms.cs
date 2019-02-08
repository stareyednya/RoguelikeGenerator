// CELL STRUCTURE METHOD

using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MazeWithRooms : MonoBehaviour {
	// Hold and locate all the cells in the maze. 
	private Dictionary<Vector2, CellS> mazeCells = new Dictionary<Vector2, CellS>();
    // Hold and locate all the cells in the dungeon (after doubling)
    private Dictionary<Vector2, CellS> dungeonCells = new Dictionary<Vector2, CellS>();
    // The final dictonary that will be used to instantiate the grid with. Will be set to one of the above depending on if the dungeon is spaced or not.
    private Dictionary<Vector2, CellS> cells = new Dictionary<Vector2, CellS>();

    // How many cells TALL the maze will be.
    public int mazeRows;
	// how many cells WIDE the maze will be. 
	public int mazeColumns;

    // How large the finished dungeon will be if doubled. Calculated from mazeRows and mazeColumns when solution is doubled to space it out.
    private int dungeonRows;
    private int dungeonColumns;

    // How much rock to fill back in. 
    public int sparseness;

	// The chance of a dead end being filled back in. 
	public int removalChance;

	// Currently storing the time since the scene started in here to save processing time with a write to disk for a debug statement. 
	public float totalGenTime;
	public float gridGenTime;
	public float mazeGenTime;
	public float sparseTime;
	public float loopTime;
	public float roomTime;
	public float instantiationTime;
    public float lockTime;

	// Parameters for the number of rooms to include, and their range of possible dimensions. 
	public int noOfRooms;
	public int minWidth;
	public int maxWidth;
	public int minHeight;
	public int maxHeight; 

	// The prefab to use as the cell icon. 
	[SerializeField]
	private GameObject cellPrefab;
	// The tile to use as a wall to fill in dead ends with.
	[SerializeField]
	private GameObject wallPrefab;
	// A tile to differentiate room tiles from the floor.
	[SerializeField]
	private GameObject roomPrefab;
    // Lock icon- currently using drink from the tutorial because i dont want to make one right now
    [SerializeField]
    private GameObject lockPrefab;
    // Key icon - currently using food from the tutorial because I don't want to make one right now 
    [SerializeField]
    private GameObject keyPrefab;
    // Player - currently does nothing, only used as a starting point to try pathfinding to the key with
    [SerializeField]
    private GameObject spawnPrefab;

    [SerializeField]
    private GameObject spacerPrefab;

    // List to store cells being checked during generation for Recursive Backtracking: the 'stack'.
    private List<CellS> stack = new List<CellS>();
	// Counter of how many cells we still have to visit. Initialised to the number of cells in the grid - 1 (for the start cell).
	private int unvisited;
	// Holds the current cell and the next cell being checked.
	private CellS currentCell;
	private CellS checkCell;

	// Array of all possible neighbour positions
	// Left, right, up, down
	private Vector2[] possibleNeighbours = new Vector2[]{new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1)};

	// Cell size to determine how far apart to place cells during generation. 
	private float cellSize;

	// Organise the editor hierarchy. 
	private GameObject mazeParent;
	private GameObject wallParent;
	// POOLING TESTER
	private GameObject wallPParent;

	// Checkboxes to select which generation to use.
	public bool recursiveBacktrack;
    public bool doubleDungeon;

	// List of all cells found to be dead ends, i.e, 3 walls are active. 
	private List<CellS> deadEnds = new List<CellS> ();

	// Tester to see what rooms is finding
	private GameObject roomParent;
	// List for cells that're found to be next to a corridor in a room. 
	private List<CellS> adjCells = new List<CellS> ();

    // Keep a unique reference to the specific tile the player spawns at- will be used for A*.
    private CellS playerSpawn;
    // Same for the key placement, but as a position since it's not an instantiated cell.
    private CellS keySpawn;


    // Use this for initialization
    void Start () 
	{
		GenerateMaze (mazeRows, mazeColumns);
		PlacePlayerSpawn (); // must be here as it works on tile types only

		InstantiateGrid ();
		instantiationTime =  Time.realtimeSinceStartup - totalGenTime;
		totalGenTime += instantiationTime;


        PlaceLocks();
        keySpawn.h = (int)ManhattanDistance (keySpawn);
        //Debug.Log (string.Format ("Manhattan distance: {0}", keySpawn.h));
        PathToSpawn();

        lockTime = Time.realtimeSinceStartup - totalGenTime;
        totalGenTime += lockTime;

        //List<CellS> bestPath = BuildPath(playerSpawn);
        //foreach (CellS c in bestPath)
        //{
        //    Debug.Log(string.Format("{0}, {1}", c.gridPos.x, c.gridPos.y));
        //}

    }

	private void GenerateMaze (int rows, int columns)
	{
		mazeRows = rows;
		mazeColumns = columns;
		CreateLayout ();
	}

	public void CreateLayout()
	{
		// Detemrine the size of the cells to place from the tile we're using. 
		cellSize = cellPrefab.transform.localScale.x;

		mazeParent = new GameObject ();
		mazeParent.transform.position = Vector2.zero; 
		mazeParent.name = "Maze";


		wallPParent = new GameObject ();
		wallPParent.transform.position = Vector2.zero; 
		wallPParent.name = "Pooling Walls";

		roomParent = new GameObject ();
		roomParent.transform.position = Vector2.zero; 
		roomParent.name = "Rooms";

		// Set the starting point of our maze somewhere in the middle. 
		Vector2 startPos = new Vector2(-(cellSize * (mazeColumns / 2)) + (cellSize / 2), -(cellSize * (mazeRows / 2)) + (cellSize / 2));
		Vector2 spawnPos = startPos;

		for (int x = 0; x < mazeColumns; x++)
		{
			for (int y = 0; y < mazeRows; y++)
			{
				Vector2 gridPos = new Vector2 (x, y);
				GenerateCell(spawnPos, gridPos);
				spawnPos.y += cellSize;
			}

			// Reset spawn position and move up a row. 
			spawnPos.y = startPos.y;
			spawnPos.x += cellSize;
		}

		// Choose a random cell to start from 
		int xStart = Random.Range(0, mazeColumns);
		int yStart = Random.Range (0, mazeRows);
		currentCell = mazeCells [new Vector2 (xStart, yStart)];

		// Mark how long it took for the grid to be generated.
		gridGenTime =  Time.realtimeSinceStartup;
		totalGenTime = gridGenTime;

		// Mark our starting cell as visited.
		currentCell.visited = true;
		unvisited = (mazeRows * mazeColumns) - 1;
		// Perform recursive backtracking to create a maze. 
		RecursiveBacktracking ();

		mazeGenTime =  Time.realtimeSinceStartup - totalGenTime;
		totalGenTime += mazeGenTime;

		// Fill in the dead ends with some walls.
		FindDeadEnds ();
		FillInEnds();
		sparseTime =  Time.realtimeSinceStartup - totalGenTime;
		totalGenTime += sparseTime;

		// Remove some dead ends by making the maze imperfect.
		RemoveLoops ();
		loopTime =  Time.realtimeSinceStartup - totalGenTime;
		totalGenTime += loopTime;
        if (doubleDungeon)
        {
            DoubleDungeon();
            cells = dungeonCells;
            mazeRows = dungeonRows - 1;
            mazeColumns = dungeonColumns - 1;
        }
        else
        {
            cells = mazeCells;
        }
        

        PlaceRooms ();
		roomTime =  Time.realtimeSinceStartup - totalGenTime;
		totalGenTime += roomTime;

	}

	// Create a cell based on the given position. 
	public void GenerateCell(Vector2 pos, Vector2 keyPos)
	{
		CellS newCell = new CellS ();
		// Store a reference to this position in the grid.
		newCell.gridPos = keyPos;
		newCell.spawnPos = pos;
		// Store this created cell.
		mazeCells[keyPos] = newCell;
	}

	// Instantiate our finished grid. 
	public void InstantiateGrid()
	{
		foreach (KeyValuePair<Vector2, CellS> c in cells)
		{
			currentCell = c.Value;
			switch (currentCell.type)
			{
			default:
			case CellS.TileType.Corridor:
				currentCell.cellObject = Instantiate (cellPrefab, currentCell.spawnPos, cellPrefab.transform.rotation);
				// Child the new cell to the maze parent. 
				currentCell.cellObject.transform.parent = mazeParent.transform;
				// Set the name of this cellObject.
				currentCell.cellObject.name = "Cell - X:" + currentCell.gridPos.x + " Y:" + currentCell.gridPos.y;
				currentCell.cScript = currentCell.cellObject.GetComponent<CellScript> ();
				// Set up walls according to the cell's marked walls. Deactivating as default instantiation value is all active.
				if (!currentCell.wallD)
					currentCell.cScript.wallD.SetActive (false);
				if (!currentCell.wallU)
					currentCell.cScript.wallU.SetActive (false);
				if (!currentCell.wallL)
					currentCell.cScript.wallL.SetActive (false);
				if (!currentCell.wallR)
					currentCell.cScript.wallR.SetActive (false);
				break;
			case CellS.TileType.Wall:
				currentCell.cellObject = Instantiate (wallPrefab, currentCell.spawnPos, wallPrefab.transform.rotation);
				// Child the new cell to the maze parent. 
				currentCell.cellObject.transform.parent = wallPParent.transform;
				// Set the name of this cellObject.
				currentCell.cellObject.name = "Wall - X:" + currentCell.gridPos.x + " Y:" + currentCell.gridPos.y;
				break;
			case CellS.TileType.Room:
				currentCell.cellObject = Instantiate (roomPrefab, currentCell.spawnPos, roomPrefab.transform.rotation);
				// Child the new cell to the maze parent. 
				currentCell.cellObject.transform.parent = roomParent.transform;
				// Set the name of this cellObject.
				currentCell.cellObject.name = "Room - X:" + currentCell.gridPos.x + " Y:" + currentCell.gridPos.y;
                // Set up walls according to the cell's marked walls. Deactivating as default instantiation value is all active.
                currentCell.cScript = currentCell.cellObject.GetComponent<CellScript> ();
                if (!currentCell.wallD)
                    currentCell.cScript.wallD.SetActive(false);
                if (!currentCell.wallU)
                    currentCell.cScript.wallU.SetActive(false);
                if (!currentCell.wallL)
                    currentCell.cScript.wallL.SetActive(false);
                if (!currentCell.wallR)
                    currentCell.cScript.wallR.SetActive(false);
                break;
            case CellS.TileType.Spawn:
                currentCell.cellObject = Instantiate(spawnPrefab, currentCell.spawnPos, spawnPrefab.transform.rotation);
                break;
            case CellS.TileType.Spacer:
                    currentCell.cellObject = Instantiate(spacerPrefab, currentCell.spawnPos, spacerPrefab.transform.rotation);
                    break;
            }
		}
        
	}

	/* MAZE GENERATION */
	public void RecursiveBacktracking()
	{
		// Loop while there are still cells in the grid we haven't visited. 
		while (unvisited > 0)
		{
			List<CellS> unvisitedNeighbours = GetUnvisitedNeighbours (currentCell);
			if (unvisitedNeighbours.Count > 0)
			{
				// Choose a random unvisited neighbour. 
				checkCell = unvisitedNeighbours[Random.Range(0, unvisitedNeighbours.Count)];
				// Add current cell to stack.
				stack.Add(currentCell);
				// Compare and remove walls if needed.
				CompareWalls(currentCell, checkCell);
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
				Vector2 pos = new Vector2 (x, y);
				if (mazeCells.ContainsKey(pos))
				{
					CellS testCell = mazeCells [pos];
					if (NoOfWalls (testCell) >= 3 && testCell.type != CellS.TileType.Wall)
					{
						deadEnds.Add (testCell);
					}
				}
			}
		}
	}

	public void FillInEnds()
	{
		for (int i = 0; i < sparseness; i++) {
			for (int j = 0; j < deadEnds.Count; j++) {
				CellS currentEnd = deadEnds [j];

				Vector2 direction = new Vector2 (0, 0); // The direction vector this passage moves in. 

				// Find the open direction of this cell- the direction this passage is going in. 
				if (!currentEnd.wallD) {
					direction.y -= 1;
				}

				if (!currentEnd.wallL) {
					direction.x -= 1;
				}

				if (!currentEnd.wallR) {
					direction.x += 1;
				}

				if (!currentEnd.wallU) {
					direction.y += 1;
				}

				// Mark this cell as a wall. 
				currentEnd.type = CellS.TileType.Wall;
				// The position of the next cell we're going to be moving to check. 
				Vector2 nextPos = deadEnds[j].gridPos + direction; 

				// Follow along ths passageway until we are no longer filling in dead ends in this direction i.e current cell does not have 3 walls.
				while (NoOfWalls(mazeCells[nextPos]) == 3 && nextPos.x < mazeRows - 1 && nextPos.y < mazeColumns - 1 && nextPos.x > 0 && nextPos.y > 0)
				{
					// Mark the current cell as a wall if it hasn't been done already. 
					if (mazeCells [nextPos + direction].type != CellS.TileType.Wall)
					{
						mazeCells [nextPos + direction].type = CellS.TileType.Wall;
					}
						
					// Move along the passageway.
					nextPos += direction;
				}


				// Add a wall to the opposite direction on our current non-dead end cell- the way back into the passage we just filled in. 
				if (direction.y == -1)
				{
					mazeCells [nextPos].wallU = true;
				}
				else if (direction.y == 1)
				{
					mazeCells [nextPos].wallD = true;
				}
				else if (direction.x == -1)
				{
					mazeCells [nextPos].wallR = true;
				}
				else if (direction.x == 1)
				{
					mazeCells [nextPos].wallL = true;
				}

				// If our current cell now has 3 walls, it's a dead end, so update our dead end list.
				// else we're done with this dead end, so remove it.
				if (NoOfWalls(mazeCells [nextPos]) == 3)
				{
					deadEnds [j] = mazeCells [nextPos];
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
		FindDeadEnds ();

		for (int i = 0; i < deadEnds.Count; i++) 
		{
			bool hitCorridor = false;
			// Roll a number to determine if we're removing this dead end. 
			if (Random.Range (1, 101) <= removalChance) 
			{
				CellS currentDeadEnd = deadEnds [i];
				// Find current dead end's valid neighbours- cells that are on the grid. 
				while (!hitCorridor)
				{
					List<CellS> neighbours = new List<CellS> ();
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

					for (int j = 0; j < possibleNeighbours.Length; j++)
					{
						if (j != indexToSkip)
						{
							// Find the position of a neighbour on the grid, relative to the current cell. 
							Vector2 nPos = currentPos + possibleNeighbours[j];
							// Check the neighbouring cell exists. 
							if (mazeCells.ContainsKey (nPos))
								neighbours.Add(mazeCells [nPos]);
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
						CompareWalls(currentDeadEnd, checkCell);

						// Advance the dead end we're checking to this new spot.
						currentDeadEnd = checkCell;
					}
					else
					{
						// Remove walls between chosen neighbour and current dead end. 
						CompareWalls(currentDeadEnd, checkCell);
						hitCorridor = true;
					}

				}
			}
		}
	}

	public void PlaceRooms()
	{
		for (int i = 0; i < noOfRooms; i++) 
		{
			// Set our best score to an arbitarily large number.
			int bestScore = int.MaxValue;
			Vector2 bestPos = new Vector2 (0, 0);
			// Generate a random room based on our dimensions to place. 
			int roomWidth = Random.Range (minWidth, maxWidth);
			int roomHeight = Random.Range (minHeight, maxHeight);

			foreach (KeyValuePair<Vector2, CellS> c in cells) 
			{
				// Put bottom left room cell at C.
				Vector2 bottomLeft = c.Key;
				// Calculate the other four corners to use in position checking. 
				Vector2 bottomRight = bottomLeft + new Vector2 (roomWidth, 0);
				Vector2 upperLeft = bottomLeft + new Vector2 (0, roomHeight);
				Vector2 upperRight = upperLeft + new Vector2 (roomWidth, 0);

				// Set our current score to 0.
				int currentScore = 0;
				//Debug.Log (string.Format ("Testing bottom left {0},{1}", bottomLeft.x, bottomLeft.y));
				// Only check room position if this position wouldn't take the room completely off the grid. 
				Vector2 cCell = new Vector2(0,0);
				if (upperLeft.y < mazeRows && upperRight.x < mazeColumns)
				{
					for (int x = (int)bottomLeft.x; x < (int)bottomRight.x; x++) 
					{
						for (int y = (int)bottomLeft.y; y < (int)upperLeft.y; y++) 
						{
							cCell = new Vector2 (x, y);
							// Check the current cell's neighbours. 
							Vector2 currentPos = cCell;
                            bool isAdjacentC = false;
							CellS current = cells [cCell];
							for (int j = 0; j < possibleNeighbours.Length; j++) {
								// Find the position of a neighbour on the grid, relative to the current cell. 
								Vector2 nPos = currentPos + possibleNeighbours [j];
								// Check the neighbouring cell exists and whether it's a corridor.
								if (cells.ContainsKey (nPos) && cells[nPos].type == CellS.TileType.Corridor)
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
				if (currentScore < bestScore && currentScore > 0) {
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
					CellS c = cells [pos];
					//cells [pos].type = CellS.TileType.Room;
					c.type = CellS.TileType.Room;
					c.roomNo = i + 1;
					if (c.isAdjacentC)
						adjCells.Add (c);

                    // Determine which walls on this room cell need to be active to block it off. 
                    // Deactivate all wall first. 
                    // Only need to do special wall check for left and below cells due to the room cells being instantiated upwards column by column.
                    c.wallL = false;
                    c.wallR = false;
                    c.wallU = false;
                    c.wallD = false;
                    Vector2 nPos = pos;
                    // Left neighbour
                    nPos = pos + possibleNeighbours[0];
                    if (cells.ContainsKey(nPos))
                    {
                        CellS n = cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallL = true;
                        }
                        if(n.type == CellS.TileType.Room && c.roomNo == n.roomNo)
                        {
                            n.wallR = false;
                        }
                    }

                    // Right neighbour 
                    nPos = pos + possibleNeighbours[1];
                    if (cells.ContainsKey(nPos))
                    {
                        CellS n = cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallR = true;
                        }
                    }

                    // Above neighbour
                    nPos = pos + possibleNeighbours[2];
                    if (cells.ContainsKey(nPos))
                    {
                        CellS n = cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallU = true;
                        }
                    }

                    // Below neighbour
                    nPos = pos + possibleNeighbours[3];
                    if (cells.ContainsKey(nPos))
                    {
                        CellS n = cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallD = true;
                        }
                        if (n.type == CellS.TileType.Room && c.roomNo == n.roomNo)
                        {
                            n.wallU = false;
                        }
                    }
                }
			} 
		}
	}

	/* MAZE GENERATION UTILITIES */
	public List<CellS> GetUnvisitedNeighbours(CellS cCell)
	{
		List<CellS> neighbours = new List<CellS> ();
		CellS nCell = cCell;
		// Store the position of our current cell. 
		Vector2 currentPos = cCell.gridPos;

		foreach(Vector2 p in possibleNeighbours)
		{
			// Find the position of a neighbour on the grid, relative to the current cell. 
			Vector2 nPos = currentPos + p;
			// Check the neighbouring cell exists. 
			if (mazeCells.ContainsKey (nPos))
				nCell = mazeCells [nPos];

			// Check if the neighbouring cell is unvisited and thus a valid neighbour. 
			if (!nCell.visited)
				neighbours.Add (nCell);
		}

		// Return the completed list of unvisited neighbours.
		return neighbours;
	}

	// Compare current cell with its neighbour and remove walls as appropriate. 
	public void CompareWalls(CellS currentCell, CellS neighbourCell)
	{
		// If neighbour is to the left of current. 
		if (neighbourCell.gridPos.x < currentCell.gridPos.x)
		{
			neighbourCell.wallR = false;
			currentCell.wallL = false;
		}
		// If neighbour is to the right of current. 
		else if (neighbourCell.gridPos.x > currentCell.gridPos.x)
		{
			neighbourCell.wallL = false;
			currentCell.wallR = false;
		}
		// If neighbour is above current. 
		else if (neighbourCell.gridPos.y > currentCell.gridPos.y)
		{
			neighbourCell.wallD = false;
			currentCell.wallU = false;
		}
		// If neighbour is below current. 
		else if (neighbourCell.gridPos.y < currentCell.gridPos.y)
		{
			neighbourCell.wallU = false;
			currentCell.wallD = false;
		}
	}

	public int NoOfWalls(CellS c)
	{
		int count = 0;
		if (c.wallD)
			count++;

		if (c.wallL)
			count++;
		if (c.wallR)
			count++;
		if (c.wallU)
			count++;

		return count;
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
                    nPos = mazeCell.gridPos + possibleNeighbours[1];

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
                    nPos = mazeCell.gridPos + possibleNeighbours[2];
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

    /* LOCKS AND KEYS */
    /* PATHFINDING */
    public CellS FindLowestFScore(List<CellS> openList)
    {
        int lowestF = int.MaxValue;
        CellS lowestCell = null;
        foreach (CellS c in openList)
        {
            if (c.f < lowestF)
            {
                lowestF = c.f;
                lowestCell = c;
            }
        }
        return lowestCell;
    }

    public bool AreConnected(CellS currentCell, CellS neighbourCell)
    {
        //Debug.Log (string.Format ("Current cell: {0},{1}  Neighbour: {2}, {3}", currentCell.gridPos.x, currentCell.gridPos.y, neighbourCell.gridPos.x, neighbourCell.gridPos.y));
        bool connected = true;
        // If neighbour is to the left of current. 
        if (neighbourCell.gridPos.x < currentCell.gridPos.x)
        {
            if (currentCell.wallL && neighbourCell.wallR)
            {
                connected = false;
                //Debug.Log (string.Format ("Left neighbour is not connected"));
            }

        }
        // If neighbour is to the right of current. 
        else if (neighbourCell.gridPos.x > currentCell.gridPos.x)
        {
            if (currentCell.wallR && neighbourCell.wallL)
            {
                connected = false;
                //Debug.Log (string.Format ("Right neighbour is not connected"));
            }
        }
        // If neighbour is above current. 
        else if (neighbourCell.gridPos.y > currentCell.gridPos.y)
        {
            if (currentCell.wallU && neighbourCell.wallD)
            {
                connected = false;
                //Debug.Log (string.Format ("Up neighbour is not connected"));
            }
        }
        // If neighbour is below current. 
        else if (neighbourCell.gridPos.y < currentCell.gridPos.y)
        {
            if (currentCell.wallD && neighbourCell.wallU)
            {
                connected = false;
                //Debug.Log (string.Format ("Down neighbour is not connected"));
            }
        }
        return connected;
    }

    public List<CellS> GetNeighbours(CellS cCell)
    {
        List<CellS> neighbours = new List<CellS>();
        CellS nCell = cCell;
        // Store the position of our current cell. 
        Vector2 currentPos = cCell.gridPos;

        foreach (Vector2 p in possibleNeighbours)
        {
            // Find the position of a neighbour on the grid, relative to the current cell. 
            Vector2 nPos = currentPos + p;
            // Check the neighbouring cell exists. 
            if (cells.ContainsKey(nPos))
                nCell = cells[nPos];

            // Check if the neighbouring cell is traversable and therefore a valid neighbour.
            // Also check if the wall in that direction is active to prevent the case of jumping to a blocked off corridor. 
            // (might be fixable when the dungeon is spaced out, but for now)
            if (nCell.type != CellS.TileType.Wall)
            {
                if ((nCell.type == CellS.TileType.Corridor && cCell.type == CellS.TileType.Corridor)
                    || (nCell.type == CellS.TileType.Room && cCell.type == CellS.TileType.Room))
                {
                    if (AreConnected(cCell, nCell))
                        neighbours.Add(nCell);
                }
                else
                {
                    neighbours.Add(nCell);
                }
            }

        }

        // Return the completed list of traversable neighbours.
        return neighbours;
    }

    public float ManhattanDistance(CellS c)
    {
        // Manhattan distance is the sum of the absolute values of the horizontal and the vertical distance
        float h = Mathf.Abs(c.gridPos.x - playerSpawn.gridPos.x) + Mathf.Abs(c.gridPos.y - playerSpawn.gridPos.y);
        return h;
    }


    public void PathToSpawn()
    {
        // Target position is the player's spawn - stored in playerSpawn already as a CellS 
        List<CellS> openList = new List<CellS>();
        List<CellS> closedList = new List<CellS>();
        // Add the starting position to the open list - the location of the key.
        CellS startCell = keySpawn;
        openList.Add(startCell);
        CellS currentCell = null;

        while (openList.Count > 0)
        {
            currentCell = FindLowestFScore(openList);
            // Add the current cell to the closed list and remove it from the open list
            closedList.Add(currentCell);
            openList.Remove(currentCell);

            // Has the target been found? 
            if (closedList.Contains(playerSpawn))
            {
                break;
            }

            List<CellS> adjacentCells = GetNeighbours(currentCell);
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
                    c.h = (int)ManhattanDistance(c);
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

    }

    // Tester to see if the pathfinding is currently working. 
    private List<CellS> BuildPath(CellS c)
    {
        List<CellS> bestPath = new List<CellS>();
        CellS currentCell = c;
        bestPath.Insert(0, currentCell);
        while (currentCell.parent != null)
        {
            currentCell = currentCell.parent;
            if (currentCell.parent != null)
            {
                bestPath.Insert(0, currentCell);
            }
        }
        return bestPath;
    }




    public void PlacePlayerSpawn()
    {
        // Choose a random point in a corridor and change that cell's type to the player's spawn. 
        bool validSpawn = false;
        CellS spawnTile = null;
        while (!validSpawn)
        {
            Vector2 pos = new Vector2(Random.Range(0, mazeRows), Random.Range(0, mazeColumns));
            //Debug.Log(string.Format("Spawn position: {0}, {1}", pos.x, pos.y));
            spawnTile = cells[pos];
            if (spawnTile.type == CellS.TileType.Corridor)
            {
                validSpawn = true;
            }
        }

        spawnTile.type = CellS.TileType.Spawn;

        playerSpawn = spawnTile;
    }


    public void PlaceLocks()
    {
        // Choose a random tile that connects a room to a corridor and place the lock there. 
        CellS lockTile = adjCells[Random.Range(0, adjCells.Count)];
        Instantiate(lockPrefab, lockTile.spawnPos, Quaternion.identity);


        // Instantiate the key in any valid corridor or room tile
        /*
		bool validKey = false;
		CellS keyTile = null;
		while (!validKey)
		{
			Vector2 pos = new Vector2 (Random.Range (0, mazeRows), Random.Range (0, mazeColumns));
			keyTile = cells[pos];
			if (keyTile.type != CellS.TileType.Wall)
			{
				validKey = true;
			}
		}

		Instantiate (keyPrefab, keyTile.spawnPos, Quaternion.identity);
		*/
        // Put they key specifically in a room. 
        /*
		int noOfRoomTiles = roomParent.transform.childCount;
		GameObject keyTile = roomParent.transform.GetChild(Random.Range(0, noOfRoomTiles)).gameObject;
		Instantiate (keyPrefab, keyTile.transform.position, Quaternion.identity);
		keySpawn = keyTile.transform.position;
		Debug.Log (string.Format ("Key spawn: {0},{1}", keySpawn.x, keySpawn.y));
		*/

        bool validSpawn = false;
        CellS spawnTile = null;
        while (!validSpawn)
        {
            Vector2 pos = new Vector2(Random.Range(0, mazeRows), Random.Range(0, mazeColumns));
            spawnTile = cells[pos];
            if (spawnTile.type == CellS.TileType.Room)
            {
                validSpawn = true;
            }
        }

        Instantiate(keyPrefab, spawnTile.spawnPos, Quaternion.identity);

        keySpawn = spawnTile;
    }

}
