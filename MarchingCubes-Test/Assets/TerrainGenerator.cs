using UnityEngine;
using Tables;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System;

public class TerrainGenerator : MonoBehaviour
{



    [Header("General")]
    public float Scale = 32.0F;
    public int n_voxels = 32;
    public int RenderDistance = 3;
    public int Height = 3;


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
    public float VerticalVariance1 = 4f;
    public float VerticalAmplitude1 = 0.5f;
    public float VerticalVariance2 = 2f;
    public float VerticalAmplitude2 = 1f;
    public float VerticalVariance3 = 1f;
    public float VerticalAmplitude3 = 2f;
    public float VerticalVariance4 = 0.5f;
    public float VerticalAmplitude4 = 4f;


    [Header("Warping to worldspace")]
    public float WarpVariance = 0.05f;
    public float WarpAmplitude = 8f;

    [Header("Bottom / Ceiling")]
    public float CeilingLimit = 1.8f;
    public float CeilingDensity = 0.1f;
    public float BottomLimit = -0.4f;
    public float BottomDensity = 0.1f;

    [Header("Density")]
    public float SeabedVariance = 0.5f;
    public float SeabedDensity = 0.2f;
    public float UndergroundVariance = 0.005f;
    public float UndergroundDensity = 0.0f;


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

    private Vector3[] meshVertices;
    private int meshVerticesCount = 0;

    private int[] meshTriangles;

    private bool ready = false;


    // Start is called before the first frame update
    void Start()
    {
        // Load the triangle table in memory
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                CaseVoxelPolys[i, j] =
                    new Vector3Int(
                        Tables.Tables.PolyTable[i, j * 3],
                        Tables.Tables.PolyTable[i, j * 3 + 1],
                        Tables.Tables.PolyTable[i, j * 3 + 2]);
            }
        }

        CasePolyCounts = Tables.Tables.PolyCounts;
        ready = true;
        generateTerrain();
    }

    private void generateTerrain()
    {
        // Init vertex trackers
        meshVerticesCount = 0;
        // Total vertices
        meshVertices = new Vector3[n_voxels * n_voxels * n_voxels * 5 * 3 * RenderDistance * RenderDistance * Height];

        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        // For each chunk in the to render chunks, render it.
        for (int x = 0; x < RenderDistance; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < RenderDistance; z++)
                {
                    generateChunk(new Vector3(x, y, z));
                }
            }
        }

        stopWatch.Stop();
        // Get the elapsed time as a TimeSpan value.
        TimeSpan ts = stopWatch.Elapsed;

        UnityEngine.Debug.Log($"{meshVerticesCount} vertices created in {ts.TotalMilliseconds} ms");
        meshTriangles = Enumerable.Range(0, meshVerticesCount).ToArray();

        var mf = gameObject.GetComponent<MeshFilter>();

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();
        mesh.Optimize();
        mf.mesh = mesh;
    }

    private void generateChunk(Vector3 chunk)
    {
        var voxel_densities = new float[n_voxels + 1, n_voxels + 1, n_voxels + 1];

        // Calculate all of the densities
        for (int x = 0; x < n_voxels + 1; x++)
        {
            for (int y = 0; y < n_voxels + 1; y++)
            {
                for (int z = 0; z < n_voxels + 1; z++)
                {
                    var ws = transform.position + chunk;

                    ws += new Vector3((float)x / n_voxels, (float)y / n_voxels, (float)z / n_voxels);
                    // x,y,z coordinate of block accesible here.
                    voxel_densities[x, y, z] = density(ws);
                }
            }
        }


        // Generate one block at a time, 1 unit consists of 32 voxels.
        for (int x = 0; x < n_voxels; x++)
        {
            for (int y = 0; y < n_voxels; y++)
            {
                for (int z = 0; z < n_voxels; z++)
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
                            float unit_size = 1.0F / n_voxels;
                            var distanceFromA = 1.0F / abdist * Mathf.Abs(a_density) * unit_size;

                            // Create point relative to the origin of this rendering chunk.
                            var pt = new Vector3((float)x / n_voxels, (float)y / n_voxels, (float)z / n_voxels);

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

                            pt += chunk;

                            // Scale the point with the scale given.
                            pt *= Scale;

                            // Pad with position of the chunk

                            // Pad with the position of the GameObject
                            pt += transform.position;

                            // Store point in vertices for mesh
                            meshVertices[meshVerticesCount] = pt;
                            meshVerticesCount++;
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

    // Update is called once per frame
    void Update()
    {

    }

    void OnValidate()
    {
        if (ready)
        {
            generateTerrain();
        }
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
