using System.Collections;
using System.Collections.Generic;
using Models;
using UnityEngine;

public class TerrainChunk
{
    public Vector2 Position { get; private set; }
    public GameObject MeshObject { get; private set; }
    public MeshCollider MeshCollider;

    private Bounds _bounds;
    private TerrainGenerator _generator;

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    private Mesh _mesh;
    private MeshData _meshData;

    private bool _meshDataReceived;

    public TerrainChunk(TerrainGenerator generator, Vector2 coord, float scale, Transform parent, Material material)
    {
        _generator = generator;
        Position = coord;
        _bounds = new Bounds(Position * scale, Vector2.one * scale);
        Vector3 position3d = new Vector3(Position.x, 0, Position.y);

        MeshObject = new GameObject($"Terrain Chunk ({coord.x}, {coord.y})");
        _meshRenderer = MeshObject.AddComponent<MeshRenderer>();
        _meshFilter = MeshObject.AddComponent<MeshFilter>();
        MeshCollider = MeshObject.AddComponent<MeshCollider>();
        _meshRenderer.material = material;

        MeshObject.transform.position = position3d;
        MeshObject.transform.parent = parent;

        _generator.RequestMeshData(Position, OnMeshReceived);
    }

    void OnMeshReceived(MeshData meshData)
    {
        _meshDataReceived = true;
        _meshData = meshData;

        var mesh = meshData.CreateMesh();
        _meshFilter.mesh = mesh;
        MeshCollider.sharedMesh = mesh;

        Update();
    }

    public void Update()
    {
        if (!_meshDataReceived) return;

        var isVisible = IsVisible();

        if (isVisible)
        {
            InfiniteTerrain.VisibleChunks.Add(this);
        }
    }

    public bool IsVisible()
    {
        float viewerToEdge = Mathf.Sqrt(_bounds.SqrDistance(InfiniteTerrain.ViewerPosition));
        bool isVisible = viewerToEdge <= InfiniteTerrain.MaxViewingDistance;
        return isVisible;
    }
}
