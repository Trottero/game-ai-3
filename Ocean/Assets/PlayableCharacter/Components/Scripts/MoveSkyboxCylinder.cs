using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSkyboxCylinder : MonoBehaviour
{
    public Transform SkyboxCylinder;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var camerapos = transform.position;
        camerapos.y = 0;
        SkyboxCylinder.position = camerapos;
    }
}
