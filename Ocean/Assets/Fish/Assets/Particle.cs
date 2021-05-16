using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public float Speed = 5f;
    public float TurningSpeed = 2.5f;
    public bool ShowDebugGizmos = false;
    public float UpdateRange = 50f;

    [Header("Swarm Parameters")]
    public float CohesionFactor = 0.25f;
    public float CohesionRange = 5f;
    public float SeperationFactor = 0.25f;
    public float SeperationRange = 5f;
    public float AlignmentFactor = 0.25f;
    public float AlignmentRange = 5f;

    [Header("Target tracking")]
    public bool TrackTarget = true;
    public float TargetFactor = 0.25f;
    public float TargetRange = 5f;
    public GameObject Target;


    [Header("Environment evasion")]
    public float EvasionFactor = 0.25f;
    public float EvasionRange = 5f;
    public int EvasionRaySamples = 100;
    public static MeshCollider EnvironmentCollider;

    // Private variables
    public static GameObject[] neighbours;
    private Vector3[] spherePoints;
    private float variableSpeed;

    // Start is called before the first frame update
    void Start()
    {
        Target = Target == null ? GameObject.Find("Target") : Target;

        spherePoints = new Vector3[EvasionRaySamples];
        variableSpeed = Speed;
        neighbours = GameObject.FindGameObjectsWithTag("ParticleGameObject")
            .Where(x => x.name != name)
            .ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        // Should update
        float viewerToFish = Vector2.Distance(new Vector3(transform.position.x, transform.position.z), InfiniteTerrain.ViewerPosition);
        // if (viewerToFish > UpdateRange)
        // {
        //     // Skip update
        //     return;
        // }

        // Get vector for target.
        var targetvec = computeTarget();

        // Evade objects
        var evasion = computeEvasion();

        // Change our rotation so we avoid other particles
        var separation = computeSeperation();

        // Prefer to move to the center of our neighbouring particles
        var cohesion = computeCohesion();

        // Align with other particles
        var alignment = computeAlignment();

        // Calculate the shit
        var optimal_vector = (SeperationFactor * separation + CohesionFactor * cohesion + TargetFactor * targetvec + AlignmentFactor * alignment + EvasionFactor * evasion).normalized;

        // Finally rotate the fish
        rotateFish(optimal_vector);

        // Debug ray shows the updated rotation
        Debug.DrawRay(transform.position, transform.forward * 2, Color.red, 0.0f);

        // Finally move forward with our current rotation
        moveForward();
    }

    private void rotateFish(Vector3 optimal_vector)
    {
        // Since default is zero vector, only rotate when the vector is not zero
        if (optimal_vector == Vector3.zero)
        {
            return;
        }

        // Draws a line to the new optimal trajectory
        if (ShowDebugGizmos)
        {
            Debug.DrawLine(transform.position, transform.position + optimal_vector, Color.magenta, 0.0f);
        }

        // Calculate Quaternion
        float singleStep = TurningSpeed * Time.deltaTime;
        var rotvec = Vector3.RotateTowards(transform.forward, optimal_vector, singleStep, 0.0f);

        // Set the rotation
        transform.rotation = Quaternion.LookRotation(rotvec);
    }

    void OnDrawGizmos()
    {
        // if (ShowDebugGizmos)
        // {
        //     // Draw a line to all of its neighbours
        //     // foreach (var item in neighbours)
        //     // {
        //     //     Debug.DrawLine(transform.position, item.transform.position, Color.green, 0.0F);
        //     // }
        //     Gizmos.DrawWireSphere(transform.position, new float[] { CohesionRange, SeperationRange, AlignmentRange, EvasionRange }.Max());
        // }
    }

    private void moveForward()
    {
        // Move ourselves forward in current direction.
        transform.localPosition += variableSpeed * transform.forward * Time.deltaTime;
    }

    private Vector3 computeTarget()
    {
        if (Target == null || !TrackTarget)
        {
            return Vector3.zero;
        }

        // Check if target is in range, if it isn't, just return zero vector
        if (Vector3.Distance(Target.transform.position, transform.position) > TargetRange)
        {
            return Vector3.zero;
        }

        // Pad coordinate system with our own localPosition
        Vector3 targetDirection = Target.transform.position - transform.position;

        return targetDirection.normalized;
    }

    private bool neighbourInRange(Transform neighbour, float seperationRange)
    {
        return Vector3.Distance(transform.position, neighbour.position) < seperationRange;
    }

    private Vector3 computeSeperation()
    {
        var result = new Vector3();
        for (int i = 0; i < neighbours.Length; i++)
        {
            if (neighbourInRange(neighbours[i].transform, SeperationRange))
            {
                result += neighbours[i].transform.position - transform.position;
            }
        }

        // Invert vector
        result *= -1;

        return result.normalized;
    }

    private Vector3 computeCohesion()
    {
        var result = new Vector3();
        var n_neighs = 0;
        for (int i = 0; i < neighbours.Length; i++)
        {
            if (neighbourInRange(neighbours[i].transform, CohesionRange))
            {
                result += neighbours[i].transform.position - transform.position;
                n_neighs++;
            }
        }

        if (n_neighs == 0)
        {
            // Avoids division by 0.
            return Vector3.zero;
        }

        // avg vector assuming current fish 0.0
        result /= n_neighs;

        return result.normalized;
    }

    private Vector3 computeAlignment()
    {
        var result = new Vector3();
        var n_neighs = 0;
        for (int i = 0; i < neighbours.Length; i++)
        {
            if (neighbourInRange(neighbours[i].transform, AlignmentRange))
            {
                result += neighbours[i].transform.forward;
                n_neighs++;
            }
        }

        if (n_neighs == 0)
        {
            // Avoids division by 0.
            return Vector3.zero;
        }

        // avg vector assuming current fish 0.0
        result /= n_neighs;

        return result.normalized;
    }

    private Vector3 computeEvasion()
    {
        // Computes the vector required to avoid obstacles.

        // Populate sphere points array with points
        getPerceptionSphere(spherePoints);

        var resultvec = new Vector3();
        var n_collisions = 0;
        var minDistance = EvasionRange;

        var pos3d = transform.position / InfiniteTerrain.Scale;
        var collider = InfiniteTerrain.Chunks[new Vector2((int)pos3d.x, (int)pos3d.z)].MeshCollider;

        // Calculate the sum of the distances between the particle and the collider
        for (int i = 0; i < EvasionRaySamples; i++)
        {

            // Cast ray from fish to point and check if it intersects with an object.
            var ray = new Ray(transform.position, spherePoints[i]);

            if (collider.Raycast(ray, out var hitinfo, EvasionRange))
            {
                // Check if this sphere is in this angle
                if (ShowDebugGizmos)
                {
                    Debug.Log(Vector3.Angle(transform.forward, spherePoints[i]));
                }

                // Add hitinfo position relative to position
                resultvec += hitinfo.point - transform.position;
                n_collisions++;

                if (hitinfo.distance < minDistance)
                {
                    minDistance = hitinfo.distance;
                }

                if (ShowDebugGizmos)
                {
                    Debug.DrawLine(transform.position, hitinfo.point, Color.blue, 0.0f);
                }
            }
        }

        if (n_collisions == 0)
        {
            return Vector3.zero;
        }


        // Take average
        resultvec /= (float)n_collisions;

        // Calculate the inverse vector, just like in seperation.
        resultvec = resultvec * -1;

        // Adjust the speed of the fish to avoid collision
        variableSpeed = RecalculateSpeed(minDistance, resultvec);
        if (ShowDebugGizmos)
        {
            Debug.DrawLine(transform.position, transform.position + resultvec, Color.cyan);
        }

        return resultvec.normalized;
    }

    private void getPerceptionSphere(Vector3[] spherePoints)
    {
        // Return all points on the sphere of perception
        for (int i = 0; i < spherePoints.Length; i++)
        {
            var phi = Math.Acos(1 - 2 * ((float)i + 0.5) / spherePoints.Length);
            var theta = Math.PI * (1 + Math.Sqrt(5)) * (float)i + 0.5; // Golden ratio
            spherePoints[i].x = (float)Math.Cos(theta) * (float)Math.Sin(phi);
            spherePoints[i].y = (float)Math.Sin(theta) * (float)Math.Sin(phi);
            spherePoints[i].z = (float)Math.Cos(phi);
        }
    }

    // Recalculates the speed of the fish based on the chance of it colliding with the environment.
    private float RecalculateSpeed(float distance, Vector3 optimalAngle)
    {
        var minDistanceFromWall = 1.0f;

        // compute gradient
        var diff = EvasionRange - minDistanceFromWall;
        var gradient = Speed / diff;
        var adjustedSpeed = distance * gradient - gradient;
        adjustedSpeed = Mathf.Clamp(adjustedSpeed, 0, Speed);

        var angle = Vector3.Angle(transform.forward, optimalAngle);
        if (angle < 45.0f && distance < 1.5f)
        {
            var scaledSpeed = Speed * 0.1f;
            adjustedSpeed = scaledSpeed - angle * scaledSpeed / 45.0f;
        }
        return adjustedSpeed;
    }
}
