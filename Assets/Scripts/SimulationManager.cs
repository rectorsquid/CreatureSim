using Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class SimulationManager: MonoBehaviour
{
	[Header("World Settings")]
	public float worldSize = 30f;
	public float cameraSize = 5f;
	public int creatureCount = 4000;
	public int maxCreatureCount = 5000;
	public int foodCount = 1000;

	[Header("Creature Settings")]
	public float creatureSize = 1.5f;
	public float secondsPerSpawn = 30f;
	public float maxAge = 120f;
	public float maxHunger = 60f;
	public float minSpeed = 0.01f;
    public float maxSpeed = 1f;
	public UnityEngine.Color creatureBodyColor = UnityEngine.Color.white;
	public UnityEngine.Color childBodyColor = UnityEngine.Color.red;

	[Header("Simulation Settings")]
	public float cellSize = 0.1f;
	public int collisionDetectionBoxRadius = 1;
	public int sensingRadiusBoxRadius = 4;

	[Header("References")]
    public GameObject creaturePrefab;
	public Shapes.Rectangle rectangleTemplate;
	public Shapes.Line lineTemplate;
	public Shapes.Disc foodTemplate;
	public Boolean useFixedSimulationTime = true;
	public bool showZeroCreatureBoxes = false;
	public FPSDisplay debugOutputDisplay;

    private Simulation sim;
	private float worldWidth = 0f;
	private float worldHeight = 0f;
	private float halfWidth = 0f;
	private float halfHeight = 0f;
	private List<GameObject> visualCreatures = new List<GameObject>();
	private List<Disc> visualFoods = new List<Disc>();	
	private float foodSize = 1f;

	private int debugItemIndex = 0;
	
	Shapes.Rectangle cellBox;
	Shapes.Rectangle collisionBox;
	Shapes.Rectangle sensoryBox;
	Shapes.Line nearestFoodLine;

    void Start()
    {
		Camera cam = Camera.main;
		cam.orthographicSize = cameraSize;
		worldWidth = worldSize;
		worldHeight = worldSize;

		halfWidth = worldWidth / 2f;
		halfHeight = worldHeight / 2f;

		cellBox = Instantiate( rectangleTemplate );
		cellBox.Color = UnityEngine.Color.white;
		collisionBox = Instantiate( rectangleTemplate );
		sensoryBox = Instantiate( rectangleTemplate );
		sensoryBox.Color = UnityEngine.Color.green;
		nearestFoodLine = Instantiate( lineTemplate );
		nearestFoodLine.Color = UnityEngine.Color.yellow;

		foodSize = creatureSize * 0.75f;

        sim = new Simulation( worldWidth, worldHeight, cellSize, creatureCount, maxCreatureCount, foodCount, creatureSize, 
			                  maxAge, maxHunger, minSpeed, maxSpeed, collisionDetectionBoxRadius, sensingRadiusBoxRadius, secondsPerSpawn );

		// Create the simulation visual data.
        for( int i = 0; i < maxCreatureCount; i++ ) {
            GameObject d = Instantiate( creaturePrefab );

			// Give each creature a random size for fun.
			float scale = UnityEngine.Random.Range( 0.6f, 1.4f );
			float inverseScale = 2f - scale;
            d.transform.localScale = Vector3.one * ( creatureSize * scale );
            visualCreatures.Add(d);
        }

        for( int i = 0; i < foodCount; i++ ) {
            Disc d = Instantiate( foodTemplate );
            d.transform.localScale = Vector3.one * foodSize;
            visualFoods.Add(d);
        }
    }

	private void findNextDebugCreature() {
		if( debugItemIndex < 0 ) { return; }
		if( sim.creatureCount == 0 ) { 
			debugItemIndex = -1;
			return; 
		}

		int index = debugItemIndex + 1;
		while( index != debugItemIndex ) {
			if( index >= sim.creatureCount ) {
				index = 0;
			}
			if( sim.Creatures[index].isAlive ) {
				break;
			}
			++index;
		}
		debugItemIndex = index;
	}

	void HandleKeys()
    {
		if (Keyboard.current.tabKey.wasPressedThisFrame)
		{
			findNextDebugCreature();
		}
	}

	private void sendDebugText() {
		String data = "";

		data += $"{sim.creatureCount:F0} Creatures\n";
		data += $"Debug Box On: {debugItemIndex:F0}\n";





		debugOutputDisplay.extraOutput = data;
	}

    void Update()
    {
		float dt = Time.deltaTime;
        float simDt = useFixedSimulationTime ? 0.0333333f : dt;

		HandleKeys();

        sim.Update( simDt );

		if( debugItemIndex >= 0 && debugItemIndex < sim.creatureCount && !sim.Creatures[debugItemIndex].isAlive ) {
			findNextDebugCreature();
		}

		sendDebugText();

        // Now read sim.Creatures and "draw" them. 
		for (int i = 0; i < maxCreatureCount; i++)
        {
			if( i >= sim.creatureCount ) {
				visualCreatures[i].SetActive( false );
				continue;
			}

            Transform t = visualCreatures[i].transform;
			t.position = sim.Creatures[i].Position;

			// Adjust the visual orientation of the creature GameObject.
            Vector2 v = sim.Creatures[i].Velocity;
			float angle = Mathf.Atan2( v.y, v.x ) * Mathf.Rad2Deg;
			t.rotation = Quaternion.Euler(0f, 0f, angle);

			visualCreatures[i].SetActive( sim.Creatures[i].isAlive );

			var body = visualCreatures[i].transform.Find("Body").gameObject;
			// Scale the age so the creatures don't go full black.
			body.GetComponent<Shapes.Disc>().Color = UnityEngine.Color.Lerp( sim.Creatures[i].isChild ? childBodyColor: creatureBodyColor, UnityEngine.Color.black, ( sim.Creatures[i].age * 0.7f ) / maxAge );
		}

		// Now read sim.Food and "draw" them. 
		// They don't move linearly, but updating them like this should deal properly with food disappearing and appearing someplace new.
		for (int i = 0; i < foodCount; i++)
        {
            Transform t = visualFoods[i].transform;
			t.position = new Vector3( sim.Foods[i].Position.x, sim.Foods[i].Position.y, 1f );
		}

		showDebug();
    }

	private void showDebug() {
		if( !showZeroCreatureBoxes || debugItemIndex < 0 ) {
			cellBox.enabled = false;
			collisionBox.enabled = false;
			sensoryBox.enabled = false;
			nearestFoodLine.enabled = false;
			return;
		}

		// Position the cell and collision sdetection boxes for debugging.
		var cellBoxPositionSize = GetCellRegionRect( sim.Creatures[debugItemIndex].Position, cellSize, 0, sim.creatureGrid );
		cellBox.transform.position = new Vector3( cellBoxPositionSize.position.x, cellBoxPositionSize.position.y, -5f );
		cellBox.Width = cellBoxPositionSize.size.x;
		cellBox.Height = cellBoxPositionSize.size.y;

		cellBox.enabled = true;
		
		var collisionBoxPositionSize = GetCellRegionRect( sim.Creatures[debugItemIndex].Position, cellSize, collisionDetectionBoxRadius, sim.creatureGrid );
		collisionBox.transform.position = new Vector3( collisionBoxPositionSize.position.x, collisionBoxPositionSize.position.y, -5f );
		collisionBox.Width = collisionBoxPositionSize.size.x;
		collisionBox.Height = collisionBoxPositionSize.size.y;

		collisionBox.enabled = true;

		var sensoryBoxPositionSize = GetCellRegionRect( sim.Creatures[debugItemIndex].Position, cellSize, sensingRadiusBoxRadius, sim.creatureGrid );
		sensoryBox.transform.position = new Vector3( sensoryBoxPositionSize.position.x, sensoryBoxPositionSize.position.y, -5f );
		sensoryBox.Width = sensoryBoxPositionSize.size.x;
		sensoryBox.Height = sensoryBoxPositionSize.size.y;

		sensoryBox.enabled = true;

		nearestFoodLine.Start = new Vector3( sim.Creatures[debugItemIndex].Position.x, sim.Creatures[debugItemIndex].Position.y, -4.5f );
		nearestFoodLine.End = new Vector3( sim.Creatures[debugItemIndex].nearestFood.x, sim.Creatures[debugItemIndex].nearestFood.y, -4.5f );
		nearestFoodLine.enabled = sim.Creatures[debugItemIndex].isFoodNearby;

		for (int i = 0; i < foodCount; i++) {
            Transform t = visualFoods[i].transform;
			t.localScale = Vector3.one * foodSize;
		}

		for( int index = 0; index < sim.Creatures[debugItemIndex].nearbyFood.Count; ++index ) {
            Transform t = visualFoods[sim.Creatures[debugItemIndex].nearbyFood[index]].transform;
			t.localScale = Vector3.one * ( foodSize * 5f );
		}

	}

	public (Vector2 position, Vector2 size) GetCellRegionRect( Vector2 worldPos, float cellSize, int radius, List<int>[,] grid ) {
		/*
		 * IMPORTANT!
		 * A rectangle could appear off the edge of the world because the grid allocation and grid location code
		 * has a grid row and a grid column that start in the world coordinates but then end outside of the world
		 * coordinates. This is done to ensure that ever coordinate int he world is in a grid square even if the 
		 * world width and/or height are not evenly divisible buy the cell size. So don't worry if the cisualization
		 * looks wrong and off the edge of the world - that's expected.
		 */

		int gridWidth  = grid.GetLength(0);
		int gridHeight = grid.GetLength(1);

		int gx = (int)((worldPos.x+halfWidth) / cellSize);
        int gy = (int)((worldPos.y+halfHeight) / cellSize);

		gx = Mathf.Clamp( gx, 0, gridWidth - 1 ); 
		gy = Mathf.Clamp( gy, 0, gridHeight - 1 );

		int minX = Mathf.Max( 0, gx - radius );
		int maxX = Mathf.Min( gridWidth - 1, gx + radius );

		int minY = Mathf.Max( 0, gy - radius );
		int maxY = Mathf.Min( gridHeight - 1, gy + radius );

		float worldMinX = minX * cellSize;
		float worldMaxX = (maxX + 1) * cellSize;

		float worldMinY = minY * cellSize;
		float worldMaxY = (maxY + 1) * cellSize;

		Vector2 pos = new Vector2( ( (worldMinX + worldMaxX) * 0.5f ) - halfWidth, ( (worldMinY + worldMaxY) * 0.5f ) - halfHeight );
		Vector2 size = new Vector2( worldMaxX - worldMinX, worldMaxY - worldMinY );

		return (pos, size);
	}
}
