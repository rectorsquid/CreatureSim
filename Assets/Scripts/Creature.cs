using System;
using System.Collections.Generic;
using UnityEngine;

public class Creature
{
	public float w;
	public float b;
	
	private float turnAngle;

	public bool isAlive = true;

    public float x, y;
    public float vx, vy;

    //public float tempx,tempy;
    //public float tempvx, tempvy;

    public int gridX;
    public int gridY;

	public float age = 0f;
	public float hunger = 0f;

	public List<int> neighbors = new List<int>( 64 );
	public List<int> nearbyFood = new List<int>( 16 );

    public Vector2 Position
    {
        get => new Vector2(x, y);
        set { x = value.x; y = value.y; }
    }

    public Vector2 Velocity
    {
        get => new Vector2(vx, vy);
        set { vx = value.x; vy = value.y; }
    }

    /*public Vector2 tempPosition
    {
        get => new Vector2(tempx, tempy);
        set { tempx = value.x; tempy = value.y; }
    }

    public Vector2 tempVelocity
    {
        get => new Vector2(tempvx, tempvy);
        set { tempvx = value.x; tempvy = value.y; }
    }*/

	public void runNetwork( float foodAngleInRadians ) {
		float myAngle = Mathf.Atan2( vy, vx );
		float relativeAngle = foodAngleInRadians - myAngle;

		if (relativeAngle > Mathf.PI) relativeAngle -= 2f * Mathf.PI;
		if (relativeAngle < -Mathf.PI) relativeAngle += 2f * Mathf.PI;

		float input = (relativeAngle + Mathf.PI) / (2f * Mathf.PI);

		float linear = w * input + b; 
		float sigmoided = 1f / (1f + Mathf.Exp(-linear));

		Velocity = computeNewVelocity( sigmoided );
	}

	private Vector2 computeNewVelocity( float turnValue ) {
		Vector2 vel = Velocity;
		float signedTurn = ( turnValue - 0.5f ) * 200f;

		float rad = signedTurn * Mathf.Deg2Rad;
		float cos = Mathf.Cos(rad);
		float sin = Mathf.Sin(rad);

		Vector2 newVel = new Vector2(
			vel.x * cos - vel.y * sin,
			vel.x * sin + vel.y * cos
		);

		newVel = newVel.normalized * vel.magnitude;

		return newVel;
	}
}
