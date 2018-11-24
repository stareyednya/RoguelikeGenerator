using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellS  {

	public enum TileType
	{
		Corridor, Wall, Room
	}

	public Vector2 gridPos;
	public Vector2 spawnPos;
	public bool wallD;
	public bool wallU;
	public bool wallL;
	public bool wallR;
	public bool visited;
	public TileType type;

	public GameObject cellObject;
	public CellScript cScript;
}
