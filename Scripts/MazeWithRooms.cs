using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeWithRooms : MonoBehaviour {
	// Hold and locate all the cells in the maze. 
	private Dictionary<Vector2, CellS> cells = new Dictionary<Vector2, CellS>();

	// How many cells TALL the maze will be.
	public int mazeRows;
	// how many cells WIDE the maze will be. 
	public int mazeColumns;

	// How much rock to fill back in. 
	public int sparseness;

	// The chance of a dead end being filled back in. 
	public int removalChance;

	// Currently storing the time since the scene started in here to save processing time with a write to disk for a debug statement. 
	public float timeSinceGeneration;

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

	// List of all cells found to be dead ends, i.e, 3 walls are active. 
	private List<CellS> deadEnds = new List<CellS> ();

	// Tester to see what rooms is finding
	private GameObject roomParent;

	// Use this for initialization
	void Start () 
	{
		GenerateMaze (mazeRows, mazeColumns);
		InstantiateGrid ();
		timeSinceGeneration =  Time.realtimeSinceStartup;
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
		currentCell = cells [new Vector2 (xStart, yStart)];

		// Mark our starting cell as visited.
		currentCell.visited = true;
		unvisited = (mazeRows * mazeColumns) - 1;
		// Perform recursive backtracking to create a maze. 
		RecursiveBacktracking ();

		// Fill in the dead ends with some walls.
		FindDeadEnds ();
		FillInEnds();

		// Remove some dead ends by making the maze imperfect.
		RemoveLoops ();

		PlaceRooms ();

	}

	// Create a cell based on the given position. 
	public void GenerateCell(Vector2 pos, Vector2 keyPos)
	{
		CellS newCell = new CellS ();
		// Store a reference to this position in the grid.
		newCell.gridPos = keyPos;
		newCell.spawnPos = pos;
		// Set the defaults of this cell. 
		newCell.wallD = true;
		newCell.wallU = true;
		newCell.wallL = true;
		newCell.wallR = true;
		newCell.visited = false;
		newCell.type = CellS.TileType.Corridor;

		// Store this created cell.
		cells[keyPos] = newCell;
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
				if (cells.ContainsKey(pos))
				{
					CellS testCell = cells [pos];
					if (noOfWalls (testCell) >= 3 && testCell.type != CellS.TileType.Wall)
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
				while (noOfWalls(cells[nextPos]) == 3 && nextPos.x < mazeRows - 1 && nextPos.y < mazeColumns - 1 && nextPos.x > 0 && nextPos.y > 0)
				{
					// Mark the current cell as a wall if it hasn't been done already. 
					if (cells [nextPos + direction].type != CellS.TileType.Wall)
					{
						cells [nextPos + direction].type = CellS.TileType.Wall;
					}
						
					// Move along the passageway.
					nextPos += direction;
				}


				// Add a wall to the opposite direction on our current non-dead end cell- the way back into the passage we just filled in. 
				if (direction.y == -1)
				{
					cells [nextPos].wallU = true;
				}
				else if (direction.y == 1)
				{
					cells [nextPos].wallD = true;
				}
				else if (direction.x == -1)
				{
					cells [nextPos].wallR = true;
				}
				else if (direction.x == 1)
				{
					cells [nextPos].wallL = true;
				}

				// If our current cell now has 3 walls, it's a dead end, so update our dead end list.
				// else we're done with this dead end, so remove it.
				if (noOfWalls(cells [nextPos]) == 3)
				{
					deadEnds [j] = cells [nextPos];
				}
				else
				{
					deadEnds.RemoveAt(j);
				}

			}


		}
		// Set all tiles which are now walls tohave all walls active to avoid random gaps against walls and for later calculation.
		// All activated at the end to avoid confusing loop adding calculations.
		foreach (KeyValuePair<Vector2, CellS> c in cells)
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
			Debug.Log (string.Format ("Dead end: {0}, {1}", deadEnds [i].gridPos.x, deadEnds [i].gridPos.y));
		}

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
							if (cells.ContainsKey (nPos))
								neighbours.Add(cells [nPos]);
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

							for (int j = 0; j < possibleNeighbours.Length; j++) {
								// Find the position of a neighbour on the grid, relative to the current cell. 
								Vector2 nPos = currentPos + possibleNeighbours [j];
								// Check the neighbouring cell exists and whether it's a corridor.
								if (cells.ContainsKey (nPos) && cells[nPos].type == CellS.TileType.Corridor)
									isAdjacentC = true;

								// For each cell overlapping a room, add 100 to score.
								if (cells[cCell].type == CellS.TileType.Room)
								{
									currentScore += 100;
								}

								// For each cell next to a corridor, add 1.
								if (cells[cCell].type == CellS.TileType.Wall && isAdjacentC)
								{
									currentScore += 1;
								}

								// For each cell overlapping a corridor, add 3 to the score
								if (cells[cCell].type == CellS.TileType.Corridor)
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
				}

			}

			// Place the room in the upper left position where the best score was. 
			for (int x = (int)bestPos.x; x < (int)bestPos.x + roomWidth; x++) 
			{
				for (int y = (int)bestPos.y; y < (int)bestPos.y + roomHeight; y++) 
				{
					Vector2 pos = new Vector2(x, y);
					cells [pos].type = CellS.TileType.Room;
				}
			}
			// For every cell adjacent to a room/corridor, add a doorway. 
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
			if (cells.ContainsKey (nPos))
				nCell = cells [nPos];

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
			//Debug.Log(string.Format("Removing left wall of cell {0}, {1}", currentCell.gridPos.x, currentCell.gridPos.y));
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

	public int noOfWalls(CellS c)
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

}
