using System;
using DefaultNamespace;
using UnityEngine;

namespace Models
{
    public class TerrainChunk
    {
        private Vector2 _position;
        private GameObject _meshObject;
        private Bounds _bounds;
        private TerrainGenerator _generator;

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        
        private LODInfo[] _detailLevels;
        private LODMesh[] _lodMeshes;

        private MapData _mapData;
        private bool _mapDataReceived;
        private int _previousLOD = -1;

        public TerrainChunk(TerrainGenerator generator, Vector2 coord, LODInfo[] detailLevels, int size, Transform parent, Material material)
        {
            _detailLevels = detailLevels;
            _generator = generator;
            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            Vector3 position3d = new Vector3(_position.x, 0, _position.y);

            _meshObject = new GameObject($"Terrain Chunk ({coord.x}, {coord.y})");
            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshCollider = _meshObject.AddComponent<MeshCollider>();
            _meshRenderer.material = material;

            _meshObject.transform.position = position3d;
            _meshObject.transform.parent = parent;

            SetVisible(false);

            _lodMeshes = new LODMesh[_detailLevels.Length];
            for (int i = 0; i < _detailLevels.Length; i++)
            {
                _lodMeshes[i] = new LODMesh(_generator, _detailLevels[i].lod, Update);
            }

            _generator.RequestMapData(_position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            _mapData = mapData;
            _mapDataReceived = true;

            Texture2D texture2D = TextureGenerator.TextureFromColourMap(mapData.ColourMap,
                TerrainGenerator.MapChunkSize, TerrainGenerator.MapChunkSize);
            _meshRenderer.material.mainTexture = texture2D;
            
            Update();
        }

        public void Update()
        {
            if (!_mapDataReceived) return;
            
            float viewerToEdge = Mathf.Sqrt(_bounds.SqrDistance(InfiniteTerrain.ViewerPosition));
            bool isVisible = viewerToEdge <= InfiniteTerrain.MaxViewingDistance;

            if (isVisible)
            {
                int lodIndex = 0;

                for (int i = 0; i < _detailLevels.Length - 1; i++)
                {
                    if (viewerToEdge > _detailLevels[i].visibleDistanceThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (_previousLOD != lodIndex)
                {
                    LODMesh lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh.HasMesh)
                    {
                        _meshFilter.mesh = lodMesh.Mesh;
                        _meshCollider.sharedMesh = lodMesh.Mesh;
                        _previousLOD = lodIndex;
                    }
                    else if (!lodMesh.HasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_mapData);
                    }
                }

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
}