using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SwarmOrchestrator : MonoBehaviour
{
    public int SwarmSize = 10;
    public bool DeleteOnExit = false;
    public bool InitOnCenter = false;
    public bool ForceBounds = true;
    public GameObject Prefab;

    private BoxCollider _boxCollider;
    private MeshCollider _meshCollider;

    private List<GameObject> _particles;

    // Start is called before the first frame update
    void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _particles = Enumerable.Range(0, SwarmSize).Select(CreateParticle).ToList();
    }

    // Update is called once per frame
    void Update()
    {
        if (!ForceBounds)
        {
            return;
        }
        // Check if any particles have gotten out of the box
        // Move them back if they have exceeded the max range
        _particles.ForEach(gobj =>
        {
            var vec = gobj.transform.localPosition;
            if (!_boxCollider.bounds.Contains(vec))
            {
                if (DeleteOnExit)
                {
                    // broken asf, dont actually use this.
                    Destroy(gobj);
                    _particles.Remove(gobj);
                    return;
                }

                var boxSize = _boxCollider.size;
                vec += _boxCollider.bounds.extents;
                vec.x %= boxSize.x;
                vec.y %= boxSize.y;
                vec.z %= boxSize.z;
                if (vec.x < 0) vec.x = boxSize.x - 0.001F;
                if (vec.y < 0) vec.y = boxSize.y - 0.001F;
                if (vec.z < 0) vec.z = boxSize.z - 0.001F;
                vec -= _boxCollider.bounds.extents;
                gobj.transform.localPosition = vec;
            }
        });
    }

    void OnDrawGizmos()
    {
        var bx = GetComponent<BoxCollider>();
        Gizmos.DrawWireCube(bx.center, bx.size);
    }

    GameObject CreateParticle(int particleIndex)
    {

        var ext = _boxCollider.bounds.extents;

        Vector3 randompos = Vector3.zero;
        if (!InitOnCenter)
        {
            // Random starting point
            randompos = new Vector3(Random.Range(-ext.x, ext.x), Random.Range(-ext.y, ext.y), Random.Range(-ext.z, ext.z));
        }

        randompos += transform.position;

        // Give it a random direction
        var randomrot = new Vector3(Random.Range(-ext.x, ext.x), Random.Range(-ext.y, ext.y), Random.Range(-ext.z, _boxCollider.bounds.extents.z));
        var rotQuat = Quaternion.LookRotation(randomrot);

        var gameObject = (GameObject)Instantiate(Prefab, randompos, rotQuat.normalized, transform);
        gameObject.name = $"particle_{particleIndex}";

        return gameObject;
    }
}
