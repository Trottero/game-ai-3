using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float movementSpeed = 10;
    public float turningSpeed = 60;
    private Animator _animator;

    // Start is called before the first frame update
    void Start()
    {
        this._animator = GetComponent<Animator>();
    }

    void Update()
    {
        float strafe = Input.GetAxis("Horizontal");
        float forward = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Mouse X");

        transform.Translate(new Vector3(strafe, 0, forward) * (movementSpeed * Time.deltaTime));
        transform.rotation *= Quaternion.Slerp(
            Quaternion.identity,
            Quaternion.LookRotation(turn < 0 ? Vector3.left : Vector3.right),
            Mathf.Abs(turn) * turningSpeed * Time.deltaTime
        );

        this._animator.SetFloat("VelocityZ", forward);
        this._animator.SetFloat("VelocityX", strafe);
    }
}
