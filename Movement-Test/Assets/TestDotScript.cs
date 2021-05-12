using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestDotScript : MonoBehaviour
{
    // Start is called before the first frame update
    public float turnRadius = 0.1F;

    private GameObject[] _others;
    void Start()
    {
        _others = GameObject.FindGameObjectsWithTag("EvasionTestTag").Where(x => x.name != name).ToArray();
    }

    // Update is called once per framew
    void Update()
    {
        Debug.DrawRay(transform.position, transform.forward * 2, Color.red, 0.0f);

        foreach (var obj in _others)
        {
            EvadeIfRequired(obj);
        }
    }

    void EvadeIfRequired(GameObject nearMiss)
    {
        var distance = Vector3.Distance(transform.position, nearMiss.transform.position);
        Debug.Log(distance);
        if (distance < 0.5f)
        {
            var vect = nearMiss.transform.position - transform.position;
            vect = vect * -1;
            var qt = Quaternion.LookRotation(vect);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, qt, turnRadius);
        }
    }
}
