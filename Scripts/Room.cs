using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room 
{
    public int roomNo;
    public List<CellS> roomCells;

    public bool hasKey;

    public Room()
    {
        roomCells = new List<CellS>();
        hasKey = false;
    }
}
