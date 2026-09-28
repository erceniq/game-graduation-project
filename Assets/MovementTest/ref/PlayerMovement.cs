using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public enum MovementState
{
    Grounded,
    Airborne,

}

public class PlayerMovement : MonoBehaviour
{
    [Header("Essentials")]
    PlayerInputHandler inputHandler;
    CharacterController controller;
    public MovementState currentState = MovementState.Grounded;



    [Header("Player Rotation")]
    Vector2 inputToRotate;
    public float rotationSpeed;
    private bool isPivotingOnSpot;

    [Header("Camera")]
    public CinemachineCamera playerCamera;


    [Header("LayerMasks")]
    public LayerMask ground;

    [Header("Move")]
    public float moveSpeedMax = 15f;
    public float airSpeedMax = 7f;
    private float currentMaxSpeed;
    private float currentSmoothTime;
    private Vector3 smoothMoveVelocity;
    private Vector3 movementHorizontal;
    public float smoothTime = 0.15f;
    public float airSmoothTime = 1f;
    private Vector3 finalMovement;

    [Header("Gravity")]
    public float playerVerticalVelocity;
    public float gravitationalAcceleration;
    public float gravityMax;
    public float groundedVerticalVelocity;
    public Vector3 sphereCastOffsetCeiling;
    public float sphereCastHeightCeiling;

