using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCharacterController : MonoBehaviour
{
    public float speed = 7.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public Transform playerCameraParent;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 60.0f;

    public float movingAnimationSpeed = 1.0f;
    public float boostAnimationMultiplier = 1.5f;

    public float boostSpeedMultiplier = 1.5f;

    CharacterController characterController;
    Animator animator;
    Vector3 moveDirection = Vector3.zero;
    Vector2 rotation = Vector2.zero;

    [HideInInspector]
    public bool canMove = true;

    private bool boosting = false;


    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        rotation.y = transform.eulerAngles.y;
    }

    void Update()
    {


        // Wait untill the key has been up with boost down.
        if (boosting && Input.GetKeyUp(KeyCode.LeftShift))
        {
            boosting = false;
        }

        if (Input.GetButton("Vertical"))
        {
            if (boosting || Input.GetKeyDown(KeyCode.LeftShift))
            {
                boosting = true;
                animator.speed = movingAnimationSpeed * boostAnimationMultiplier;
            }
            else
            {
                animator.speed = movingAnimationSpeed;
            }
        }
        else
        {
            animator.speed = 1.0f;
        }


        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        float curSpeedX = canMove ? speed * Input.GetAxis("Vertical") : 0;
        curSpeedX = boosting ? curSpeedX * boostSpeedMultiplier : curSpeedX;

        float curSpeedY = canMove ? speed * Input.GetAxis("Horizontal") : 0;

        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        // moveDirection.y -= gravity * Time.deltaTime;

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove)
        {
            rotation.y += Input.GetAxis("Mouse X") * lookSpeed;
            rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotation.x = Mathf.Clamp(rotation.x, -lookXLimit, lookXLimit);

            // playerCameraParent.localRotation = Quaternion.Euler(rotation.x, 0, 0);
            playerCameraParent.localRotation = Quaternion.Euler(0, 0, 0);
            transform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0);
        }
    }
}
