using UnityEngine;
using UnityEngine.InputSystem;

public class LeftRightJump : MonoBehaviour
{
    private float gravity = -9.81f;
    private float jump = 15.00f;
    private float speed = 5.00f;
    private Vector3 velocity = Vector3.zero;
    private InputMap inputMap;
    private CharacterController characterController;


    void Awake()
    {
        inputMap = new InputMap();
        characterController = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        inputMap.Player.Enable();
    }

    void Update()
    {
        if (!characterController.isGrounded && velocity.y >= gravity)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (inputMap.Player.Jump.WasPressedThisFrame() && characterController.isGrounded)
        {
            velocity.y = jump;
        }

        velocity.x = inputMap.Player.LeftRight.ReadValue<float>() * speed;

        // Always apply the velovcity
        characterController.Move(velocity * Time.deltaTime);
    }
}
