using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoguelikeGenerator : MonoBehaviour {

	// The prefab to use as the cell icon. 
	[SerializeField]
	protected GameObject cellPrefab;
	// The tile to use as a wall to fill in dead ends with.
	[SerializeField]
	protected GameObject wallPrefab;
	// A tile to differentiate room tiles from the floor.
	[SerializeField]
	protected GameObject roomPrefab;

	// Options for the full level generation to run. 
	public bool mazeWithRooms; // default MWR with all the instantation 
	public bool MWRPooling; // MWR using pooling for swapping floor and wall tiles
	public bool MWRStruct; // MWR instantiating all tiles at the end 

	private RoguelikeGenerator rg;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
