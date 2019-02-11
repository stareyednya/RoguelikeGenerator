using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding 
{
    // Array of all possible neighbour positions
    // Left, right, up, down
    public static Vector2[] possibleNeighbours = new Vector2[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };

    public static CellS FindLowestFScore(List<CellS> openList)
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

    public static bool AreConnected(CellS currentCell, CellS neighbourCell)
    {
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

    public static List<CellS> GetNeighbours(CellS cCell, int currentLock)
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
            if (RoguelikeGenerator.instance.cells.ContainsKey(nPos))
                nCell = RoguelikeGenerator.instance.cells[nPos];

            // Check if the neighbouring cell is traversable and therefore a valid neighbour.
            // Also check if the wall in that direction is active to prevent the case of jumping to a blocked off corridor. 
            // (might be fixable when the dungeon is spaced out, but for now)
            if (nCell.type != CellS.TileType.Wall && nCell.gridPos != RoguelikeGenerator.instance.lockKeySpawns[currentLock].lockSpawn.gridPos)
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

    public static float ManhattanDistance(CellS c, CellS t)
    {
        // Manhattan distance is the sum of the absolute values of the horizontal and the vertical distance
        float h = Mathf.Abs(c.gridPos.x - t.gridPos.x) + Mathf.Abs(c.gridPos.y - t.gridPos.y);
        return h;
    }

    
}
