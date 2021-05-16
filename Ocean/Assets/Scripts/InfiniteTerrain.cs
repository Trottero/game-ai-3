using System.Collections.Generic;
using System.Linq;
using Models;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    private const float ViewerMoveThresholdForChunkUpdate = 25f;

    private const float SquaredViewerMoveThresholdForChunkUpdate =
        ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

    public static float MaxViewingDistance = 150f;
    public static float Scale = 32.0f;
    public static Vector2 ViewerPosition;

    public Transform Viewer;
    public Material MapMaterial;

    private int _numberOfChunksVisible;
    private TerrainGenerator _generator;

    private Vector2 _previousViewerPosition;
    public static MeshCollider MeshCollider;

    public static Dictionary<Vector2, TerrainChunk> Chunks = new Dictionary<Vector2, TerrainChunk>();
    public static List<TerrainChunk> VisibleChunks = new List<TerrainChunk>();

    void Start()
    {
        // MaxViewingDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;

        _generator = FindObjectOfType<TerrainGenerator>();

        _numberOfChunksVisible = Mathf.RoundToInt(MaxViewingDistance / Scale);
        MeshCollider = gameObject.AddComponent<MeshCollider>();
        MeshCollider.sharedMesh = new Mesh();
        MeshCollider.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;


        UpdateVisibleChunks();
    }

    void Update()
    {
        var position = Viewer.position;
        ViewerPosition = new Vector2(position.x, position.z);

        if ((_previousViewerPosition - ViewerPosition).sqrMagnitude > SquaredViewerMoveThresholdForChunkUpdate)
        {
            _previousViewerPosition = ViewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        // set everything to invisible
        // VisibleChunks.ForEach(chunk => chunk.SetVisible(false));
        var vischunks = new List<Vector2>();

        int x = Mathf.RoundToInt(ViewerPosition.x / Scale);
        int y = Mathf.RoundToInt(ViewerPosition.y / Scale);

        // check if chunks are visible
        for (int yOffset = -_numberOfChunksVisible; yOffset <= _numberOfChunksVisible; yOffset++)
        {
            for (int xOffset = -_numberOfChunksVisible; xOffset <= _numberOfChunksVisible; xOffset++)
            {
                Vector2 chunk = new Vector2(x + xOffset, y + yOffset);

                if (Chunks.ContainsKey(chunk))
                {
                    Chunks[chunk].Update();
                }
                else
                {
                    Chunks.Add(chunk, new TerrainChunk(_generator, chunk, Scale, transform, MapMaterial));
                }

                vischunks.Add(chunk);
            }
        }
        Chunks.Where(chunk => !vischunks.Contains(chunk.Key)).ToList().ForEach(x => Destroy(x.Value.MeshObject));

        // remove chunks that are invisible since this update
        Chunks = Chunks.Where(chunk => vischunks.Contains(chunk.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
