
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


public class Food
{
    public float x, y;

    public int gridX;
    public int gridY;

    public Vector2 Position
    {
        get => new Vector2(x, y);
        set { x = value.x; y = value.y; }
    }
}

public class Simulation
{
    private readonly float width;
    private readonly float height;
	private readonly float halfWidth;
    private readonly float halfHeight;
    private readonly float cellSize;
    private readonly float creatureRadius;
	private readonly int collisionRadius;
	private readonly int sensesRadius;
	private readonly float maxAge;
	private readonly float maxHunger;
	private readonly float minSpeed;
	private readonly float maxSpeed;
	private readonly float secondsPerSpawn;
	private readonly float secondsPerFoodSpawn;

    private readonly Creature[] creatures;
    private readonly Food[] foods;

    public readonly List<int>[,] creatureGrid;
    public readonly List<int>[,] foodGrid;
	public int creatureCount;
	public int maxCreatureCount;
	public int foodCount;
	public int maxFoodCount;

	private Vector2 dummyVector;

    public Simulation( float width, float height, float cellSize, int creatureCount, int foodCount, float creatureRadius, 
		               float maxAge, float maxHunger, float minSpeed, float maxSpeed, int collisionRadius, int sensesRadius, 
					   float secondsPerSpawn, float secondsPerFoodSpawn )
    {
        this.width = width;
        this.height = height;
		this.halfWidth = width / 2f;
        this.halfHeight = height / 2f;
        this.cellSize = cellSize;
		this.creatureCount = creatureCount;
		maxCreatureCount = ( creatureCount * 3 ) / 2;
		this.creatureRadius = creatureRadius;
		this.foodCount = foodCount;
		maxFoodCount = ( foodCount * 3 ) / 2;
		this.maxAge = maxAge;
		this.maxHunger = maxHunger;
		this.minSpeed = minSpeed;
		this.maxSpeed = maxSpeed;
		this.collisionRadius = collisionRadius;
		this.sensesRadius = sensesRadius;
		this.secondsPerSpawn = secondsPerSpawn;
		this.secondsPerFoodSpawn = secondsPerFoodSpawn;
		
        int gridX = Mathf.CeilToInt(width / cellSize);
        int gridY = Mathf.CeilToInt(height / cellSize);

        creatureGrid = new List<int>[gridX, gridY];
        for (int x = 0; x < gridX; x++) {
            for (int y = 0; y < gridY; y++) {
                creatureGrid[x, y] = new List<int>(4);
			}
		}

        foodGrid = new List<int>[gridX, gridY];
        for (int x = 0; x < gridX; x++) {
            for (int y = 0; y < gridY; y++) {
                foodGrid[x, y] = new List<int>(4);
			}
		}

        creatures = new Creature[maxCreatureCount];
        for ( int i = 0; i < maxCreatureCount; i++ )
        {
            creatures[i] = new Creature();
			if( i < creatureCount ) {
				// Only initialize the living creatures. The rest are placeholders for children.
				initializeCreature( i, null );
				AddCreatureToGrid( i );
			}
        }

        foods = new Food[maxFoodCount];
		for ( int i = 0; i < maxFoodCount; i++ )
        {
            foods[i] = new Food();
			if( i < foodCount ) {
				initializeFood( i );
				AddFoodToGrid( i );
			}
        }
	}

    public void Update( float dt )
    {
		dt = Mathf.Min(Time.deltaTime, 0.1f);

		// DO NOT MULTITHREAD SOME LOOPS BECAUSE OF GRID AND OTHER ARRAY UPDATES.

        for( int i = 0; i < creatureCount; i++ )
        {
			if( creatures[i].isAlive ) {
				MoveCreature( i, dt );
				UpdateCreatureGridMembership( i );
			}
        }

        for( int i = 0; i < creatureCount; i++ )
        {
			if( !creatures[i].isAlive ) { continue; }
			checkCollisions( i );
		}

		for( int i = 0; i < creatureCount; i++ ) {
			if( creatures[i].isAlive ) {
				checkFood( i );
			}
		}

		// Only self contained code that does not modify the overall simulation data, such as
		// the grids or creature and food arrays, can be parallel. This code is running the neural network
		// but does not modify anything outside of the individual creatures.
		Parallel.For( 0, creatureCount, i => {
			if( creatures[i].isAlive ) {
				checkSteering( i );
			}
		} );

		// Save the count so the loop can add creatures without processing them in the same loop.
		int count = creatureCount;
		for( int i = 0; i < count; i++ )
        {
			if( !creatures[i].isAlive ) { continue; }
			processCreatureLifecycle( i, dt );
		}

		if( RandomEvent( dt, secondsPerFoodSpawn ) ) {
			spawnNewFood();
		}

    }

