using UnityEngine;

namespace Models
{
    public struct MapData
    {
        public float[,] HeightMap { get; set; }
        public Color[] ColourMap { get; set; }
    }
}