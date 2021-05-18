using System.Linq;
using UnityEngine;

namespace Models
{
    public class MeshData
    {
        public Vector3[] Vertices { get; private set; }

        private int _vertexCounter = 0;

        public MeshData(int numberOfVoxels, int VerticalRenderDistance)
        {
            // Maximal number of vertices in a single chunk
            Vertices = new Vector3[numberOfVoxels * numberOfVoxels * numberOfVoxels * 5 * 3 * VerticalRenderDistance];
        }

        public Mesh CreateMesh()
        {
            // Mesh assumes that all vertices are in order
            var meshTriangles = Enumerable.Range(0, _vertexCounter).ToArray();

            // Construct mesh
            var mesh = new Mesh { vertices = Vertices, triangles = meshTriangles };

            mesh.RecalculateNormals();
            mesh.Optimize();

            return mesh;
        }

        public void AddVertex(Vector3 vertex)
        {
            Vertices[_vertexCounter] = vertex;
            _vertexCounter++;
        }
    }
}
