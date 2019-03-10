using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room 
{
    public int roomNo;
    public List<CellS> roomCells;

    public List<Cell> roomCellsInstantiated;

    public bool hasKey;
    public bool hasLock;
    public int doorwayCount;

    public Vector2Int left;

    public int width;
    public int height;


    public Room()
    {
        roomCells = new List<CellS>();
        roomCellsInstantiated = new List<Cell>();
        hasKey = false;
        hasLock = false;
        doorwayCount = 0;
    }

   

}
