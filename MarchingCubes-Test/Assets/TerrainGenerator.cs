using UnityEngine;
using Tables;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System;

public class TerrainGenerator : MonoBehaviour
{
    public float Scale = 32.0F;
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

    public int n_voxels = 32;

    // Start is called before the first frame update
    void Start()
    {
        // Total vertices
        meshVertices = new Vector3[n_voxels * n_voxels * n_voxels * 3 * 3 * 9];


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

        var chunk = new Vector3(0, 0, 0);


        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        var voxel_densities = new float[n_voxels + 1, n_voxels + 1, n_voxels + 1];

        // Calculate all of the densities
        for (int x = 0; x < n_voxels + 1; x++)
        {
            for (int y = 0; y < n_voxels + 1; y++)
            {
                for (int z = 0; z < n_voxels + 1; z++)
                {
                    var ws = transform.position + chunk * Scale;

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


        stopWatch.Stop();
        // Get the elapsed time as a TimeSpan value.
        TimeSpan ts = stopWatch.Elapsed;

        UnityEngine.Debug.Log($"{meshVerticesCount} vertices created in {ts.TotalMilliseconds} ms");
        meshTriangles = Enumerable.Range(0, meshVerticesCount).ToArray();

        var mf = gameObject.GetComponent<MeshFilter>();

        var mesh = new Mesh();

        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();
        mesh.Optimize();
        mf.mesh = mesh;
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

    void GenerateChunk()
    {
        // Adds a given chunk to the vertex buffer

    }

    // Update is called once per frame
    void Update()
    {

    }


    float density(Vector3 point)
    {
        // Density function: Determines the density at a certain point
        // Negative = empty space
        // Positive = It is inside the shape

        // Ground plane
        // Debug.Log(d + "  " + point.y);
        float density = -point.y;

        density += (Mathf.PerlinNoise(point.x * 2, point.z * 2) - 0.5f);
        density += Mathf.PerlinNoise(point.x * 4, point.z * 4) * 0.25f - 0.125f;
        density += Mathf.PerlinNoise(point.x, point.z) - 0.5f;
        // Add intensitie for y
        // density += perlin(point.y * 1, 0);
        density += perlin(point.y * 3, 0) * 0.5f;
        density += perlin(point.y * 8, 0) * 0.125f;

        return density;
    }

    float perlin(float x, float y)
    {
        // Returns perlin noise but from [-1, 1] 
        return (Mathf.PerlinNoise(x, y) - 0.5f) * 2;
    }
}