	private void killCreature( int creatureIndex ) {
		int lastCreatureIndex = creatureCount-1;
		// Remove the current creature from the grid.
		removeCreatureFromGrid( creatureIndex );
		// remove the last creature fonr the grid.
		removeCreatureFromGrid( lastCreatureIndex );

		// Swap with the last alive creature in the array and shrink the alive creature count.
		var currentCreature = creatures[creatureIndex];
		currentCreature.isAlive = false;
		if( creatureCount > 1 ) {
			var lastCreature = creatures[lastCreatureIndex];
			creatures[creatureIndex] = lastCreature;
			creatures[lastCreatureIndex] = currentCreature;
		}
		--creatureCount;
		// Add the creature at the current index back inbto the grid.
		AddCreatureToGrid( creatureIndex );
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void removeFoodFromGrid( int foodIndex ) {
		foodGrid[foods[foodIndex].gridX, foods[foodIndex].gridY].Remove( foodIndex );
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void removeCreatureFromGrid( int creatureIndex ) {
		creatureGrid[creatures[creatureIndex].gridX, creatures[creatureIndex].gridY].Remove( creatureIndex );
	}

	private void killFood( int foodIndex ) {
		int lastFoodIndex = foodCount-1;
		// Remove the killed food from the grid.
		removeFoodFromGrid( foodIndex );
		// Remove the food in the last food index from the grid.
		removeFoodFromGrid( lastFoodIndex );
		// Swap the food objects and lower the food count to "kill" the food now in the last position.
		var currentFood = foods[foodIndex];
		if( foodCount > 1 ) {
			var lastFood = foods[lastFoodIndex];
			foods[foodIndex] = lastFood;
			foods[lastFoodIndex] = currentFood;
		}
		--foodCount;
		// Add the food at the current index back into the grid.
		AddFoodToGrid( foodIndex );
	}
	private void spawnNewCreature( Vector2 position, Creature parent ) {
		if( creatureCount >= maxCreatureCount ) {
			return;
		}
		int index = creatureCount++;

		initializeCreature( index, parent );

		// Override the defau;t random location.
		Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
		Vector2 childPosition = position + dir * creatureRadius;
		childPosition.x = Mathf.Clamp( childPosition.x, 0f, width - 1f );
		childPosition.y = Mathf.Clamp( childPosition.y, 0f, height - 1f );
		creatures[index].Position = position + dir * creatureRadius;
		creatures[index].isAlive = true;
		creatures[index].isChild = true;

        AddCreatureToGrid( index );
	}

	private void spawnNewFood() {
		if( foodCount >= maxFoodCount ) {
			return;
		}
		int index = foodCount++;

		initializeFood( index );
		AddFoodToGrid( index );
	}


	private void checkFood( int i ) {
        var creature = creatures[i];
		
		int gx = creature.gridX;
        int gy = creature.gridY;

		// Look nearby for food to eat.
		foodGrid.GetNeighbors( gx, gy, collisionRadius, creature.eatableFood );

		for( int index = 0; index < creature.eatableFood.Count; ++index ) {
			var foodIndex = creature.eatableFood[index];
			var food = foods[foodIndex];

			if( eatFood( ref creature, ref food, foodIndex, creatureRadius ) ) {
				return;
			}
		}
	}

	private void checkSteering( int i ) {
        var creature = creatures[i];
		
		int gx = creature.gridX;
        int gy = creature.gridY;

		// Look far for nearby food.
		foodGrid.GetNeighbors( gx, gy, sensesRadius, creature.nearbyFood );

		int bestFood = -1;
		float bestFoodDistanceSquared = 0f;

		for( int index = 0; index < creature.nearbyFood.Count; ++index ) {
			var c2 = creature.nearbyFood[index];
			var food = foods[c2];

			// Deal with nearby food if nothing has been eaten.
			Vector2 d = food.Position - creature.Position;
			float distSq = d.sqrMagnitude;

			if( bestFood == -1 || distSq < bestFoodDistanceSquared ) {
				bestFoodDistanceSquared = distSq;
				bestFood = c2;
			}
		}

		if( bestFood >= 0 ) {
			creature.nearestFood = foods[bestFood].Position;
			creature.isFoodNearby = true;
			processCreatureInput( ref creature, true, foods[bestFood].Position );
		} else {
			creature.isFoodNearby = false;
			processCreatureInput( ref creature, false, dummyVector );
		}
	}

	private void processCreatureInput( ref Creature creature, bool seesFood, Vector2 foodPosition ) {
		// Convert food angle to a realtive angle.
		Vector2 relativePosition = foodPosition - creature.Position;
		float angleToFood = Mathf.Atan2( relativePosition.y, relativePosition.x );
		float myDirectionAngle = Mathf.Atan2( creature.vy, creature.vx );
		float relativeFoodAngle = angleToFood - myDirectionAngle;

		if (relativeFoodAngle > Mathf.PI) relativeFoodAngle -= 2f * Mathf.PI;
		if (relativeFoodAngle < -Mathf.PI) relativeFoodAngle += 2f * Mathf.PI;

		float distanceToFood = relativePosition.magnitude;

		creature.runNetwork( seesFood, relativeFoodAngle, distanceToFood );
	}

	private void checkCollisions( int i ) {
        var creature = creatures[i];
		
		int gx = creature.gridX;
        int gy = creature.gridY;

		// Look close for collisions
		creatureGrid.GetNeighbors( gx, gy, collisionRadius, creature.neighbors );
		for( int index = 0; index < creature.neighbors.Count; ++index ) {
			var c2 = creature.neighbors[index];
			if (c2 <= i) continue;

			var other = creatures[c2];
			if( !other.isAlive ) { continue; }

			ResolvePair( ref creature, ref other, creatureRadius * 1.2f, restitution: 0.15f, friction: 0.2f, slop: 0.0005f );
		}
	}

	bool eatFood( ref Creature a, ref Food b, int foodIndex, float radius ) {
		Vector2 d = b.Position - a.Position;
		float distSq = d.sqrMagnitude;
		float r = radius + radius;

		if( distSq >= r * r ) { return false; }
		float dist = Mathf.Sqrt( distSq );
		if( dist > radius )	{ return false; }

		a.hunger = Mathf.Max( 0f, a.hunger - maxHunger * 0.25f );

		killFood( foodIndex );

		return true;
	}


	static void ResolvePair(
		ref Creature a,
		ref Creature b,
		float radius,
		float restitution,
		float friction,
		float slop )
	{
		Vector2 d = b.Position - a.Position;
		float distSq = d.sqrMagnitude;
		float r = radius + radius;

		if( distSq >= r * r )
			return;

		float dist = Mathf.Sqrt( distSq );

		Vector2 n;
		if( dist > 1e-6f )
			n = d / dist;
		else
			n = UnityEngine.Random.insideUnitCircle.normalized;

		float penetration = r - dist;

		// 1) Position correction (equal mass)
		float pen = Mathf.Max( 0f, penetration - slop );
		Vector2 corr = n * ( pen * 0.5f );
		a.Position -= corr;
		b.Position += corr;

		// 2) Bounce impulse (equal mass)
		Vector2 relVel = b.Velocity - a.Velocity;
		float vn = Vector2.Dot( relVel, n );

		if( vn < 0f )
		{
			float invMassSum = 2f; // 1 + 1
			float j = -( 1f + restitution ) * vn / invMassSum;

			Vector2 impulse = n * j;
			a.Velocity -= impulse;
			b.Velocity += impulse;

			// 3) Friction
			Vector2 rv2 = b.Velocity - a.Velocity;
			Vector2 t = rv2 - n * Vector2.Dot( rv2, n );
			float tMag = t.magnitude;

			if( tMag > 1e-6f )
			{
				t /= tMag;
				float vt = Vector2.Dot( rv2, t );
				float jt = -vt / invMassSum;

				float maxFriction = friction * j;
				jt = Mathf.Clamp( jt, -maxFriction, maxFriction );

				Vector2 fImpulse = t * jt;
				a.Velocity -= fImpulse;
				b.Velocity += fImpulse;
			}
		}
	}

	private bool RandomEvent( float deltaTime, float secondsPerEvent )
	{
		// Rate λ = events per second
		float lambda = 1f / secondsPerEvent;

		// Probability of at least one event in this frame
		float p = 1f - Mathf.Exp(-lambda * deltaTime);

		return UnityEngine.Random.value < p;
	}


	private void processCreatureLifecycle( int i, float dt )
    {
        var c = creatures[i];
		c.hunger += dt;
		c.age += dt;

		if( c.hunger >= maxHunger ) {
			killCreature( i );
			return;
		}

		if( c.age >= maxAge ) {
			killCreature( i );
			return;
		}

		if( RandomEvent( dt, secondsPerSpawn ) ) {
			spawnNewCreature( c.Position, c );
		}
	}

    private void MoveCreature( int i, float dt )
    {
        var c = creatures[i];
        c.Position += c.Velocity * dt;

        // clamp to world bounds / bounce logic
        if (c.x + creatureRadius > halfWidth || c.x - creatureRadius < -halfWidth)
        {
            c.vx *= -1f;
            c.x = Mathf.Clamp(c.x, -halfWidth + creatureRadius, halfWidth - creatureRadius);
        }
        if (c.y + creatureRadius > halfHeight || c.y - creatureRadius < -halfHeight)
        {
            c.vy *= -1f;
            c.y = Mathf.Clamp(c.y, -halfHeight + creatureRadius, halfHeight - creatureRadius);
        }
    }

    private void UpdateCreatureGridMembership( int i )
    {
        var c = creatures[i];

        int newgx = (int)((c.Position.x+halfWidth) / cellSize);
        int newgy = (int)((c.Position.y+halfHeight) / cellSize);

        if (newgx != c.gridX || newgy != c.gridY)
        {
            creatureGrid[c.gridX, c.gridY].Remove(i);
            creatureGrid[newgx, newgy].Add(i);

            c.gridX = newgx;
            c.gridY = newgy;
        }
    }

	private void initializeCreature( int i, Creature fromParent ) {
        Vector3 pos = new Vector3(
			UnityEngine.Random.Range( -halfWidth + creatureRadius, halfWidth - creatureRadius ),
            UnityEngine.Random.Range( -halfHeight + creatureRadius, halfHeight - creatureRadius ),
            0f
        );
		creatures[i].Position = pos;
		creatures[i].Velocity = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range( minSpeed, maxSpeed );
		creatures[i].age = UnityEngine.Random.Range( 0f, maxAge / 2f );
		creatures[i].hunger = UnityEngine.Random.Range( 0f, maxHunger / 5f );
		
		creatures[i].initializeBrain( fromParent );
	}

	private void initializeFood( int i ) {
        Vector3 pos = new Vector3( ThreadSafeRandom.Range( -halfWidth + creatureRadius, halfWidth - creatureRadius ), ThreadSafeRandom.Range( -halfHeight + creatureRadius, halfHeight - creatureRadius ), 0f );
		foods[i].Position = pos;
	}

    private void AddCreatureToGrid( int i )
    {
        var c = creatures[i];
        int gx = (int)((c.Position.x+halfWidth) / cellSize);
        int gy = (int)((c.Position.y+halfHeight) / cellSize);
        c.gridX = gx;
        c.gridY = gy;
        creatureGrid[gx, gy].Add(i);
    }

    private void AddFoodToGrid( int i )
    {
        var f = foods[i];
        int gx = (int)((f.Position.x+halfWidth) / cellSize);
        int gy = (int)((f.Position.y+halfHeight) / cellSize);
        f.gridX = gx;
        f.gridY = gy;
        foodGrid[gx, gy].Add( i );
    }

    public Creature[] Creatures => creatures;
    public Food[] Foods => foods;
}

public static class ThreadSafeRandom
{
    [ThreadStatic]
    private static System.Random local;

    private static System.Random rng
    {
        get
        {
            if (local == null)
            {
                // Unique seed per thread
                local = new System.Random(
                    Environment.TickCount * (Thread.CurrentThread.ManagedThreadId + 1)
                );
            }
            return local;
        }
    }

    // Float range (like UnityEngine.UnityEngine.Random.Range)
    public static float Range(float min, float max)
    {
        return (float)(rng.NextDouble() * (max - min) + min);
    }

    // Int range (like UnityEngine.UnityEngine.Random.Range)
    public static int Range(int min, int max)
    {
        return rng.Next(min, max);
    }

    // Equivalent to UnityEngine.UnityEngine.Random.insideUnitCircle
    public static Vector2 insideUnitCircle
    {
        get
        {
            // Uniform distribution inside a circle
            double angle = rng.NextDouble() * Math.PI * 2.0;
            double radius = Math.Sqrt(rng.NextDouble());

            return new Vector2(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius)
            );
        }
    }

    // Equivalent to UnityEngine.UnityEngine.Random.insideUnitCircle.normalized
    public static Vector2 unitVector
    {
        get
        {
            double angle = rng.NextDouble() * Math.PI * 2.0;
            return new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)
            );
        }
    }
}

