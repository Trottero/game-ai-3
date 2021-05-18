using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Models;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("General")]
    public float Scale = 32.0F;
    public int numberOfVoxels = 16;
    public int HorizontalRenderDistance = 3;
    public int VerticalRenderDistance = 3;
    public int Seed;
    public int MaxFishPerChunk = 3;

    [Header("Noise weights")]
    public float HorizontalWeight = 1f;
    public float VerticalWeight = 1f;

    [Header("Horizontal noise")]
    public float HorizontalVariance1 = 4f;
    public float HorizontalAmplitude1 = 0.125f;
    public float HorizontalVariance2 = 2f;
    public float HorizontalAmplitude2 = 0.5f;
    public float HorizontalVariance3 = 0f;
    public float HorizontalAmplitude3 = 0f;
    public float HorizontalVariance4 = 0f;
    public float HorizontalAmplitude4 = 0f;

    [Header("Vertical noise")]
    public float VerticalVariance1 = 1f;
    public float VerticalAmplitude1 = 0.5f;
    public float VerticalVariance2 = 4f;
    public float VerticalAmplitude2 = 0.125f;
    public float VerticalVariance3 = 0f;
    public float VerticalAmplitude3 = 0f;
    public float VerticalVariance4 = 0f;
    public float VerticalAmplitude4 = 0f;


    [Header("Warping to worldspace")]
    public float WarpVariance = 0.05f;
    public float WarpAmplitude = 0f;

    [Header("Bottom / Ceiling")]
    public float CeilingLimit = 2.4f;
    public float CeilingDensity = 4f;
    public float BottomLimit = -0.25f;
    public float BottomDensity = 2f;

    [Header("Density")]
    public float SeabedVariance = 0.5f;
    public float SeabedDensity = 0.2f;
    public float UndergroundVariance = 0.005f;
    public float UndergroundDensity = 0.0f;


    private Queue<QueuedAction<MeshData>> _meshDataActionQueue = new Queue<QueuedAction<MeshData>>();
    private int[] CasePolyCounts;
    private Vector3Int[,] CaseVoxelPolys = new Vector3Int[256, 5];

    private Vector3Int[,] EdgeVertices = new Vector3Int[,]{
        { new Vector3Int(0, 0, 0 ), new Vector3Int(0, 1, 0 ) }, { new Vector3Int(0, 1, 0 ), new Vector3Int(1, 1, 0 ) }, { new Vector3Int(1, 0, 0 ), new Vector3Int(1, 1, 0 ) }, { new Vector3Int(0, 0, 0 ), new Vector3Int(1, 0, 0 ) },
        { new Vector3Int(0, 0, 1 ), new Vector3Int(0, 1, 1 ) }, { new Vector3Int(0, 1, 1 ), new Vector3Int(1, 1, 1 ) }, { new Vector3Int(1, 0, 1 ), new Vector3Int(1, 1, 1 ) }, { new Vector3Int(0, 0, 1 ), new Vector3Int(1, 0, 1 ) },
        { new Vector3Int(0, 0, 0 ), new Vector3Int(0, 0, 1 ) }, { new Vector3Int(0, 1, 0 ), new Vector3Int(0, 1, 1 ) }, { new Vector3Int(1, 1, 0 ), new Vector3Int(1, 1, 1 ) }, { new Vector3Int(1, 0, 0 ), new Vector3Int(1, 0, 1 ) }
    };

    // Used to easily loop through the vertices in the correct order (clock-wise x, y) and then again with z + 1
    private Vector3Int[] VertexOffsets = new Vector3Int[]{
        new Vector3Int(0, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0),
        new Vector3Int(0, 0, 1), new Vector3Int(0, 1, 1), new Vector3Int(1, 1, 1), new Vector3Int(1, 0, 1)
    };

    private bool tablesLoaded = false;
    void Start()
    {
        // Load the triangle table in memory
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                CaseVoxelPolys[i, j] =
                    new Vector3Int(
                        Tables.PolyTable[i, j * 3],
                        Tables.PolyTable[i, j * 3 + 1],
                        Tables.PolyTable[i, j * 3 + 2]);
            }
        }

        CasePolyCounts = Tables.PolyCounts;

        tablesLoaded = true;
    }

    void Update()
    {
        lock (_meshDataActionQueue)
        {
            while (_meshDataActionQueue.Any())
            {
                var queuedAction = _meshDataActionQueue.Dequeue();
                queuedAction.Action(queuedAction.Payload);
            }
        }
    }

    public void RequestMeshData(Vector2 pos, Action<MeshData> callback)
    {
        void ThreadStart()
        {
            GenerateMeshData(pos, callback);
        }

        new Thread(ThreadStart).Start();
    }

    private void GenerateMeshData(Vector2 pos, Action<MeshData> callback)
    {
        while (!tablesLoaded)
        {

        }

        var meshData = new MeshData(numberOfVoxels, VerticalRenderDistance);

        generateVerticalChunk(pos, meshData);

        lock (_meshDataActionQueue)
        {
            _meshDataActionQueue.Enqueue(new QueuedAction<MeshData> { Action = callback, Payload = meshData });
        }
    }

    private void generateVerticalChunk(Vector2 chunk, MeshData meshData)
    {
        for (int i = 0; i < VerticalRenderDistance; i++)
        {
            generateChunk(new Vector3(chunk.x, i, chunk.y), meshData);
        }
    }

    private void generateChunk(Vector3 chunk, MeshData meshData)
    {
        var voxel_densities = new float[numberOfVoxels + 1, numberOfVoxels + 1, numberOfVoxels + 1];

        // Calculate all of the densities
        for (int x = 0; x < numberOfVoxels + 1; x++)
        {
            for (int y = 0; y < numberOfVoxels + 1; y++)
            {
                for (int z = 0; z < numberOfVoxels + 1; z++)
                {
                    var ws = chunk;

                    ws += new Vector3((float)x / numberOfVoxels, (float)y / numberOfVoxels, (float)z / numberOfVoxels);
                    // x,y,z coordinate of block accesible here.
                    voxel_densities[x, y, z] = density(ws);
                }
            }
        }

        var fishCounter = 0;

        // Generate one block at a time, 1 unit consists of 32 voxels.
        for (int x = 0; x < numberOfVoxels; x++)
        {
            for (int y = 0; y < numberOfVoxels; y++)
            {
                for (int z = 0; z < numberOfVoxels; z++)
                {
                    var cubeCase = 0;
                    var exponent = 1;

                    // Transform densities to bits and calculate decimal
                    for (int v = 0; v < 8; v++)
                    {
                        var offset = VertexOffsets[v];
                        cubeCase += to_binary(voxel_densities[x + offset.x, y + offset.y, z + offset.z]) * exponent;
                        exponent *= 2;
                    }

                    if (cubeCase == 0) // Empty case
                    {
                        if (fishCounter < MaxFishPerChunk)
                        {
                            var pos = new Vector3((float)x / numberOfVoxels, (float)y / numberOfVoxels, (float)z / numberOfVoxels);
                            pos += chunk;
                            pos *= Scale;
                            pos.x -= chunk.x;
                            pos.z -= chunk.z;
                            FishGenerator.queueFishCreation(pos);
                            fishCounter++;
                        }
                    }

                    // Get number of polys given the current case
                    var n_polys = CasePolyCounts[cubeCase];

                    for (int poly = 0; poly < n_polys; poly++)
                    {
                        // Polygon exists of a list of edge indices that it connects to
                        var polygon = CaseVoxelPolys[cubeCase, poly];

                        // For all edges in the polygon (3)
                        for (int e = 0; e < 3; e++)
                        {
                            // Get edge index from this polygon
                            var edge = polygon[e];

                            // Gets offsets for vertices based on a given edge
                            var vertex_a = EdgeVertices[edge, 0]; // From
                            var vertex_b = EdgeVertices[edge, 1]; // To

                            // Retrieve densities for current point + offset
                            var a_density = voxel_densities[x + vertex_a.x, y + vertex_a.y, z + vertex_a.z];
                            var b_density = voxel_densities[x + vertex_b.x, y + vertex_b.y, z + vertex_b.z];

                            // Subtract voxel density of A from B
                            var abdist = Mathf.Abs(a_density - b_density);

                            // Calculate distance from pt a.
                            float unit_size = 1.0F / numberOfVoxels;
                            var distanceFromA = 1.0F / abdist * Mathf.Abs(a_density) * unit_size;

                            // Create point relative to the origin of this rendering chunk.
                            var pt = new Vector3((float)x / numberOfVoxels, (float)y / numberOfVoxels, (float)z / numberOfVoxels);

                            // Position depends on the way the vertice is running.
                            // Only one changes, we need to find that one and use that to set our unit
                            // This can easily be done by comparing the offsets.
                            if (vertex_a.x != vertex_b.x)
                            {
                                pt.x += distanceFromA;
                            }
                            else if (vertex_a.y != vertex_b.y)
                            {
                                pt.y += distanceFromA;
                            }
                            else if (vertex_a.z != vertex_b.z)
                            {
                                pt.z += distanceFromA;
                            }

                            // Pad the vertex with vertex_a. e.g when it is in the back it should be + (0, 0, 1) (x, y, z)
                            // This is multiplied by unit_size to make sure it stays inside the voxel
                            pt += new Vector3(vertex_a.x, vertex_a.y, vertex_a.z) * unit_size;

                            // Pad with position of the chunk
                            pt += chunk;

                            // Scale the point with the scale given.
                            pt *= Scale;

                            // some dumb shit that I have to do with the chunk system wtf bro
                            pt.x -= chunk.x;
                            pt.z -= chunk.z;

                            // Store point in vertices for mesh
                            meshData.AddVertex(pt);
                        }
                    }
                }
            }
        }
    }

    int to_binary(float number)
    {
        // Returns 1 if positive 0 if negative
        if (number >= 0)
        {
            return 1;
        }
        return 0;
    }

    float density(Vector3 point)
    {
        // Density function: Determines the density at a certain point
        // Negative = empty space
        // Positive = It is inside the shape

        // Ground plane
        // Debug.Log(d + "  " + point.y);

        var original = point;

        float density = 0.0f;

        if (point.y > 1.5)
        {
            density += perlin(point.x, point.z, SeabedVariance) * SeabedDensity;
        }
        else
        {
            density += perlin(point.x, point.z, UndergroundVariance) * UndergroundDensity;
        }

        // Warping
        var warpx = perlin(point.x, 0, WarpVariance);
        var warpy = perlin(point.y, 0, WarpVariance);
        var warpz = perlin(point.z, 0, WarpVariance);

        point += new Vector3(warpx, warpy, warpz) * WarpAmplitude;

        var horizontalDensity = perlin(point.x, point.z, HorizontalVariance1) * HorizontalAmplitude1;
        horizontalDensity += perlin(point.x, point.z, HorizontalVariance2) * HorizontalAmplitude2;
        horizontalDensity += perlin(point.x, point.z, HorizontalVariance3) * HorizontalAmplitude3;
        horizontalDensity += perlin(point.x, point.z, HorizontalVariance4) * HorizontalAmplitude4;

        density += horizontalDensity * HorizontalWeight;

        var verticalDensity = perlin(point.y, 0, VerticalVariance1) * VerticalAmplitude1;
        verticalDensity += perlin(point.y, 0, VerticalVariance2) * VerticalAmplitude2;
        verticalDensity += perlin(point.y, 0, VerticalVariance3) * VerticalAmplitude3;
        verticalDensity += perlin(point.y, 0, VerticalVariance4) * VerticalAmplitude4;

        density += verticalDensity * VerticalWeight;

        if (point.y > CeilingLimit)
        {
            density -= Math.Abs(point.y - CeilingLimit) * CeilingDensity;
        }

        if (point.y < BottomLimit)
        {
            density += Math.Abs(point.y - BottomLimit) * BottomDensity; // Linear equation
        }

        return density;
    }

    float perlin(float x, float y, float variance = 1f)
    {
        // Returns perlin noise but from [-1, 1]
        return (Mathf.PerlinNoise(x * variance, y * variance) - 0.5f) * 2;
    }


}
