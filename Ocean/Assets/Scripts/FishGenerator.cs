using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FishGenerator : MonoBehaviour
{
    // Object to spawn
    public GameObject Prefab;

    public MeshCollider Collider;


    private static GameObject _prefab;
    private static Queue<Vector3> _fishQueue = new Queue<Vector3>();
    private List<GameObject> _fishList = new List<GameObject>();
    private int fishCounter = 0;

    // Start is called before the first frame update
    void Start()
    {
        _prefab = Prefab;
    }

    // Update is called once per frame
    void Update()
    {
        lock (_fishQueue)
        {
            while (_fishQueue.Any())
            {
                var pos = _fishQueue.Dequeue();
                var gameObject = (GameObject)Instantiate(_prefab, pos, Random.rotation);
                gameObject.name = $"Fish {fishCounter}";
                fishCounter++;
                _fishList.Add(gameObject);
            }
        }
        var neighboursDirty = false;
        // destroy stragglers
        for (int i = 0; i < _fishList.Count; i++)
        {
            var fish = _fishList[i];
            float viewerToFish = Vector2.Distance(new Vector2(fish.transform.position.x, fish.transform.position.z), InfiniteTerrain.ViewerPosition);
            if (viewerToFish > InfiniteTerrain.MaxViewingDistance + 20f)
            {
                Destroy(fish);
                _fishList.Remove(fish);
                neighboursDirty = true;
            }
        }

        // If any neightbours have been deleted
        if (neighboursDirty)
        {
            Particle.neighbours = _fishList.ToArray();
        }
    }

    // Creates a fish and adds it to the known fish list.
    public static void queueFishCreation(Vector3 worldPos)
    {
        lock (_fishQueue)
        {
            _fishQueue.Enqueue(worldPos);
        }
    }
}
