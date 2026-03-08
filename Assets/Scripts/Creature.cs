using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public static class BrainConfig
{
	public const int N_INPUTS = 4;
	public const int N_HIDDEN = 8;
	public const int N_OUTPUTS = 2;
}

public struct NeuralNet
{
    // Input → Hidden
    public float[] w1;   // size: N_HIDDEN * N_INPUTS
    public float[] b1;   // size: N_HIDDEN

    // Hidden → Output
    public float[] w2;   // size: N_OUTPUTS * N_HIDDEN
    public float[] b2;   // size: N_OUTPUTS

    // Activations (no allocation after init)
    public float[] hidden;  // size: N_HIDDEN
    public float[] output;  // size: N_OUTPUTS

	public static float Tanh(float x)
	{
		// Numerically stable tanh for floats
		float e1 = Mathf.Exp(x);
		float e2 = Mathf.Exp(-x);
		return (e1 - e2) / (e1 + e2);
	}

	public static float FastTanh(float x)
	{
		// Clamp to avoid overflow
		if (x < -3f) return -1f;
		if (x > 3f) return 1f;

		float x2 = x * x;
		return x * (27f + x2) / (27f + 9f * x2);
	}

	public void Evaluate( float[] inputs )
	{
		// Hidden layer
		for (int h = 0; h < BrainConfig.N_HIDDEN; h++)
		{
			float sum = b1[h];
			int wIndex = h * BrainConfig.N_INPUTS;

			for (int i = 0; i < BrainConfig.N_INPUTS; i++)
				sum += w1[wIndex + i] * inputs[i];

			hidden[h] = FastTanh(sum);
		}

		// Output layer
		for (int o = 0; o < BrainConfig.N_OUTPUTS; o++)
		{
			float sum = b2[o];
			int wIndex = o * BrainConfig.N_HIDDEN;

			for (int h = 0; h < BrainConfig.N_HIDDEN; h++)
				sum += w2[wIndex + h] * hidden[h];

			output[o] = FastTanh(sum);
		}
	}
}

public class Creature
{
    public float[] input;
	public NeuralNet brain;
	
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

	public Vector2 nearestFood;
	public bool isFoodNearby = false;

	public void initializeBrain()
    {
        brain = new NeuralNet();

        brain.w1 = new float[BrainConfig.N_HIDDEN * BrainConfig.N_INPUTS];
        brain.b1 = new float[BrainConfig.N_HIDDEN];

        brain.w2 = new float[BrainConfig.N_OUTPUTS * BrainConfig.N_HIDDEN];
        brain.b2 = new float[BrainConfig.N_OUTPUTS];

        brain.hidden = new float[BrainConfig.N_HIDDEN];
        brain.output = new float[BrainConfig.N_OUTPUTS];

        input = new float[BrainConfig.N_INPUTS];

        // Optional: randomize weights here
        RandomizeBrain();
    }

	private void RandomizeBrain()
    {
        for (int i = 0; i < brain.w1.Length; i++)
            brain.w1[i] = UnityEngine.Random.Range(-1f, 1f);

        for (int i = 0; i < brain.b1.Length; i++)
            brain.b1[i] = UnityEngine.Random.Range(-1f, 1f);

        for (int i = 0; i < brain.w2.Length; i++)
            brain.w2[i] = UnityEngine.Random.Range(-1f, 1f);

        for (int i = 0; i < brain.b2.Length; i++)
            brain.b2[i] = UnityEngine.Random.Range(-1f, 1f);
    }

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

	public void runNetwork( bool isFoodNearby, float relativeFoodAngle ) {
		if (relativeFoodAngle > Mathf.PI) relativeFoodAngle -= 2f * Mathf.PI;
		if (relativeFoodAngle < -Mathf.PI) relativeFoodAngle += 2f * Mathf.PI;
		
		input[0] = isFoodNearby ? ( relativeFoodAngle / Mathf.PI ) : 0.0f;
		input[1] = isFoodNearby ? 1.0f : 0.0f;
		input[2] = 1.0f; // A non-zero bias to keep the network from getting all zero values.
		input[3] = 0f;

		//inputs[1] = normalizedDistanceToFood;
		//inputs[2] = energyLevel;
		//inputs[3] = currentTurnAngle;
		//inputs[4] = previousOutputTurn;
		//inputs[5] = previousOutputSpeed;

		brain.Evaluate( input );

		Velocity = computeNewVelocity( brain.output[0], brain.output[1] );
	}

	private Vector2 computeNewVelocity(float turnValue, float speedValue)
	{
		const float maxSpeed = 0.5f;
		const float maxTurnAngle = 90.0f;

		// Map -1..+1 → 0..1 (temporary, as you said)
		speedValue = (speedValue * 0.5f) + 0.5f;

		float speed = Mathf.Abs(speedValue) * maxSpeed;

		// Current facing angle from velocity
		float currentAngle = Mathf.Atan2(Velocity.y, Velocity.x);

		// Turn is a delta, not an absolute angle
		float turnAngle = turnValue * maxTurnAngle * Mathf.Deg2Rad;
		float newAngle = currentAngle + turnAngle;

		float x = Mathf.Cos(newAngle) * speed;
		float y = Mathf.Sin(newAngle) * speed;

		return new Vector2(x, y);
	}
}
