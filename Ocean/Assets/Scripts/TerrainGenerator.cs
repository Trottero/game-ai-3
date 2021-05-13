using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Models;
using UnityEditor.AssetImporters;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int seed;

    private Queue<QueuedAction<MeshData>> _meshDataActionQueue = new Queue<QueuedAction<MeshData>>();

    private void Update()
    {
        lock (_meshDataActionQueue)
        {
            while (_meshDataActionQueue.Any())
            {
                var queuedAction = _meshDataActionQueue.Dequeue();
                queuedAction.Action(queuedAction.Payload);
            }
        }
    }

    public void RequestMeshData(Vector2 pos, Action<MeshData> callback)
    {
        void ThreadStart()
        {
            GenerateMeshData(pos, callback);
        }

        new Thread(ThreadStart).Start();
    }

    private void GenerateMeshData(Vector2 pos, Action<MeshData> callback)
    {
        // TODO: generate epic marching cubes
        var meshData = new MeshData(10, 10, false);

        lock (_meshDataActionQueue)
        {
            _meshDataActionQueue.Enqueue(new QueuedAction<MeshData> {Action = callback, Payload = meshData});
        }
    }
}
