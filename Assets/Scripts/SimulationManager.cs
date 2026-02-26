using Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.UIElements;

public class SimulationManager: MonoBehaviour
{
	[Header("Creature Environment Settings")]
	public int creatureCount = 1000;
	public int foodCount = 1000;
	public float creatureSize = 1.5f;

	// These two are just for starting values until the simulation is running...
	public float minSpeed = 0.01f;
    public float maxSpeed = 1f;

	[Header("Simulation Settings")]
	public float cellSize = 0.1f;
	public int collisionDetectionBoxRadius = 1;
	public int sensingRadiusBoxRadius = 4;

	[Header("References")]
    public GameObject creaturePrefab;
	public Shapes.Rectangle rectangleTemplate;
	public Shapes.Disc foodTemplate;
	public Boolean useFixedSimulationTime = true;
	public float maxAge = 120f;
	public float maxHunger = 60f;
	public UnityEngine.Color creatureBodyColor = UnityEngine.Color.white;

    private Simulation sim;
	private float worldWidth = 0f;
	private float worldHeight = 0f;
	private float halfWidth = 0f;
	private float halfHeight = 0f;
	private List<GameObject> visualCreatures = new List<GameObject>();
	private List<Disc> visualFoods = new List<Disc>();	
	
	Shapes.Rectangle cellBox;
	Shapes.Rectangle collisionBox;
	Shapes.Rectangle sensoryBox;

    void Start()
    {
		Camera cam = Camera.main;
		worldHeight = cam.orthographicSize * 2;
		worldWidth = worldHeight * cam.aspect;

		halfWidth = worldWidth / 2f;
		halfHeight = worldHeight / 2f;

		cellBox = Instantiate( rectangleTemplate );
		cellBox.Color = UnityEngine.Color.white;
		collisionBox = Instantiate( rectangleTemplate );
		sensoryBox = Instantiate( rectangleTemplate );
		sensoryBox.Color = UnityEngine.Color.green;

        sim = new Simulation( worldWidth, worldHeight, cellSize, creatureCount, foodCount, creatureSize, maxAge, maxHunger, minSpeed, maxSpeed, collisionDetectionBoxRadius, sensingRadiusBoxRadius );

		// Create the simulation visual data.
        for( int i = 0; i < creatureCount; i++ ) {
            GameObject d = Instantiate( creaturePrefab );

			// Give each creature a random size for fun.
			float scale = UnityEngine.Random.Range( 0.6f, 1.4f );
			float inverseScale = 2f - scale;
            d.transform.localScale = Vector3.one * ( creatureSize * scale );
            visualCreatures.Add(d);
        }

        for( int i = 0; i < foodCount; i++ ) {
            Disc d = Instantiate( foodTemplate );
            d.transform.localScale = Vector3.one * ( creatureSize / 2f );
            visualFoods.Add(d);
        }
    }

    void Update()
    {
		float dt = Time.deltaTime;
        float simDt = useFixedSimulationTime ? 0.02f : dt;

        sim.Update( simDt );

        // Now read sim.Creatures and "draw" them. 
		for (int i = 0; i < creatureCount; i++)
        {
            Transform t = visualCreatures[i].transform;
			t.position = sim.Creatures[i].Position;

			// Adjust the visual orientation of the creature GameObject.
            Vector2 v = sim.Creatures[i].Velocity;
			float angle = Mathf.Atan2( v.y, v.x ) * Mathf.Rad2Deg;
			t.rotation = Quaternion.Euler(0f, 0f, angle);

			visualCreatures[i].SetActive (sim.Creatures[i].isAlive);

			var body = visualCreatures[i].transform.Find("Body").gameObject;
			// Scale the age so the creatures don't go full black.
			body.GetComponent<Shapes.Disc>().Color = UnityEngine.Color.Lerp( creatureBodyColor, UnityEngine.Color.black, ( sim.Creatures[i].age * 0.7f ) / maxAge );
		}

		// Now read sim.Food and "draw" them. 
		// They don't move linearly, but updating them like this should deal properly with food disappearing and appearing someplace new.
		for (int i = 0; i < foodCount; i++)
        {
            Transform t = visualFoods[i].transform;
			t.position = new Vector3( sim.Foods[i].Position.x, sim.Foods[i].Position.y, 1f );
		}

		// Position the cell and collision sdetection boxes for debugging.
		var cellBoxPositionSize = GetCellRegionRect( sim.Creatures[0].Position, cellSize, 0, sim.creatureGrid );
		cellBox.transform.position = new Vector3( cellBoxPositionSize.position.x, cellBoxPositionSize.position.y, -5f );
		cellBox.Width = cellBoxPositionSize.size.x;
		cellBox.Height = cellBoxPositionSize.size.y;

		/*Camera.main.transform.position =
			new Vector3( sim.Creatures[0].Position.x,
						 sim.Creatures[0].Position.y,
						 Camera.main.transform.position.z );*/

		
		var collisionBoxPositionSize = GetCellRegionRect( sim.Creatures[0].Position, cellSize, collisionDetectionBoxRadius, sim.creatureGrid );
		collisionBox.transform.position = new Vector3( collisionBoxPositionSize.position.x, collisionBoxPositionSize.position.y, -5f );
		collisionBox.Width = collisionBoxPositionSize.size.x;
		collisionBox.Height = collisionBoxPositionSize.size.y;

		var sensoryBoxPositionSize = GetCellRegionRect( sim.Creatures[0].Position, cellSize, sensingRadiusBoxRadius, sim.creatureGrid );
		sensoryBox.transform.position = new Vector3( sensoryBoxPositionSize.position.x, sensoryBoxPositionSize.position.y, -5f );
		sensoryBox.Width = sensoryBoxPositionSize.size.x;
		sensoryBox.Height = sensoryBoxPositionSize.size.y;
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
