using System;
using System.Collections.Generic;
using UnityEngine;

public class Creature
{
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
}
