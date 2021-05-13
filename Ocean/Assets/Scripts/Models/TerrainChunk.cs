using System.Collections;
using System.Collections.Generic;
using Models;
using UnityEngine;

public class TerrainChunk
{
    public Vector2 Position { get; private set; }
    private GameObject _meshObject;
    private Bounds _bounds;
    private TerrainGenerator _generator;

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    private Mesh _mesh;
    private MeshData _meshData;

    private bool _meshDataReceived;

    public TerrainChunk(TerrainGenerator generator, Vector2 coord, float scale, Transform parent, Material material)
    {
        _generator = generator;
        Position = coord * scale;
        _bounds = new Bounds(Position, Vector2.one * scale);
        Vector3 position3d = new Vector3(Position.x, 0, Position.y);

        _meshObject = new GameObject($"Terrain Chunk ({coord.x}, {coord.y})");
        _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
        _meshFilter = _meshObject.AddComponent<MeshFilter>();
        _meshCollider = _meshObject.AddComponent<MeshCollider>();
        _meshRenderer.material = material;

        _meshObject.transform.position = position3d;
        _meshObject.transform.parent = parent;

        SetVisible(false);

        _generator.RequestMeshData(Position, OnMeshReceived);
    }

    void OnMeshReceived(MeshData meshData)
    {
        _meshDataReceived = true;
        _meshData = meshData;

        var mesh = meshData.CreateMesh();
        _meshFilter.mesh = mesh;
        _meshCollider.sharedMesh = mesh;

        Update();
    }

    public void Update()
    {
        if (!_meshDataReceived) return;

        float viewerToEdge = Mathf.Sqrt(_bounds.SqrDistance(InfiniteTerrain.ViewerPosition));
        bool isVisible = viewerToEdge <= InfiniteTerrain.MaxViewingDistance;

        if (isVisible)
        {
            InfiniteTerrain.VisibleChunks.Add(this);
        }

        SetVisible(isVisible);
    }

    public void SetVisible(bool visible)
    {
        _meshObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return _meshObject.activeSelf;
    }
}
