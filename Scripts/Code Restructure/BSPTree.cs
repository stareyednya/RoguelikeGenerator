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
        SubDungeon root = new SubDungeon(new Rect(0, 0, rows, columns));
        CreateBSP(root);
        root.CreateRoom();
        DrawRooms(root);
        DrawCorridors(root);

        //// Create the tree to form the dungeon with and add the root region to it i.e the full thing.
        //RegionNode root = new RegionNode(new Vector2Int(0, 0), rows, columns);
        //dungeon = new DungeonTree(root);

        //// Split up the region
        //dungeon.SplitLeaves();



        //foreach (RegionNode l in dungeon.leaves)
        //{
        //    Debug.Log(string.Format("Region at bottom left {0}, {1}, width {2}, height {3}", l.bottomLeft.x, l.bottomLeft.y, l.width, l.height));
        //}

        

    }

    public void CreateBSP(SubDungeon subDungeon)
    {
        Debug.Log("Splitting sub-dungeon " + subDungeon.debugId + ": " + subDungeon.rect);
        //if (subDungeon.IAmLeaf() && splits < RoguelikeGenerator.instance.noOfRooms / 2)
        if (subDungeon.IAmLeaf())
        {
            // if the sub-dungeon is too large
            if (subDungeon.rect.width > RoguelikeGenerator.instance.maxWidth
              || subDungeon.rect.height > RoguelikeGenerator.instance.maxHeight
              || Random.Range(0.0f, 1.0f) > 0.25)
            {

                if (subDungeon.Split(RoguelikeGenerator.instance.minWidth, RoguelikeGenerator.instance.maxWidth))
                {
                    Debug.Log("Splitted sub-dungeon " + subDungeon.debugId + " in "
                      + subDungeon.left.debugId + ": " + subDungeon.left.rect + ", "
                      + subDungeon.right.debugId + ": " + subDungeon.right.rect);

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

        if (subdungeon.IAmLeaf())
        {
            for (int x = (int)subdungeon.room.x; x < subdungeon.room.xMax; x++)
            {
                for (int y = (int)subdungeon.room.y; y < subdungeon.room.yMax; y++)
                {
                    Vector2 gridPos = new Vector2(x, y);
                    //Debug.Log(string.Format("Poition {0}, {1}", gridPos.x, gridPos.y));
                    if (RoguelikeGenerator.instance.cells.ContainsKey(gridPos))
                        RoguelikeGenerator.instance.cells[gridPos].type = CellS.TileType.Room;
                }
            }
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
                    if (RoguelikeGenerator.instance.cells.ContainsKey(gridPos) && RoguelikeGenerator.instance.cells[gridPos].type != CellS.TileType.Room)
                    {
                        RoguelikeGenerator.instance.cells[gridPos].type = CellS.TileType.Corridor;
                    }
                }
            }
        }
    }


}
