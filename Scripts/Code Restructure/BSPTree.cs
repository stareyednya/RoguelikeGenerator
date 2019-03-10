using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSPTree 
{
    private int rows;
    private int columns;

    // Cell size to determine how far apart to place cells during generation. 
    private float cellSize;

    private DungeonTree dungeon;

    private static int splits = 0;

    private static int roomCounter = 1;

    public BSPTree(int gridRows, int gridColumns)
    {
        rows = gridRows;
        columns = gridColumns;
        // Generate the grid filled with wall tiles. 
        CreateLayout();
        
        GenerateDungeon();

    }

    public void CreateLayout()
    {
        // Detemrine the size of the cells to place from the tile we're using. 
        cellSize = RoguelikeGenerator.instance.cellPrefab.transform.localScale.x;
        
        Vector2 startPos = new Vector2(-(cellSize * (columns / 2)) + (cellSize / 2), -(cellSize * (rows / 2)) + (cellSize / 2));
        Vector2 spawnPos = startPos;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2 gridPos = new Vector2(x, y);
                RoguelikeGenerator.instance.cells[gridPos] = MazeUtils.GenerateCell(spawnPos, gridPos, CellS.TileType.Wall);
                spawnPos.y += cellSize;
            }

            // Reset spawn position and move up a row. 
            spawnPos.y = startPos.y;
            spawnPos.x += cellSize;
        }
    }

    public void GenerateDungeon()
    {
        SubDungeon root = new SubDungeon(new Rect(0, 0, rows, columns), null);
        CreateBSP(root);

        // Mark how long it took for the grid to be generated.
        RoguelikeGenerator.instance.gridGenTime = Time.realtimeSinceStartup - RoguelikeGenerator.instance.totalGenTime;
        RoguelikeGenerator.instance.totalGenTime += RoguelikeGenerator.instance.gridGenTime;

        root.CreateRoom();
        //root.GetAllRooms();
        //root.CreateLoops();

        DrawCorridors(root);

        RoguelikeGenerator.instance.mazeGenTime = Time.realtimeSinceStartup - RoguelikeGenerator.instance.totalGenTime;
        RoguelikeGenerator.instance.totalGenTime += RoguelikeGenerator.instance.mazeGenTime;
        DrawRooms(root);

        RoguelikeGenerator.instance.roomTime = Time.realtimeSinceStartup - RoguelikeGenerator.instance.totalGenTime;
        RoguelikeGenerator.instance.totalGenTime += RoguelikeGenerator.instance.roomTime;

        
    }

    public void CreateBSP(SubDungeon subDungeon)
    {
        //Debug.Log("Splitting sub-dungeon " + subDungeon.debugId + ": " + subDungeon.rect);
        //if (subDungeon.IAmLeaf() && splits < RoguelikeGenerator.instance.noOfRooms / 2)
        if (subDungeon.IAmLeaf() && splits <= RoguelikeGenerator.instance.noOfRooms + 1 / 2)
        {
            // if the sub-dungeon is too large
            if (subDungeon.rect.width > RoguelikeGenerator.instance.maxWidth
              || subDungeon.rect.height > RoguelikeGenerator.instance.maxHeight
              || Random.Range(0.0f, 1.0f) > 0.25)
            {

                if (subDungeon.Split(RoguelikeGenerator.instance.minWidth, RoguelikeGenerator.instance.maxWidth))
                {
                    //Debug.Log("Splitted sub-dungeon " + subDungeon.debugId + " in "
                    //  + subDungeon.left.debugId + ": " + subDungeon.left.rect + ", "
                    //  + subDungeon.right.debugId + ": " + subDungeon.right.rect);

                    CreateBSP(subDungeon.left);
                    CreateBSP(subDungeon.right);

                    splits++;
                }
            }
        }
    }

    public void DrawRooms(SubDungeon subdungeon)
    {
        if (subdungeon == null)
            return;

        if (subdungeon.IAmLeaf() && !subdungeon.room.Equals(new Rect(-1, -1, 0, 0) ))
        {
            Room r = new Room();
            r.roomNo = roomCounter;
            for (int x = (int)subdungeon.room.x; x < subdungeon.room.xMax; x++)
            {
                for (int y = (int)subdungeon.room.y; y < subdungeon.room.yMax; y++)
                {
                    Vector2 gridPos = new Vector2(x, y);
                    CellS c = RoguelikeGenerator.instance.cells[gridPos];
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
                    nPos = gridPos + MazeUtils.possibleNeighbours[1];
                    if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                    {
                        CellS n = RoguelikeGenerator.instance.cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallR = true;
                        }
                    }

                    // Above neighbour
                    nPos = gridPos + MazeUtils.possibleNeighbours[2];
                    if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                    {
                        CellS n = RoguelikeGenerator.instance.cells[nPos];
                        if (n.type == CellS.TileType.Wall || (n.type == CellS.TileType.Room && c.roomNo != n.roomNo))
                        {
                            c.wallU = true;
                        }
                    }

                    // Below neighbour
                    nPos = gridPos + MazeUtils.possibleNeighbours[3];
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
                if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                {
                    CellS n = RoguelikeGenerator.instance.cells[nPos];
                    if (n.type == CellS.TileType.Corridor && n.wallR)
                    {
                        c.wallL = true;
                    }
                    else if (n.type == CellS.TileType.Corridor && !n.wallR)
                    {
                        RoguelikeGenerator.instance.adjCells.Add(c);
                    }
                }

                // Right neighbour 
                nPos = c.gridPos + MazeUtils.possibleNeighbours[1];
                if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                {
                    CellS n = RoguelikeGenerator.instance.cells[nPos];
                    if (n.type == CellS.TileType.Corridor && n.wallL)
                    {
                        c.wallR = true;
                    }
                    else if (n.type == CellS.TileType.Corridor && !n.wallL)
                        RoguelikeGenerator.instance.adjCells.Add(c);
                }

                // Above neighbour
                nPos = c.gridPos + MazeUtils.possibleNeighbours[2];
                if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                {
                    CellS n = RoguelikeGenerator.instance.cells[nPos];
                    if (n.type == CellS.TileType.Corridor && n.wallD)
                    {
                        c.wallU = true;
                    }
                    else if (n.type == CellS.TileType.Corridor && !n.wallD)
                        RoguelikeGenerator.instance.adjCells.Add(c);
                }

                // Below neighbour
                nPos = c.gridPos + MazeUtils.possibleNeighbours[3];
                if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                {
                    CellS n = RoguelikeGenerator.instance.cells[nPos];
                    if (n.type == CellS.TileType.Corridor && n.wallU)
                    {
                        c.wallD = true;
                    }
                    else if (n.type == CellS.TileType.Corridor && !n.wallU)
                        RoguelikeGenerator.instance.adjCells.Add(c);
                }
            }

            RoguelikeGenerator.instance.rooms.Add(r);
            roomCounter++;
        }
        else
        {
            DrawRooms(subdungeon.left);
            DrawRooms(subdungeon.right);
        }
    }

    public void DrawCorridors(SubDungeon subdungeon)
    {
        if (subdungeon == null)
            return;

        DrawCorridors(subdungeon.left);
        DrawCorridors(subdungeon.right);

        foreach (Rect corridor in subdungeon.corridors)
        {
            for (int x = (int)corridor.x; x < corridor.xMax; x++)
            {
                for (int y = (int)corridor.y; y < corridor.yMax; y++)
                {
                    Vector2 gridPos = new Vector2(x, y);
                    
                    //if (RoguelikeGenerator.instance.cells.ContainsKey(gridPos) && RoguelikeGenerator.instance.cells[gridPos].type != CellS.TileType.Room)
                    if (RoguelikeGenerator.instance.cells.ContainsKey(gridPos))
                    {
                        CellS c = RoguelikeGenerator.instance.cells[gridPos];
                        if (c.type != CellS.TileType.Room)
                        {
                            c.type = CellS.TileType.Corridor;
                        }
                            

                        List<CellS> neighbours = MazeUtils.GetNeighbours(c);

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
    }


}
