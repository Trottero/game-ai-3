using System.Collections;
using System.Collections.Generic;
using Models;
using UnityEngine;

public class TerrainChunk
{
    public Vector2 Position { get; private set; }
    public static GameObject WaterPrefab;
    private GameObject _meshObject;
    public MeshCollider MeshCollider;

    private Bounds _bounds;
    private TerrainGenerator _generator;

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    private Mesh _mesh;
    private MeshData _meshData;

    private bool _meshDataReceived;

    private GameObject _water;
    public TerrainChunk(TerrainGenerator generator, Vector2 coord, float scale, Transform parent, Material material)
    {
        _generator = generator;
        Position = coord;
        _bounds = new Bounds(Position * scale, Vector2.one * scale);
        Vector3 position3d = new Vector3(Position.x, 0, Position.y);

        _meshObject = new GameObject($"Terrain Chunk ({coord.x}, {coord.y})");
        _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
        _meshFilter = _meshObject.AddComponent<MeshFilter>();
        MeshCollider = _meshObject.AddComponent<MeshCollider>();
        _meshRenderer.material = material;

        _meshObject.transform.position = position3d;
        _meshObject.transform.parent = parent;

        _water = GameObject.Instantiate(WaterPrefab, position3d, Quaternion.Euler(0, 0, 0));
        _water.name = $"Water ({coord.x}, {coord.y})";
        _water.transform.parent = parent;

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

    public void Destroy()
    {
        GameObject.Destroy(_meshObject);
        GameObject.Destroy(_water);
    }
}
