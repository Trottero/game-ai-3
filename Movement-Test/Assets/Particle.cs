using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public float speed = 1f;
    public float turningSpeed = 1f;
    public float perceptionRange = 5f;
    public bool isHighlighted = false;

    public float CohesionFactor = 0.25f;
    public float CohesionRange = 5f;
    public float SeperationFactor = 0.25f;
    public float SeperationRange = 5f;
    public float AlignmentFactor = 0.25f;
    public float AlignmentRange = 5f;

    public float TargetFactor = 0.25f;
    public float EvasionFactor = 0.25f;
    public float EvasionRange = 5f;
    public int EvasionRaySamples = 100;
    public bool TrackTarget = true;

    private GameObject target;
    private GameObject[] _neighbours;
    private MeshCollider _boundingBoxCollider;
    private Vector3[] _spherePoints;
    private float _maxRange;

    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.Find("Target");
        _maxRange = new float[] { CohesionRange, SeperationRange, AlignmentRange, EvasionRange }.Max();
        _boundingBoxCollider = GameObject.Find("BoundingBox").GetComponent<MeshCollider>();
        _spherePoints = new Vector3[EvasionRaySamples];

        _neighbours = GameObject.FindGameObjectsWithTag("ParticleGameObject")
            // Exclude self and calculate distances
            .Where(x => x.name != name)
            .ToArray();

    }

    // Update is called once per frame
    void Update()
    {

        // Get vector for target.
        var targetvec = computeTarget();

        // Evade objects
        var evasion = computeEvasion();

        // Change our rotation so we avoid other particles
        var separation = computeSeperation(_neighbours);

        // Prefer to move to the center of our neighbours
        var cohesion = computeCohesion(_neighbours);

        // Align with other particles
        var alignment = computeAlignment(_neighbours);

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
        Debug.DrawLine(transform.position, transform.position + optimal_vector, Color.magenta, 0.0f);

        // Calculate Quaternion
        float singleStep = turningSpeed * Time.deltaTime;
        var rotvec = Vector3.RotateTowards(transform.forward, optimal_vector, singleStep, 0.0f);

        // Set the rotation
        transform.rotation = Quaternion.LookRotation(rotvec);
    }

    void OnDrawGizmos()
    {
        if (isHighlighted)
        {
            foreach (var item in _neighbours)
            {
                Debug.DrawLine(transform.position, item.transform.position, Color.green, 0.0F);
            }
            Gizmos.DrawWireSphere(transform.position, _maxRange);
        }
    }

    private void moveForward()
    {
        // Move ourselves forward in current direction.
        transform.localPosition += speed * transform.forward * Time.deltaTime;
    }

    private Vector3 computeTarget()
    {
        if (target == null || !TrackTarget)
        {
            return Vector3.zero;
        }

        // Pad coordinate system with our own localPosition
        Vector3 targetDirection = target.transform.position - transform.position;

        return targetDirection.normalized;
    }

    private Vector3 computeSeperation(GameObject[] neighbours)
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

    private bool neighbourInRange(Transform neighbour, float seperationRange)
    {
        return Vector3.Distance(transform.position, neighbour.position) < seperationRange;
    }

    private Vector3 computeCohesion(GameObject[] neighbours)
    {

        if (neighbours.Length == 0)
        {
            // Avoids division by 0.
            return Vector3.zero;
        }

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

        // avg vector assuming current fish 0.0
        result /= n_neighs;

        return result.normalized;
    }

    private Vector3 computeAlignment(GameObject[] neighbours)
    {
        if (neighbours.Length == 0)
        {
            // Avoids division by 0.
            return Vector3.zero;
        }

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

        // avg vector assuming current fish 0.0
        result /= n_neighs;

        return result.normalized;
    }

    private Vector3 computeEvasion()
    {
        // Computes the vector required to avoid obstacles.

        // Populate sphere points array with points
        getPerceptionSphere(EvasionRaySamples, _spherePoints);

        var resultvec = new Vector3();
        var n_collisions = 0;

        // Calculate the sum of the distances between the particle and the collider
        for (int i = 0; i < EvasionRaySamples; i++)
        {
            // Cast ray from fish to point and check if it intersects with an object.
            var ray = new Ray(transform.position, _spherePoints[i]);
            if (_boundingBoxCollider.Raycast(ray, out var hitinfo, EvasionRange))
            {
                // Add hitinfo position relative to position
                resultvec += hitinfo.point - transform.position;
                n_collisions++;
                if (isHighlighted && n_collisions % 5 == 0)
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

        if (isHighlighted)
        {
            Debug.DrawLine(transform.position, transform.position + resultvec, Color.cyan);
        }

        return resultvec.normalized;
    }

    private void getPerceptionSphere(int n_samples, Vector3[] spherePoints)
    {
        // Return all points on the sphere of perception
        for (int i = 0; i < n_samples; i++)
        {
            var phi = Math.Acos(1 - 2 * ((float)i + 0.5) / n_samples);
            var theta = Math.PI * (1 + Math.Sqrt(5)) * (float)i + 0.5;
            spherePoints[i].x = (float)Math.Cos(theta) * (float)Math.Sin(phi);
            spherePoints[i].y = (float)Math.Sin(theta) * (float)Math.Sin(phi);
            spherePoints[i].z = (float)Math.Cos(phi);
        }
    }
}
