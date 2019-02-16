using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionNode
{
    private static Vector2Int MIN_REGION_SIZE = new Vector2Int(RoguelikeGenerator.instance.maxWidth, RoguelikeGenerator.instance.maxHeight);
    private RegionNode parent;
    public RegionNode[] children; // left child, right child

    // Location of this region. Represented by the bottom left corner of the region and its height/width. 
    public Vector2Int bottomLeft;
    public int width;
    public int height;

    // Split details.
    Vector2Int splitStart;
    bool splitHorizontal;

    // Room information 
    Vector2Int room;
    Vector2Int roomSize;
    List<Corridor> corridors;

    public RegionNode(Vector2Int bL, int w, int h)
    {
        bottomLeft = bL;
        width = w;
        height = h;
    }

    public bool Split()
    {
        // If we're already split, abort.
        if (children != null)
        {
            return false;
        }

        children = new RegionNode[2];

        // Choose direction of the split. 
        splitHorizontal = Random.Range(0.1f, 1.1f) > 0.5;
        if (width > height && width / height >= 1.25)
            splitHorizontal = false;
        else if (height > width && height / width >= 1.25)
            splitHorizontal = true;

        int maxSize = (splitHorizontal ? height - MIN_REGION_SIZE.y : width - MIN_REGION_SIZE.x);
        if ((!splitHorizontal && maxSize <= MIN_REGION_SIZE.x) || (splitHorizontal && maxSize <= MIN_REGION_SIZE.y))
                return false; // area too small to split anymore


        // Choose start position of the split, along the left or bottom sides depending on the direction of the split.
        // Create left and right children based on direction of the split.
        // Horizontal
        if (splitHorizontal)
        {

            splitStart = new Vector2Int(0, Random.Range(MIN_REGION_SIZE.y, maxSize));
            children[0] = new RegionNode(bottomLeft, width, splitStart.y);
            children[1] = new RegionNode(new Vector2Int(bottomLeft.x, bottomLeft.y + splitStart.y), width, height - splitStart.y);
        }
        // Vertical
        else
        {
            splitStart = new Vector2Int(Random.Range(MIN_REGION_SIZE.x, maxSize), 0);
            children[0] = new RegionNode(bottomLeft, splitStart.x, height);
            children[1] = new RegionNode(new Vector2Int(bottomLeft.x + splitStart.x, bottomLeft.y), width - splitStart.x, height);
            
        }

        // Test to see the regions

        //for (int x = children[0].bottomLeft.x; x < children[0].height; x++)
        //{
        //    for (int y = children[0].bottomLeft.y; y < children[0].width; y++)
        //    {
        //        Vector2 gridPos = new Vector2(x, y);
        //        RoguelikeGenerator.instance.cells[gridPos].type = CellS.TileType.Corridor;
                
        //    }
        //}
        

        return true;

    }

    // Generate all rooms and corridors for this leaf and all its children. 
    public void CreateRooms()
    {
        if (children[0] != null || children[1] != null)
        {
            // This leaf has been split, so go into the children leaves.
            children[0].CreateRooms();
            children[1].CreateRooms();

            CreateCorridors(children[0], children[1]);
        }
        else
        {
            // No children, so this leaf is ready to contain a room. 
            int roomWidth = Random.Range(RoguelikeGenerator.instance.minWidth, RoguelikeGenerator.instance.maxWidth);
            int roomHeight = Random.Range(RoguelikeGenerator.instance.minHeight, RoguelikeGenerator.instance.maxHeight);

            // Place the room at a random point in the leaf, but not right against the sides of it- would merge rooms together
            Vector2Int roomPos = new Vector2Int(Random.Range(1, width - roomWidth - 1), Random.Range(1, height - roomHeight - 1));
            room = new Vector2Int(bottomLeft.x + roomPos.x, bottomLeft.y + roomPos.y);
            roomSize = new Vector2Int(roomWidth, roomHeight);

            // Convert these cells on the grid to be rooms. 
            for (int x = room.x; x < room.x + roomWidth; x++)
            {
                for (int y = room.y; y < room.y + roomHeight; y++)
                {
                    Vector2 gridPos = new Vector2(x, y);
                    RoguelikeGenerator.instance.cells[gridPos].type = CellS.TileType.Room;

                }
            }
        }
    }

    // Iterate from any leaf into one of the rooms that are inside one of the children leaves. 
    public Vector2Int GetRoom()
    {
        if (room != null)
            return room;
        else
        {
            Vector2Int leftRoom;
            Vector2Int rightRoom;

            leftRoom = children[0].GetRoom();
            rightRoom = children[1].GetRoom();
            if (leftRoom == null && rightRoom == null)
                return Vector2Int.zero;
            else if (rightRoom == null)
                return leftRoom;
            else if (leftRoom == null)
                return rightRoom;
            else if (Random.Range(0.1f, 1.1f) > 0.5)
                return leftRoom;
            else
                return rightRoom;
        }
    }

    // Takes a pair of rooms, picks a random point in both, and creates a 'room' to connect the points together. 
    public void CreateCorridors(RegionNode l, RegionNode r)
    {
        corridors = new List<Corridor>();

        Vector2Int p1 = new Vector2Int(Random.Range(l.room.x + 1, l.room.x + l.roomSize.x - 2), Random.Range(l.room.y + 1, l.room.y + l.roomSize.y - 2));
        Vector2Int p2 = new Vector2Int(Random.Range(r.room.x + 1, r.room.x + r.roomSize.x - 2), Random.Range(r.room.y + 1, r.room.y + r.roomSize.y - 2));

        int w = p2.x - p1.x;
        int h = p2.y - p2.x;

        if (w < 0)
        {
            if (h < 0)
            {
                corridors.Add(new Corridor(p2.x, p2.y, Mathf.Abs(w), 1));
                corridors.Add(new Corridor(p1.x, p2.y, 1, Mathf.Abs(h)));
            }
            else if (h > 0)
            {
                corridors.Add(new Corridor(p2.x, p2.y, Mathf.Abs(w), 1));
                corridors.Add(new Corridor(p1.x, p1.y, 1, Mathf.Abs(h)));
            }
            else
            {
                corridors.Add(new Corridor(p2.x, p2.y, Mathf.Abs(w), 1));
            }
        }
        else if (w > 0)
        {
            if (h < 0)
            {
                corridors.Add(new Corridor(p1.x, p1.y, Mathf.Abs(w), 1));
                corridors.Add(new Corridor(p2.x, p2.y, 1, Mathf.Abs(h)));
            }
            else if (h > 0)
            {
                corridors.Add(new Corridor(p1.x, p2.y, Mathf.Abs(w), 1));
                corridors.Add(new Corridor(p1.x, p1.y, 1, Mathf.Abs(h)));
            }
            else
            {
                corridors.Add(new Corridor(p1.x, p1.y, Mathf.Abs(w), 1));
            }
        }
        else
        {
            if (h < 0)
            {
                corridors.Add(new Corridor(p2.x, p2.y, 1, Mathf.Abs(h)));
            }
            else if (h > 0)
            {
                corridors.Add(new Corridor(p1.x, p1.y, 1, Mathf.Abs(h)));
            }
        }

        foreach (Corridor c in corridors)
        {
            // Convert these cells on the grid to be corridors. 
            for (int x = c.x; x < c.x + width; x++)
            {
                for (int y = c.y; y < c.y + height; y++)
                {
                    Vector2 gridPos = new Vector2(x, y);
                    //Debug.Log(string.Format("Poition {0}, {1}", gridPos.x, gridPos.y));
                    if (RoguelikeGenerator.instance.cells.ContainsKey(gridPos))
                        RoguelikeGenerator.instance.cells[gridPos].type = CellS.TileType.Corridor;

                }
            }
        }
    }
}
