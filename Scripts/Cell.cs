using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell {

	public Vector2 gridPos;
	public GameObject cellObject;
	public CellScript cScript;
    public bool isAdjacentC;
    public int roomNo;

    // Pathfinding information for this cell 
    public int g; // Steps from start point to this cell 
    public int h; // Steps from this cell to the destination 
    public Cell parent; // Parent cell in this path 

    // Getter function for calculating f on the fly 
    public int f
    {
        get
        {
            return g + h;
        }
    }
}
