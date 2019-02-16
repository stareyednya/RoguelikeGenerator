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
        // Create the tree to form the dungeon with and add the root region to it i.e the full thing.
        RegionNode root = new RegionNode(new Vector2Int(0, 0), rows, columns);
        dungeon = new DungeonTree(root);

        // Split up the regions.
        dungeon.SplitLeaves();

        //foreach (RegionNode l in dungeon.leaves)
        //{
        //    Debug.Log(string.Format("Region at bottom left {0}, {1}, width {2}, height {3}", l.bottomLeft.x, l.bottomLeft.y, l.width, l.height));
        //}


    }


}
