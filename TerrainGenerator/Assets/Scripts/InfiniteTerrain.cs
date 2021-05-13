using System.Collections.Generic;
using System.Linq;
using Models;
using UnityEngine;

namespace DefaultNamespace
{
    public class InfiniteTerrain : MonoBehaviour
    {
        private const float ViewerMoveThresholdForChunkUpdate = 25f;
        private const float SquaredViewerMoveThresholdForChunkUpdate =
            ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

        public LODInfo[] detailLevels;
        public static float MaxViewingDistance;
        
        public Transform Viewer;
        public Material MapMaterial;

        private int _chunkSize;
        private int _numberOfChunksVisible;
        private TerrainGenerator _generator;

        public static Vector2 ViewerPosition;
        private Vector2 _previousViewerPosition;
        
        private Dictionary<Vector2, TerrainChunk> _chunks = new Dictionary<Vector2, TerrainChunk>();
        public static List<TerrainChunk> VisibleChunks = new List<TerrainChunk>();

        void Start()
        {
            MaxViewingDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold; 
            
            _generator = FindObjectOfType<TerrainGenerator>();
            _chunkSize = TerrainGenerator.MapChunkSize - 1;
            _numberOfChunksVisible = Mathf.RoundToInt(MaxViewingDistance / _chunkSize);

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
            VisibleChunks.ForEach(x => x.SetVisible(false));
            VisibleChunks.Clear();
            
            int x = Mathf.RoundToInt(ViewerPosition.x / _chunkSize);
            int y = Mathf.RoundToInt(ViewerPosition.y / _chunkSize);

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
                        _chunks.Add(chunk, new TerrainChunk(_generator, chunk, detailLevels, _chunkSize, transform, MapMaterial));
                    }
                }
            }
        }
    }
}