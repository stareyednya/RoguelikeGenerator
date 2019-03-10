using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSP : MonoBehaviour
{
    public int rows;
    public int columns;
    private Dictionary<Vector2, CellS> cells = new Dictionary<Vector2, CellS>();
    private List<CellS> adjCells = new List<CellS>();
    private List<Room> rooms = new List<Room>();

    // Cell size to determine how far apart to place cells during generation. 
    private float cellSize;

    private DungeonTree dungeon;

    private static int splits = 0;

    private static int roomCounter = 1;

    // The prefab to use as the cell icon. 
    [SerializeField]
    private GameObject cellPrefab;
    // The tile to use as a wall to fill in dead ends with.
    [SerializeField]
    private GameObject wallPrefab;
    // A tile to differentiate room tiles from the floor.
    [SerializeField]
    private GameObject roomPrefab;

    public int noOfRooms;
    public int minWidth;
    public int maxWidth;
    public int minHeight;
    public int maxHeight;

    // Organise the editor hierarchy. 
    [HideInInspector]
    public GameObject mazeParent;
    [HideInInspector]
    public GameObject wallParent;
    [HideInInspector]
    public GameObject roomParent;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        mazeParent = new GameObject();
        mazeParent.transform.position = Vector2.zero;
        mazeParent.name = "Maze";


        wallParent = new GameObject();
        wallParent.transform.position = Vector2.zero;
        wallParent.name = "Walls";

        roomParent = new GameObject();
        roomParent.transform.position = Vector2.zero;
        roomParent.name = "Rooms";

        Debug.Log("Creating layout");
        yield return StartCoroutine(CreateLayout());

        Debug.Log("Creating BSP");
        SD root = new SD(new Rect(0, 0, rows, columns), null, noOfRooms, minWidth, minHeight,maxWidth, maxHeight);
        yield return StartCoroutine(CreateBSP(root));

        Debug.Log("Creating Rooms");
        root.CreateRoom();
        yield return null;
        Debug.Log("Getting all rooms");
        root.GetAllRooms();
        yield return null;

        Debug.Log("Drawing corridors");
        yield return StartCoroutine(DrawCorridors(root));

        Debug.Log("Drawing rooms");
        yield return StartCoroutine(DrawRooms(root));

        InstantiateGrid();
        Debug.Log("Done");

    }

    public IEnumerator CreateLayout()
    {
        // Detemrine the size of the cells to place from the tile we're using. 
        cellSize = cellPrefab.transform.localScale.x;

        Vector2 startPos = new Vector2(-(cellSize * (columns / 2)) + (cellSize / 2), -(cellSize * (rows / 2)) + (cellSize / 2));
        Vector2 spawnPos = startPos;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2 gridPos = new Vector2(x, y);
                cells[gridPos] = MazeUtils.GenerateCell(spawnPos, gridPos, CellS.TileType.Wall);
                spawnPos.y += cellSize;
            }

            // Reset spawn position and move up a row. 
            spawnPos.y = startPos.y;
            spawnPos.x += cellSize;
        }

        yield return null;
    }

    public IEnumerator CreateBSP(SD subDungeon)
    {
        Debug.Log("Splitting sub-dungeon " + subDungeon.debugId + ": " + subDungeon.rect);
        //if (subDungeon.IAmLeaf() && splits < RoguelikeGenerator.instance.noOfRooms / 2)
        if (subDungeon.IAmLeaf() && splits <= noOfRooms + 1 / 2)
        {
            // if the sub-dungeon is too large
            if (subDungeon.rect.width > maxWidth
              || subDungeon.rect.height > maxHeight
              || Random.Range(0.0f, 1.0f) > 0.25)
            {

                if (subDungeon.Split(minWidth, maxWidth))
                {
                    Debug.Log("Splitted sub-dungeon " + subDungeon.debugId + " in "
                      + subDungeon.left.debugId + ": " + subDungeon.left.rect + ", "
                      + subDungeon.right.debugId + ": " + subDungeon.right.rect);

                   // CreateBSP(subDungeon.left);
                    yield return StartCoroutine(CreateBSP(subDungeon.left));
                    //CreateBSP(subDungeon.right);
                    yield return StartCoroutine(CreateBSP(subDungeon.right));

                    splits++;
                }
            }
        }

        //yield return null;
    }

    public IEnumerator DrawCorridors(SD subdungeon)
    {
        if (subdungeon == null)
            yield break;

        yield return StartCoroutine(DrawCorridors(subdungeon.left));
        yield return StartCoroutine(DrawCorridors(subdungeon.right));

        foreach (Rect corridor in subdungeon.corridors)
        {
            for (int x = (int)corridor.x; x < corridor.xMax; x++)
            {
                for (int y = (int)corridor.y; y < corridor.yMax; y++)
                {
                    Vector2 gridPos = new Vector2(x, y);

                    //if (RoguelikeGenerator.instance.cells.ContainsKey(gridPos) && RoguelikeGenerator.instance.cells[gridPos].type != CellS.TileType.Room)
                    if (cells.ContainsKey(gridPos))
                    {
                        CellS c = cells[gridPos];
                        if (c.type != CellS.TileType.Room)
                        {
                            c.type = CellS.TileType.Corridor;
                        }


                        List<CellS> neighbours = MazeUtils.GetNeighbours(c, ref cells);

                        foreach (CellS n in neighbours)
                        {
                            //Debug.Log(string.Format("c {0}, {1} a {2}, n {3}, {4} a {5}", c.gridPos.x, c.gridPos.y, c.type, n.gridPos.x, n.gridPos.y, n.type));
                            if (n.type == CellS.TileType.Corridor)
                            {
                                MazeUtils.CompareWalls(c, n);

                            }
                        }

                    }
                }
            }
        }

        //yield return null;
    }

    public IEnumerator DrawRooms(SD subdungeon)
    {
        if (subdungeon == null)
            yield break;

        if (subdungeon.IAmLeaf() && !subdungeon.room.Equals(new Rect(-1, -1, 0, 0)))
        {
            Room r = new Room();
            r.roomNo = roomCounter;
            for (int x = (int)subdungeon.room.x; x < subdungeon.room.xMax; x++)
            {
                for (int y = (int)subdungeon.room.y; y < subdungeon.room.yMax; y++)
                {
                    Vector2 gridPos = new Vector2(x, y);
                    Debug.Log(string.Format("{0}, {1}", x, y));
                    CellS c = cells[gridPos];
                    c.type = CellS.TileType.Room;
                    c.roomNo = roomCounter;


                    // Determine which walls on this room cell need to be active to block it off. 
                    // Deactivate all walls first. 
                    // Only need to do special wall check for left and below cells due to the room cells being instantiated upwards column by column.
                    c.wallL = false;
                    c.wallR = false;
                    c.wallU = false;
                    c.wallD = false;
                    Vector2 nPos = gridPos;
                    // Left neighbour
                    nPos = gridPos + MazeUtils.possibleNeighbours[0];
                    if (cells.ContainsKey(nPos))
                    {
                        CellS n = cells[nPos];
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
                    nPos = gridPos + MazeUtils.possibleNeighbours[1];
                    if (cells.ContainsKey(nPos))
                    {
                        CellS n = cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallR = true;
                        }
                    }

                    // Above neighbour
                    nPos = gridPos + MazeUtils.possibleNeighbours[2];
                    if (cells.ContainsKey(nPos))
                    {
                        CellS n = cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallU = true;
                        }
                    }

                    // Below neighbour
                    nPos = gridPos + MazeUtils.possibleNeighbours[3];
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

                    r.roomCells.Add(c);

                    //Debug.Log(string.Format("Poition {0}, {1}", gridPos.x, gridPos.y));
                    //if (RoguelikeGenerator.instance.cells.ContainsKey(gridPos))
                    //    RoguelikeGenerator.instance.cells[gridPos].type = CellS.TileType.Room;
                }
            }

            foreach (CellS c in r.roomCells)
            {
                Vector2 nPos = c.gridPos;
                // Left neighbour
                nPos = c.gridPos + MazeUtils.possibleNeighbours[0];
                if (cells.ContainsKey(nPos))
                {
                    CellS n = cells[nPos];
                    if (n.type == CellS.TileType.Corridor && n.wallR)
                    {
                        c.wallL = true;
                    }
                    else if (n.type == CellS.TileType.Corridor && !n.wallR)
                    {
                        adjCells.Add(c);
                    }
                }

                // Right neighbour 
                nPos = c.gridPos + MazeUtils.possibleNeighbours[1];
                if (cells.ContainsKey(nPos))
                {
                    CellS n = cells[nPos];
                    if (n.type == CellS.TileType.Corridor && n.wallL)
                    {
                        c.wallR = true;
                    }
                    else if (n.type == CellS.TileType.Corridor && !n.wallL)
                        adjCells.Add(c);
                }

                // Above neighbour
                nPos = c.gridPos + MazeUtils.possibleNeighbours[2];
                if (cells.ContainsKey(nPos))
                {
                    CellS n = cells[nPos];
                    if (n.type == CellS.TileType.Corridor && n.wallD)
                    {
                        c.wallU = true;
                    }
                    else if (n.type == CellS.TileType.Corridor && !n.wallD)
                        adjCells.Add(c);
                }

                // Below neighbour
                nPos = c.gridPos + MazeUtils.possibleNeighbours[3];
                if (cells.ContainsKey(nPos))
                {
                    CellS n = cells[nPos];
                    if (n.type == CellS.TileType.Corridor && n.wallU)
                    {
                        c.wallD = true;
                    }
                    else if (n.type == CellS.TileType.Corridor && !n.wallU)
                        adjCells.Add(c);
                }
            }

            rooms.Add(r);
            roomCounter++;
        }
        else
        {
            yield return StartCoroutine(DrawRooms(subdungeon.left));
            yield return StartCoroutine(DrawRooms(subdungeon.right));
        }
        //yield return null;
    }

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
            }
        }
        
    }
}
