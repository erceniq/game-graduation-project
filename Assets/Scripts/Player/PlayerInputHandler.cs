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
    //demo
    public bool pressed1;
    public bool pressed2;
    public bool pressed3;
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

        //demo
        controls.Guns._1.performed += On1Performed;
        controls.Guns._1.canceled += On1Canceled;
        controls.Guns._2.performed += On2Performed;
        controls.Guns._2.canceled += On2Canceled;
        controls.Guns._3.performed += On3Performed;
        controls.Guns._3.canceled += On3Canceled;
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


        //demo
        controls.Guns._1.performed -= On1Performed;
        controls.Guns._1.canceled -= On1Canceled;
        controls.Guns._2.performed -= On2Performed;
        controls.Guns._2.canceled -= On2Canceled;
        controls.Guns._3.performed -= On3Performed;
        controls.Guns._3.canceled -= On3Canceled;

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



    //demo

    private void On3Canceled(InputAction.CallbackContext context)
    {
        pressed3 = false;
    }

    private void On3Performed(InputAction.CallbackContext context)
    {
        pressed3 = true;
    }

    private void On2Canceled(InputAction.CallbackContext context)
    {
        pressed2 = false;
    }

    private void On2Performed(InputAction.CallbackContext context)
    {
        pressed2 = true;
    }

    private void On1Canceled(InputAction.CallbackContext context)
    {
        pressed1 = false;
    }

    private void On1Performed(InputAction.CallbackContext context)
    {
        pressed1 = true;
    }


}