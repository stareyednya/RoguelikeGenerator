using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class RoguelikeGenerator : MonoBehaviour {

    // Singleton instance of RLG to allow variable access and editing in inspector without using static everywhere. 
    public static RoguelikeGenerator instance = null;
    
    // Used with methods that make use of the cell structure method. 
    public Dictionary<Vector2, CellS> cells;
    // Used with methods that make use of early instantiation methods. 
    public Dictionary<Vector2, Cell> instantiateCells = new Dictionary<Vector2, Cell>();
    // Contain groups of cells that make up a room with their information. 
    public List<Room> rooms = new List<Room>();
    // List for cells that're found to be next to a corridor in a room. 
    public List<CellS> adjCells = new List<CellS>();
    public List<Cell> adjCellsInstances = new List<Cell>();

    // Object pools. 
    // Hold and locate all the cells in the maze. 
    public Dictionary<Vector2, Cell> cellsP = new Dictionary<Vector2, Cell>();
    // Matching set of wall objects to instantiate.
    public Dictionary<Vector2, Cell> wallsP = new Dictionary<Vector2, Cell>();
    public Dictionary<Vector2, Cell> roomsP = new Dictionary<Vector2, Cell>();

    // How large a final result is wanted. WIll be passed to maze generation as is if not doubling dungeon, halved otherwise. 
    public int dungeonRows;
    public int dungeonColumns;

    [HideInInspector]
    public int mazeRows;
    [HideInInspector]
    public int mazeColumns;

    // How much rock to fill back in. 
    public int sparseness;
    private int actualSparseness;

    // The chance of a dead end being filled back in. 
    public int removalChance;

    // Parameters for the number of rooms to include, and their range of possible dimensions. 
    public int noOfRooms;
    public int minWidth;
    public int maxWidth;
    public int minHeight;
    public int maxHeight;

    // How many lock and key pairs to place. 
    public int noOfLocks;
   

    // The prefab to use as the cell icon. 
	public GameObject cellPrefab;
	// The tile to use as a wall to fill in dead ends with.
	public GameObject wallPrefab;
	// A tile to differentiate room tiles from the floor.
	public GameObject roomPrefab;
    // The tile to represent where the player spawns.
    public GameObject spawnPrefab;
    // The tile to represent where the player leaves the level. 
    public GameObject exitPrefab;

    // Arrays of lock and key prefabs, both in matching order of their colour to place the same two at the same time.
    public GameObject[] lockPrefabs;
    public GameObject[] keyPrefabs;
    // To hold the spawn of lock and key pairs.
    public LockAndKey[] lockKeySpawns;

    // Keep a unique reference to the specific tile the player spawns at- will be used for A*.
    public CellS playerSpawn;
    public Cell playerSpawnInstance;

    public CellS exitSpawn;

    // Options for the full level generation to run. 
    public bool MWRInstantiate; // default MWR with all the instantation 
	public bool MWRPooling; // MWR using pooling for swapping floor and wall tiles
	public bool MWRStruct; // MWR instantiating all tiles at the end 
    public bool BSPTree; // BSP tree generation (cell struct)
    public bool doubleDungeon; // space out the dungeon or not. 
    public bool seed; // generate the same map every time
    public bool test; // Immediately restart the process for use with a mouse macro and output to CSV

    // Currently storing the time since the scene started in here to save processing time with a write to disk for a debug statement. 
    public float startupTime;
    public float gridGenTime;
    public float mazeGenTime;
    public float sparseTime;
    public float loopTime;
    public float doublingTime;
    public float roomTime;
    public float lockTime;
    public float instantiationTime;
    public float totalGenTime;

    // Organise the editor hierarchy. 
    [HideInInspector]
    public GameObject mazeParent;
    [HideInInspector]
    public GameObject wallParent;
    [HideInInspector]
    public GameObject roomParent;

    public int replacementCount = 0;

    // Use this for initialization
    void Start () {
        // To account for unity splash screen.
        startupTime = Time.realtimeSinceStartup;
        totalGenTime = startupTime;

        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject); // So we do not have more than one instance.

        if (seed)
            Random.InitState(104);

        Debug.Log(string.Format("Random seed: {0}", Random.state));

        actualSparseness = sparseness;
        if (doubleDungeon)
        {
            mazeRows = dungeonRows / 2;
            mazeColumns = dungeonColumns / 2;
            sparseness /= 4;
        }
        else
        {
            mazeRows = dungeonRows;
            mazeColumns = dungeonColumns;
        }

        mazeParent = new GameObject();
        mazeParent.transform.position = Vector2.zero;
        mazeParent.name = "Maze";


        wallParent = new GameObject();
        wallParent.transform.position = Vector2.zero;
        wallParent.name = "Walls";

        roomParent = new GameObject();
        roomParent.transform.position = Vector2.zero;
        roomParent.name = "Rooms";

        if (MWRStruct)
        {
           new MWRCellStruct(mazeRows, mazeColumns);
            //yield return null;
            
           PlacePlayerSpawn();

            // Safe guards for infinite loop of looking for new rooms or running out of prefabs.
            if (noOfLocks >= noOfRooms)
            {
                noOfLocks = noOfRooms - 1;
            }
            if (noOfLocks > lockPrefabs.Length)
            {
                noOfLocks = lockPrefabs.Length;
            }
            lockKeySpawns = new LockAndKey[noOfLocks];
            new LockKeyPlacer(noOfLocks);

            lockTime = Time.realtimeSinceStartup - totalGenTime;
            totalGenTime += lockTime;

            PlaceExit();
            ExplorePath();

            InstantiateGrid();
            instantiationTime = Time.realtimeSinceStartup - totalGenTime;
            totalGenTime += instantiationTime;
        }
        else if (MWRPooling)
        {
            new MWRPooling(mazeRows, mazeColumns);
           // yield return null;

            // Safe guards for infinite loop of looking for new rooms or running out of prefabs.
            if (noOfLocks >= noOfRooms)
            {
                noOfLocks = noOfRooms - 1;
            }
            if (noOfLocks > lockPrefabs.Length)
            {
                noOfLocks = lockPrefabs.Length;
            }
            lockKeySpawns = new LockAndKey[noOfLocks];
            new LockKeyPlacer(noOfLocks, true);
        }
        else if (MWRInstantiate)
        {
            new MWRInstantiate(mazeRows, mazeColumns);
           // yield return null;
            // Safe guards for infinite loop of looking for new rooms or running out of prefabs.
            if (noOfLocks >= noOfRooms)
            {
                noOfLocks = noOfRooms - 1;
            }
            if (noOfLocks > lockPrefabs.Length)
            {
                noOfLocks = lockPrefabs.Length;
            }
            lockKeySpawns = new LockAndKey[noOfLocks];
            new LockKeyPlacer(noOfLocks, true);
        }
        else if (BSPTree)
        {
            cells = new Dictionary<Vector2, CellS>();
            new BSPTree(mazeRows, mazeColumns);
            //yield return null;
            PlacePlayerSpawn();

            // Safe guards for infinite loop of looking for new rooms or running out of prefabs.
            if (noOfLocks >= noOfRooms)
            {
                noOfLocks = noOfRooms - 1;
            }
            if (noOfLocks > lockPrefabs.Length)
            {
                noOfLocks = lockPrefabs.Length;
            }
            

            lockKeySpawns = new LockAndKey[noOfLocks];
            new LockKeyPlacer(noOfLocks);

            lockTime = Time.realtimeSinceStartup - totalGenTime;
            totalGenTime += lockTime;

            //List<CellS> bestPath = Pathfinding.BuildPath(playerSpawn);
            //foreach (CellS c in bestPath)
            //{
            //    Debug.Log(string.Format("{0}, {1}", c.gridPos.x, c.gridPos.y));
            //}

            PlaceExit();

            InstantiateGrid();
            instantiationTime = Time.realtimeSinceStartup - totalGenTime;
            totalGenTime += instantiationTime;
        }

        if (test)
            WriteCSV();

       
    }

    public void PlacePlayerSpawn()
    {
        // Choose a random point in a corridor and change that cell's type to the player's spawn. 
        bool validSpawn = false;
        CellS spawnTile = null;
        while (!validSpawn)
        {
            Vector2 pos = new Vector2(Random.Range(0, dungeonRows), Random.Range(0, dungeonColumns));
            spawnTile = cells[pos];
            if (spawnTile.type == CellS.TileType.Corridor)
            {
                validSpawn = true;
            }
        }

        spawnTile.type = CellS.TileType.Spawn;

        playerSpawn = spawnTile;
    }

    // Probably needs some kind of pathfinding check to decide if it's too close to the spawn or not. 
    public void PlaceExit()
    {
        CellS exitPos;
        // If locks have been used, place the exit behind the last one.
        if (noOfLocks > 0)
        {
            Room room = rooms[lockKeySpawns[lockKeySpawns.Length - 1].lockSpawn.roomNo - 1];
            // Choose a random cell in this room to be the exit. 
            do
            {
                exitPos = room.roomCells[Random.Range(0, room.roomCells.Count)];
            } while (exitPos == lockKeySpawns[lockKeySpawns.Length - 1].lockSpawn);
            exitPos.type = CellS.TileType.Exit;

            exitSpawn = exitPos;
        }
        // Else just place it in a random room. 
        else
        {
            Room room = rooms[Random.Range(0, rooms.Count)];
            // Choose a random cell in this room to be the exit. 
            do
            {
                exitPos = room.roomCells[Random.Range(0, room.roomCells.Count)];
            } while (adjCells.Contains(exitPos));
            exitPos.type = CellS.TileType.Exit;

            exitSpawn = exitPos;
        }
        exitSpawn = exitPos;

    }

    // Instantiate our finished grid. 
    public void InstantiateGrid()
    {
        

        foreach (KeyValuePair<Vector2, CellS> c in cells)
        {
            CellS currentCell = c.Value;
            switch (currentCell.type)
            {
                default:
                case CellS.TileType.Corridor:
                    currentCell.cellObject = Instantiate(cellPrefab, currentCell.spawnPos, cellPrefab.transform.rotation);
                    // Child the new cell to the maze parent. 
                    currentCell.cellObject.transform.parent = mazeParent.transform;
                    // Set the name of this cellObject.
                    currentCell.cellObject.name = "Cell - X:" + currentCell.gridPos.x + " Y:" + currentCell.gridPos.y;
                    currentCell.cScript = currentCell.cellObject.GetComponent<CellScript>();
                    // Set up walls according to the cell's marked walls. Deactivating as default instantiation value is all active.
                    if (!currentCell.wallD)
                        currentCell.cScript.wallD.SetActive(false);
                    if (!currentCell.wallU)
                        currentCell.cScript.wallU.SetActive(false);
                    if (!currentCell.wallL)
                        currentCell.cScript.wallL.SetActive(false);
                    if (!currentCell.wallR)
                        currentCell.cScript.wallR.SetActive(false);
                    break;
                case CellS.TileType.Wall:
                    currentCell.cellObject = Instantiate(wallPrefab, currentCell.spawnPos, wallPrefab.transform.rotation);
                    // Child the new cell to the maze parent. 
                    currentCell.cellObject.transform.parent = wallParent.transform;
                    // Set the name of this cellObject.
                    currentCell.cellObject.name = "Wall - X:" + currentCell.gridPos.x + " Y:" + currentCell.gridPos.y;
                    break;
                case CellS.TileType.Room:
                    currentCell.cellObject = Instantiate(roomPrefab, currentCell.spawnPos, roomPrefab.transform.rotation);
                    // Child the new cell to the maze parent. 
                    currentCell.cellObject.transform.parent = roomParent.transform;
                    // Set the name of this cellObject.
                    currentCell.cellObject.name = "Room - X:" + currentCell.gridPos.x + " Y:" + currentCell.gridPos.y;
                    // Set up walls according to the cell's marked walls. Deactivating as default instantiation value is all active.
                    currentCell.cScript = currentCell.cellObject.GetComponent<CellScript>();
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
                case CellS.TileType.Exit:
                    currentCell.cellObject = Instantiate(exitPrefab, currentCell.spawnPos, exitPrefab.transform.rotation);
                    break;
            }
        }

        for (int i = 0; i < noOfLocks; i++)
        {
            Instantiate(lockPrefabs[i], lockKeySpawns[i].lockSpawn.spawnPos, Quaternion.identity);
            Instantiate(keyPrefabs[i], lockKeySpawns[i].keySpawn.spawnPos, Quaternion.identity);
        }
    }

    // Check the amount of exploration that needs to be done from the player's spawn to reaching the exit- the critical path
    // Currently forward pathfinding just going straight from the start to the exit with no concern for locks, but needs to update the target depending on locks it finds later 
    public void ExplorePath()
    {
        PathfindingReset();
        // Target position is the player's spawn - stored in playerSpawn already as a CellS 
        List<CellS> openList = new List<CellS>();
        List<CellS> closedList = new List<CellS>();
        // Add the starting position to the open list - the player spawn.
        CellS startCell = playerSpawn;
        openList.Add(startCell);
        CellS currentCell = null;

        while (openList.Count > 0)
        {
            currentCell = Pathfinding.FindLowestFScore(openList);
            // Add the current cell to the closed list and remove it from the open list
            closedList.Add(currentCell);
            openList.Remove(currentCell);

            // Has the target been found? 
            if (closedList.Contains(exitSpawn))
            {
                break;
            }

            List<CellS> adjacentCells = Pathfinding.GetTraversableNeighbours(currentCell);
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

        List<CellS> bestPath = Pathfinding.BuildPath(exitSpawn);
        foreach (CellS c in bestPath)
        {
            Debug.Log(string.Format("{0}, {1}", c.gridPos.x, c.gridPos.y));
        }
        Debug.Log(string.Format("Number of cells on the critical path: {0} ", bestPath.Count));

    }

    public void PathfindingReset()
    {
        foreach (KeyValuePair<Vector2, CellS> c in cells)
        {
            CellS cell = c.Value;
            cell.g = 0;
            cell.h = 0;
            cell.parent = null;
        }
    }


    void WriteCSV()
    {
        string filePath = "C:\\Users\\Eleanor\\Documents\\Computer Science\\Stage 3\\Dissertation\\Results\\output.csv";

        TextWriter tw = new StreamWriter(filePath, true);

        // MWR/BSP generation
        //tw.WriteLine(dungeonRows + "x" + dungeonColumns + "," + actualSparseness + "," + noOfRooms + "," + minWidth + " to " + maxWidth + "," + minHeight + " to " + maxHeight + "," + startupTime + ","
        //            + gridGenTime + "," + mazeGenTime + "," + sparseTime + "," + loopTime + "," + doublingTime + "," + roomTime + "," + instantiationTime + "," + totalGenTime);

        // Lock placement
        tw.WriteLine(noOfLocks + "," + startupTime + "," + gridGenTime + "," + mazeGenTime + "," + sparseTime + "," + loopTime + "," + doublingTime + "," + roomTime + "," + lockTime + "," + instantiationTime + "," + replacementCount + "," + totalGenTime);



        tw.Flush();
        tw.Close();

        //EditorApplication.isPlaying = false;
        
    }
    

   
}
