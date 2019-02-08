using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellS  {

	public enum TileType
	{
		Corridor, Wall, Room, Spawn, Spacer
	}

	public Vector2 gridPos;
	public Vector2 spawnPos;
	public bool wallD;
	public bool wallU;
	public bool wallL;
	public bool wallR;
	public bool visited;
	public TileType type;

	// Information about which room this cell is part of, if any. Defaults to 0.
	public int roomNo = 0;
	public bool isAdjacentC = false;

	// Pathfinding information for this cell 
	public int g; // Steps from start point to this cell 
	public int h; // Steps from this cell to the destination 
	public CellS parent; // Parent cell in this path 

	public GameObject cellObject;
	public CellScript cScript;

	// Getter function for calculating f on the fly 
	public int f 
	{
		get
		{
			return g + h;	
		}
	}

    public CellS()
    {
        wallD = true;
        wallL = true;
        wallR = true;
        wallU = true;
        visited = false;
        type = TileType.Corridor;
    }

    public CellS(CellS copy)
    {
        wallD = copy.wallD;
        wallL = copy.wallL;
        wallR = copy.wallR;
        wallU = copy.wallU;
        visited = copy.visited;
        type = copy.type;
    }
}
