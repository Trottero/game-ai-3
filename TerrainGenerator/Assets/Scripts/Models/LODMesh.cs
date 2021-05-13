using DefaultNamespace;
using UnityEngine;

namespace Models
{
    public class LODMesh
    {
        public Mesh Mesh { get; private set; }
        public bool HasRequestedMesh { get; private set; }
        public bool HasMesh { get; private set; }

        private readonly TerrainGenerator _generator;
        private readonly int _lod;
        private readonly System.Action _updateAction;

        public LODMesh(TerrainGenerator generator, int lod, System.Action update)
        {
            _generator = generator;
            _lod = lod;
            _updateAction = update;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            Mesh = meshData.CreateMesh();
            HasMesh = true;
            _updateAction();
        }

        public void RequestMesh(MapData mapData)
        {
            HasRequestedMesh = true;
            _generator.RequestMeshData(mapData, _lod, OnMeshDataReceived);
        }

    }
}