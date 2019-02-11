using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockAndKey 
{
    public CellS lockSpawn;

    public CellS keySpawn;
    
    public LockAndKey(CellS l, CellS k)
    {
        lockSpawn = l;
        keySpawn = k;
    }
}
