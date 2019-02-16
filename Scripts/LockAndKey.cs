using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockAndKey 
{
    public CellS lockSpawn;
    public CellS keySpawn;

    public Cell lockInstance;
    public Cell keyInstance;
    
    public LockAndKey(CellS l, CellS k)
    {
        lockSpawn = l;
        keySpawn = k;
    }

    public LockAndKey(Cell l, Cell k)
    {
        lockInstance = l;
        keyInstance = k;
    }
}
