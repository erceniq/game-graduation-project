using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions controls;
    //movement
    public Vector2 moveInput { get; private set; }
    public bool jumpPressed { get; private set; }
    public bool jumpHeld { get; private set; }

    public bool sprintHeld { get; private set; }

    //interaction

    public bool interacted;

    //gun
    public bool gunShooting;


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



        controls.Guns.Enable();
        controls.Guns.Shoot.started += OnGunShotStarted;
        controls.Guns.Shoot.canceled += OnGunShotCanceled;
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


        controls.Guns.Shoot.started -= OnGunShotStarted;
        controls.Guns.Shoot.canceled -= OnGunShotCanceled;
        controls.Guns.Disable();
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



    private void OnGunShotStarted(InputAction.CallbackContext context)
    {
        gunShooting = true;
    }

    private void OnGunShotCanceled(InputAction.CallbackContext context)
    {
        gunShooting = false;
    }
}