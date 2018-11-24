using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntAndKill : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	/*
	public void HuntAndKillAlgorithm()
	{
		bool unvisitedCells = true;
		bool hadNeighbours = true;
		// Perform walk until current cell has no visited neighbours. 
		while (unvisitedCells)
		{
			while (hadNeighbours)
			{
				hadNeighbours = Walk ();
			}

			// Find a new current cell by hunting, or determine that all cells have been visited. 
			unvisitedCells = Hunt();
		}
	}

	public bool Walk()
	{
		bool hadNeighbours = false;
		// Find the current cell's unvisited neighbours. 
		List<Cell> unvisitedNeighbours = new List<Cell> ();
		unvisitedNeighbours = GetUnvisitedNeighbours (currentCell);
		// Check that our current cell has neighbours we can carve a passage to. 
		if (unvisitedNeighbours.Count != 0)
		{
			// Choose a random direction to go in by picking a random unvisited neighbour
			checkCell = unvisitedNeighbours [Random.Range (0, unvisitedNeighbours.Count)];
			// Compare and remove walls if needed
			CompareWalls(currentCell, checkCell);
			// Make our new current cell the neighbour we were checking. 
			currentCell = checkCell;
			// Mark our new current cell as visited. 
			unvisited.Remove(currentCell);
			hadNeighbours = true;
		}

		return hadNeighbours;
	}

	// Sets a new current cell to start from, or false if all cells have been visited. 
	public bool Hunt()
	{
		if (unvisited.Count == 0)
			return false;
		// Scan the grid for an unvisited cell that is adjacent to a visited cell.
		Cell nCell = null;
		Cell testCell = null;
		List<Cell> visitedNeighbours = new List<Cell> ();

		// Scan through our remaining unvisited cells to try find one with a visited neighbour.
		for (int i = 0; i < unvisited.Count; i++)
		{
			testCell = unvisited[i];
			// The cell wil only be a candidate if it itself has not been visited.
			if (unvisited.Contains(testCell))
			{

				// Find this cell's visible neighbours.
				foreach(Vector2 p in possibleNeighbours)
				{
					// Find the position of a neighbour on the grid, relative to the current cell. 
					Vector2 nPos = testCell.gridPos + p;
					// Check the neighbouring cell exists. 
					if (cells.ContainsKey (nPos))
					{
						nCell = cells [nPos];
						if (!unvisited.Contains(nCell))
						{
							visitedNeighbours.Add(cells [nPos]);
							// Break from the loop conditions so we don't loop through the rest of the grid when we dont need to.
							i = unvisited.Count;
						}
					}
				}
			}
		}

		// Randomly choose one of the neighbour cells to carve a passage to. 
		nCell = visitedNeighbours[Random.Range(0, visitedNeighbours.Count)];
		// Create passage between the cell we found and its neighbour. 
		CompareWalls(testCell, nCell);
		// Mark the formerly unvisited cell as visited. 
		unvisited.Remove(testCell);
		// Make this newly found cell our current cell to start from.
		currentCell = testCell;
		return true;
	}
	*/
}
