using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonTree
{
    private RegionNode root;
    public List<RegionNode> leaves;

    public DungeonTree(RegionNode r)
    {
        root = r;
        leaves = new List<RegionNode>();
        leaves.Add(root);
    }

    public void AddLeaf(RegionNode leaf)
    {
        leaves.Add(leaf);
    }

    public void SplitLeaves()
    {
        bool didSplit = true;
        // Temporary list of leaves to add to prevent modifying the collection while iterating. 
        List<RegionNode> toAdd = new List<RegionNode>();
        // Loop through every leaf in the tree until no more can be split. 
        while (didSplit)
        {
            didSplit = false;
            foreach (RegionNode r in leaves)
            {
                // If this leaf is not already split
                if (r.children == null)
                {
                    if (r.Split())
                    {
                        // If we did split this leaf, add the child leaves to the tree so we can loop into them
                        toAdd.Add(r.children[0]);
                        toAdd.Add(r.children[1]);
                        didSplit = true;
                    }
                }
            }
            foreach (RegionNode r in toAdd)
                AddLeaf(r);
        }


        // Go through each leaf and create a room in each. 
        root.CreateRooms();
    }
}
