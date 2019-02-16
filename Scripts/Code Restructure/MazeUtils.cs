using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeUtils
{

    // Array of all possible neighbour positions
    // Left, right, up, down
    public static Vector2[] possibleNeighbours = new Vector2[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };

    // Create a cell based on the given position. 
    public static CellS GenerateCell(Vector2 pos, Vector2 keyPos, CellS.TileType type = CellS.TileType.Corridor)
    {
        CellS newCell = new CellS(type);
        // Store a reference to this position in the grid.
        newCell.gridPos = keyPos;
        newCell.spawnPos = pos;
        return newCell;
    }

    public static List<CellS> GetUnvisitedNeighbours(CellS cCell, ref Dictionary<Vector2, CellS> mazeCells)
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
            if (mazeCells.ContainsKey(nPos))
                nCell = mazeCells[nPos];

            // Check if the neighbouring cell is unvisited and thus a valid neighbour. 
            if (!nCell.visited)
                neighbours.Add(nCell);
        }

        // Return the completed list of unvisited neighbours.
        return neighbours;
    }

    public static List<Cell> GetUnvisitedNeighbours(Cell cCell, ref Dictionary<Vector2, Cell> cells, ref List<Cell> unvisited)
    {
        List<Cell> neighbours = new List<Cell>();
        Cell nCell = cCell;
        // Store the position of our current cell. 
        Vector2 currentPos = cCell.gridPos;

        foreach (Vector2 p in possibleNeighbours)
        {
            // Find the position of a neighbour on the grid, relative to the current cell. 
            Vector2 nPos = currentPos + p;
            // Check the neighbouring cell exists. 
            if (cells.ContainsKey(nPos))
                nCell = cells[nPos];

            // Check if the neighbouring cell is unvisited and thus a valid neighbour. 
            if (unvisited.Contains(nCell))
                neighbours.Add(nCell);
        }

        // Return the completed list of unvisited neighbours.
        return neighbours;
    }

    // Compare current cell with its neighbour and remove walls as appropriate. 
    public static void CompareWalls(CellS currentCell, CellS neighbourCell)
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

    // Compare current cell with its neighbour and remove walls as appropriate. 
    public static void CompareWallsInPool(Cell currentCell, Cell neighbourCell)
    {
        // If neighbour is to the left of current. 
        if (neighbourCell.gridPos.x < currentCell.gridPos.x)
        {
            RemoveWall(neighbourCell.cScript, 2);
            RemoveWall(currentCell.cScript, 1);
        }
        // If neighbour is to the right of current. 
        else if (neighbourCell.gridPos.x > currentCell.gridPos.x)
        {
            RemoveWall(neighbourCell.cScript, 1);
            RemoveWall(currentCell.cScript, 2);
        }
        // If neighbour is above current. 
        else if (neighbourCell.gridPos.y > currentCell.gridPos.y)
        {
            RemoveWall(neighbourCell.cScript, 4);
            RemoveWall(currentCell.cScript, 3);
        }
        // If neighbour is below current. 
        else if (neighbourCell.gridPos.y < currentCell.gridPos.y)
        {

            RemoveWall(neighbourCell.cScript, 3);
            RemoveWall(currentCell.cScript, 4);
        }
    }

    // Disables the cell wall chosen by the wallID.
    private static void RemoveWall(CellScript cScript, int wallID)
    {
        switch (wallID)
        {
            case 1:
                // Disable the left wall.
                cScript.wallL.SetActive(false);
                break;
            case 2:
                // Disable the right wall.
                cScript.wallR.SetActive(false);
                break;
            case 3:
                // Disable the above wall.
                cScript.wallU.SetActive(false);
                break;
            case 4:
                // Disable the below wall.
                cScript.wallD.SetActive(false);
                break;
        }
    }

    // Count how many walls this cell currently has active. 
    public static int NoOfWalls(CellS c)
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

    public static int NoOfWalls(Cell c)
    {
        int count = 0;
        if (c.cScript.wallD.activeInHierarchy)
            count++;

        if (c.cScript.wallL.activeInHierarchy)
            count++;
        if (c.cScript.wallR.activeInHierarchy)
            count++;
        if (c.cScript.wallU.activeInHierarchy)
            count++;

        return count;
    }


}
