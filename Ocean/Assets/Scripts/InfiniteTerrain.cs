using System.Collections.Generic;
using System.Linq;
using Models;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    private const float ViewerMoveThresholdForChunkUpdate = 25f;

    private const float SquaredViewerMoveThresholdForChunkUpdate =
        ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

    public static float MaxViewingDistance;
    public static float Scale;
    public static Vector2 ViewerPosition;

    public Transform Viewer;
    public Material MapMaterial;

    private int _numberOfChunksVisible;
    private TerrainGenerator _generator;

    private Vector2 _previousViewerPosition;

    private Dictionary<Vector2, TerrainChunk> _chunks = new Dictionary<Vector2, TerrainChunk>();
    public static List<TerrainChunk> VisibleChunks = new List<TerrainChunk>();

    void Start()
    {
        // MaxViewingDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;

        _generator = FindObjectOfType<TerrainGenerator>();
        // _chunkSize = TerrainGenerator.MapChunkSize - 1;
        // _numberOfChunksVisible = Mathf.RoundToInt(MaxViewingDistance / _chunkSize);

        UpdateVisibleChunks();
    }

    void Update()
    {
        var position = Viewer.position;
        ViewerPosition = new Vector2(position.x, position.z);

        // if ((_previousViewerPosition - ViewerPosition).sqrMagnitude > SquaredViewerMoveThresholdForChunkUpdate)
        {
            _previousViewerPosition = ViewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        // set everything to invisible
        VisibleChunks.ForEach(chunk => chunk.SetVisible(false));
        VisibleChunks.Clear();

        int x = Mathf.RoundToInt(ViewerPosition.x / Scale);
        int y = Mathf.RoundToInt(ViewerPosition.y / Scale);

        // check if chunks are visible
        for (int yOffset = -_numberOfChunksVisible; yOffset <= _numberOfChunksVisible; yOffset++)
        {
            for (int xOffset = -_numberOfChunksVisible; xOffset <= _numberOfChunksVisible; xOffset++)
            {
                Vector2 chunk = new Vector2(x + xOffset, y + yOffset);

                if (_chunks.ContainsKey(chunk))
                {
                    _chunks[chunk].Update();
                }
                else
                {
                    _chunks.Add(chunk, new TerrainChunk(_generator, chunk, Scale, transform, MapMaterial));
                }
            }
        }

        // remove chunks that are invisible since this update
        _chunks = _chunks.Where(chunk => chunk.Value.IsVisible()).ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
