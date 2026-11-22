using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions controls;

    public Vector2 moveInput { get; private set; }
    public bool jumpPressed { get; private set; }
    public bool jumpHeld { get; private set; }

    public bool sprintHeld { get; private set; }







    public bool interacted;
    private void Awake()
    {
        if (controls == null)
            controls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        if (controls == null)
            controls = new PlayerInputActions();

        controls.Movement.Enable();
        controls.Movement.Jump.started += OnJumpStarted;
        controls.Movement.Jump.canceled += OnJumpCanceled;

        controls.Movement.Sprint.started += OnSprintStarted;
        controls.Movement.Sprint.canceled += OnSprintCanceled;



        controls.Other.Enable();
        controls.Other.Interact.performed += OnInteractPerformed;
        controls.Other.Interact.canceled += OnInteractCanceled;
    }
    

    private void OnDisable()
    {
        controls.Movement.Jump.started -= OnJumpStarted;
        controls.Movement.Jump.canceled -= OnJumpCanceled;
        controls.Movement.Sprint.started -= OnSprintStarted;
        controls.Movement.Sprint.canceled -= OnSprintCanceled;
        controls.Movement.Disable();



        
        controls.Other.Interact.performed -= OnInteractPerformed;
        controls.Other.Interact.canceled -= OnInteractCanceled;
        controls.Other.Disable();
    }

    

    private void Update()
    {
        moveInput = controls.Movement.Move.ReadValue<Vector2>();
        
    }



    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpHeld = true;

    }

    private void OnJumpCanceled(InputAction.CallbackContext context) => jumpHeld = false;

    public void ConsumeJumpPress() => jumpPressed = false;



    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        sprintHeld = true;
    }
    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        sprintHeld = false;
    }


    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        interacted = true;
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        interacted = false;
    }
}