    [Header("Jump")]
    public float jumpPower;
    public int jumpRemaining = 2;
    public float jumpPowerModifier = 70f;
    public bool jumpTimerGoing;
    private float jumpBufferTimer;
    private float coyoteTimer;
    private const float JUMP_BUFFER_DURATION = 0.2f;
    private const float COYOTE_TIME_DURATION = 0.15f;
    public bool hitHead;
    public float rayCastDistance = 1f;







    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
    }
    private void FixedUpdate()
    {


        if (inputHandler.jumpPressed)
        {
            inputHandler.ConsumeJumpPress();
            jumpBufferTimer = JUMP_BUFFER_DURATION;
            jumpTimerGoing = true;
        }
        Timers();
        StateHandler();

    }
    private void Timers()
    {
        if(coyoteTimer > 0f)
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }

        if (jumpBufferTimer > 0)
        {
            jumpTimerGoing = true;
            jumpBufferTimer -= Time.fixedDeltaTime;
        }
        else if (jumpBufferTimer <= 0)
        {

            jumpBufferTimer = 0f;
            jumpTimerGoing = false;

        }
    }
    private void StateHandler()
    {
        switch (currentState)
        {
            case MovementState.Grounded:
                currentMaxSpeed = moveSpeedMax;
                currentSmoothTime = smoothTime;

                jumpRemaining = 2;
                playerVerticalVelocity = groundedVerticalVelocity;
                //holding jump button determines the power
                if (inputHandler.jumpHeld && jumpPower < 0.1f)
                {
                    jumpPower += Time.fixedDeltaTime;
                }

                bool releasedEarly = !inputHandler.jumpHeld && jumpPower > 0.001f;
                bool reachedMaxCharge = jumpPower >= 0.1f;

                if (jumpTimerGoing && !inputHandler.jumpHeld && jumpPower < 0.001f)
                {
                    


                    inputHandler.ConsumeJumpPress();
                    jumpRemaining = 1;
                    playerVerticalVelocity = 0.20f * jumpPowerModifier;
                    jumpPower = 0f;
                    currentState = MovementState.Airborne;
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;
                    break;
                }
                else if ((releasedEarly || reachedMaxCharge) && jumpTimerGoing)
                {

                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;
                    inputHandler.ConsumeJumpPress();
                    jumpRemaining = 1;
                    playerVerticalVelocity = (0.10f + jumpPower) * jumpPowerModifier;
                    jumpPower = 0f;
                    currentState = MovementState.Airborne;
                    break;
                }


                HorizontalMovement();
                VerticalMovement();
                if (!controller.isGrounded)
                {
                    if (jumpRemaining == 2)
                    {
                        coyoteTimer = COYOTE_TIME_DURATION;
                    }
                    currentState = MovementState.Airborne;
                }

                break;

            case MovementState.Airborne:
                currentMaxSpeed = airSpeedMax;
                currentSmoothTime = airSmoothTime;

                if(coyoteTimer>=0.03f && jumpTimerGoing)
                {
                    inputHandler.ConsumeJumpPress();
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;

                    jumpRemaining = 1;
                    playerVerticalVelocity = 0.3f * jumpPowerModifier;
                    jumpPower = 0f;
                }
                else if (jumpRemaining > 0 && jumpTimerGoing)
                {

                    inputHandler.ConsumeJumpPress();
                    jumpBufferTimer = 0f;
                    jumpTimerGoing = false;
                    jumpRemaining = 0;
                    playerVerticalVelocity = 0.3f * jumpPowerModifier;
                    jumpPower = 0f;
                }
                HorizontalMovement();
                VerticalMovement();
                if (controller.isGrounded) currentState = MovementState.Grounded;

                break;



                
          
        }
    }

        




    private void HorizontalMovement()
    {
        inputToRotate = inputHandler.moveInput;
        float inputMagnitude = Mathf.Clamp01(inputToRotate.magnitude);
        float angleDifference = 0f;

        if (inputMagnitude > 0.1f && currentState == MovementState.Grounded)
        {
            float inputAngle = Mathf.Atan2(inputToRotate.x, inputToRotate.y) * Mathf.Rad2Deg;
            float cameraAngle = playerCamera.transform.rotation.eulerAngles.y;
            float targetAngle = inputAngle + cameraAngle;

            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

            if (!isPivotingOnSpot && angleDifference > 110f)
            {
                isPivotingOnSpot = true;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        if (isPivotingOnSpot && angleDifference < 15f)
        {
            isPivotingOnSpot = false;
        }

        if (inputMagnitude <= 0.05f)
        {
            isPivotingOnSpot = false;
        }

        Vector3 targetVelocity;


        if (currentState == MovementState.Grounded && isPivotingOnSpot)
        {
            targetVelocity = Vector3.zero;
        }
        else if (currentState == MovementState.Grounded)
        {
            float alignmentModifier = Mathf.Clamp01(1f - (angleDifference / 180f));
            targetVelocity = transform.forward * inputMagnitude * currentMaxSpeed * alignmentModifier;
        }
        else if(currentState == MovementState.Airborne)
        {
            Vector3 vel1 = playerCamera.transform.right * inputHandler.moveInput.x * currentMaxSpeed;
            Vector3 vel2 = playerCamera.transform.forward * inputHandler.moveInput.y * currentMaxSpeed;
            targetVelocity = vel1 + vel2;
        }
        else
        {
            targetVelocity = Vector3.zero;
        }

        movementHorizontal = Vector3.SmoothDamp(movementHorizontal, targetVelocity, ref smoothMoveVelocity, currentSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
    }



    private void VerticalMovement()
    {
        if (currentState == MovementState.Airborne )
        {
            jumpPower = 0f;
            playerVerticalVelocity -= gravitationalAcceleration * Time.fixedDeltaTime;
            playerVerticalVelocity = Mathf.Max(playerVerticalVelocity, gravityMax);


        }
        Vector3 motion = new Vector3(0, playerVerticalVelocity, 0);
        finalMovement = movementHorizontal + motion;
        controller.Move(finalMovement * Time.fixedDeltaTime);
        jumpPower = Mathf.Clamp01(jumpPower);


        HeadHit();

    }


    private void HeadHit()
    {
        Vector3 worldControllerCenter = transform.TransformPoint(controller.center);
        Vector3 castOrigin = worldControllerCenter + sphereCastOffsetCeiling;
        RaycastHit hitCeiling;
        RaycastHit hitCeiling2;
        hitHead = (Physics.SphereCast(castOrigin, controller.radius, transform.up, out hitCeiling, sphereCastHeightCeiling, ground)) 
            || (Physics.Raycast(castOrigin,  transform.up, out hitCeiling2,rayCastDistance , ground));
        if (hitHead) playerVerticalVelocity = groundedVerticalVelocity;
  
    }



   



}