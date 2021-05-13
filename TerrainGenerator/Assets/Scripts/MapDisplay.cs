using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

public class MapDisplay : MonoBehaviour
{
    public RawImage hudMinimap;
    public MeshFilter MeshFilter;
    public MeshRenderer MeshRenderer;
    public MeshCollider MeshCollider;
    
    public void DrawMinimap(Texture2D texture)
    {
        hudMinimap.texture = texture;
        // hudMinimap.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        var data = meshData.CreateMesh();
        MeshFilter.sharedMesh = data;
        MeshRenderer.sharedMaterial.mainTexture = texture;
        MeshCollider.sharedMesh = data;
    }
}
