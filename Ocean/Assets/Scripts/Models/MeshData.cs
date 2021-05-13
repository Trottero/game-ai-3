using UnityEngine;

namespace Models
{
    public class MeshData
    {
        public Vector3[] Vertices { get; set; }
        public int[] Triangles { get; set; }
        public Vector2[] UVs { get; set; }

        private int _triangleCurrIndex;

        public MeshData(int meshWidth, int meshHeight, bool useDiscreteVertices)
        {
            if (useDiscreteVertices)
            {
                Vertices = new Vector3[(meshWidth - 1) * (meshHeight - 1) * 6];
                UVs = new Vector2[(meshWidth - 1) * (meshHeight - 1) * 6];
            }
            else
            {
                Vertices = new Vector3[meshWidth * meshHeight];
                UVs = new Vector2[meshWidth * meshHeight];
            }

            Triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];

            _triangleCurrIndex = 0;
        }

        public Mesh CreateMesh()
        {
            var mesh = new Mesh {vertices = Vertices, triangles = Triangles, uv = UVs};
            mesh.RecalculateNormals();
            return mesh;
        }

        public void AddTriangle(int a, int b, int c)
        {
            Triangles[_triangleCurrIndex] = a;
            Triangles[_triangleCurrIndex + 1] = b;
            Triangles[_triangleCurrIndex + 2] = c;
            _triangleCurrIndex += 3;
        }
    }
}
