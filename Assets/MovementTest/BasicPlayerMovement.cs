using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class LeftRightJump : MonoBehaviour
{
    [Header("Locomotion Settings")]
    public float speed = 5.0f;
    public float jumpHeight = 7.0f;
    public float gravity = -20.0f;

    [Header("Animation Setup")]
    public RuntimeAnimatorController defaultController;

    public enum CombatState
    {
        None,
        Attack1,
        Attack2,
        Attack3,
        JumpAttack
    }

    [Header("Combat Timings (Tuned for Mixamo Clips)")]
    // Attack 1: Strike finishes at 0.88s, Combo window until 1.25s, Total duration 1.25s
    [SerializeField] private float attack1StrikeTime = 0.88f;
    [SerializeField] private float attack1TotalTime = 1.25f;

    // Attack 2: Strike finishes at 0.82s, Combo window until 1.20s, Total duration 1.20f
    [SerializeField] private float attack2StrikeTime = 0.82f;
    [SerializeField] private float attack2TotalTime = 1.20f;

    // Attack 3: Finisher strike finishes at 1.35s, Total duration 1.55f
    [SerializeField] private float attack3StrikeTime = 1.35f;
    [SerializeField] private float attack3TotalTime = 1.55f;

    // Jump Attack: Slam speed & landing recovery
    [SerializeField] private float jumpAttackSlamSpeed = -12.0f;
    [SerializeField] private float jumpAttackLandRecovery = 0.25f;

    // Internal State
    private CharacterController characterController;
    private Animator animator;
    private InputMap inputMap;

    private Vector3 velocity = Vector3.zero;
    private CombatState combatState = CombatState.None;
    private float stateTimer = 0f;
    private bool inputBuffered = false;
    private bool jumpAttackLanded = false;
    private float landTimer = 0f;
    private float zPositionLock = 0f;

    // Public getters for combat state
    public bool IsAttacking => combatState != CombatState.None;
    public CombatState CurrentCombatState => combatState;

    void Awake()
    {
        inputMap = new InputMap();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        zPositionLock = transform.position.z;
        EnsureAnimatorController();
    }

    void OnEnable()
    {
        inputMap.Player.Enable();
    }

    void OnDisable()
    {
        inputMap.Player.Disable();
        ResetCombatState();
    }

    public bool EnsureAnimatorController()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null && animator.runtimeAnimatorController == null)
        {
            if (defaultController != null)
            {
                animator.runtimeAnimatorController = defaultController;
            }
            else
            {
                animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("MidasController");
            }
        }

        return animator != null && animator.runtimeAnimatorController != null;
    }

    void Update()
    {
        EnsureAnimatorController();

        float horizontal = inputMap.Player.LeftRight.ReadValue<float>();
        bool attackPressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                          || (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame);
        bool jumpPressed = inputMap.Player.Jump.WasPressedThisFrame();

        UpdateCombat(attackPressed, horizontal, jumpPressed);
        UpdateLocomotion(horizontal, jumpPressed);
        LockZPosition();
    }

    private void UpdateCombat(bool attackPressed, float horizontal, bool jumpPressed)
    {
        bool isGrounded = characterController.isGrounded;

        switch (combatState)
        {
            case CombatState.None:
                if (attackPressed)
                {
                    if (!isGrounded)
                    {
                        StartJumpAttack();
                    }
                    else
                    {
                        // Turn towards horizontal input if moving when attack initiated
                        if (Mathf.Abs(horizontal) > 0.1f)
                        {
                            transform.rotation = Quaternion.Euler(0f, horizontal > 0 ? 90f : -90f, 0f);
                        }
                        StartAttack1();
                    }
                }
                break;

            case CombatState.Attack1:
                stateTimer += Time.deltaTime;

                // Buffer any attack press during Attack 1
                if (attackPressed)
                {
                    inputBuffered = true;
                }

                // Has the strike finished its impact and forward swing?
                if (stateTimer >= attack1StrikeTime)
                {
                    // If player clicked (buffered or right now), chain cleanly to Attack 2
                    if (inputBuffered)
                    {
                        StartAttack2();
                        return;
                    }

                    // Responsive recovery cancel: player pressed move or jump
                    if (Mathf.Abs(horizontal) > 0.1f || jumpPressed)
                    {
                        EndAttackToLocomotion(horizontal);
                        return;
                    }
                }

                // Full duration finished without input
                if (stateTimer >= attack1TotalTime)
                {
                    EndAttackToLocomotion(horizontal);
                }
                break;

            case CombatState.Attack2:
                stateTimer += Time.deltaTime;

                if (attackPressed)
                {
                    inputBuffered = true;
                }

                if (stateTimer >= attack2StrikeTime)
                {
                    if (inputBuffered)
                    {
                        StartAttack3();
                        return;
                    }

                    if (Mathf.Abs(horizontal) > 0.1f || jumpPressed)
                    {
                        EndAttackToLocomotion(horizontal);
                        return;
                    }
                }

                if (stateTimer >= attack2TotalTime)
                {
                    EndAttackToLocomotion(horizontal);
                }
                break;

            case CombatState.Attack3:
                stateTimer += Time.deltaTime;

                // Finisher recovery cancel
                if (stateTimer >= attack3StrikeTime)
                {
                    if (Mathf.Abs(horizontal) > 0.1f || jumpPressed)
                    {
                        EndAttackToLocomotion(horizontal);
                        return;
                    }
                }

                if (stateTimer >= attack3TotalTime)
                {
                    EndAttackToLocomotion(horizontal);
                }
                break;

            case CombatState.JumpAttack:
                stateTimer += Time.deltaTime;

                if (!jumpAttackLanded)
                {
                    velocity.y = jumpAttackSlamSpeed;

                    if (isGrounded && stateTimer > 0.15f)
                    {
                        jumpAttackLanded = true;
                        landTimer = 0f;
                    }
                }
                else
                {
                    landTimer += Time.deltaTime;
                    if (landTimer >= jumpAttackLandRecovery)
                    {
                        EndAttackToLocomotion(horizontal);
                    }
                }
                break;
        }
    }

    private void StartAttack1()
    {
        combatState = CombatState.Attack1;
        stateTimer = 0f;
        inputBuffered = false;

        if (EnsureAnimatorController())
        {
            animator.ResetTrigger("Jump");
            animator.CrossFadeInFixedTime("Attack1", 0.05f);
        }
    }

    private void StartAttack2()
    {
        combatState = CombatState.Attack2;
        stateTimer = 0f;
        inputBuffered = false;

        if (EnsureAnimatorController())
        {
            animator.CrossFadeInFixedTime("Attack2", 0.06f);
        }
    }

    private void StartAttack3()
    {
        combatState = CombatState.Attack3;
        stateTimer = 0f;
        inputBuffered = false;

        if (EnsureAnimatorController())
        {
            animator.CrossFadeInFixedTime("Attack3", 0.06f);
        }
    }

    private void StartJumpAttack()
    {
        combatState = CombatState.JumpAttack;
        stateTimer = 0f;
        inputBuffered = false;
        jumpAttackLanded = false;
        landTimer = 0f;

        if (EnsureAnimatorController())
        {
            animator.ResetTrigger("Jump");
            animator.CrossFadeInFixedTime("JumpAttack", 0.06f);
        }
    }

    private void EndAttackToLocomotion(float horizontal)
    {
        ResetCombatState();

        if (EnsureAnimatorController() && characterController.isGrounded)
        {
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                animator.CrossFadeInFixedTime("Run", 0.10f);
            }
            else
            {
                animator.CrossFadeInFixedTime("Idle", 0.12f);
            }
        }
    }

    private void ResetCombatState()
    {
        combatState = CombatState.None;
        stateTimer = 0f;
        inputBuffered = false;
        jumpAttackLanded = false;
        landTimer = 0f;
    }

    private void UpdateLocomotion(float horizontal, bool jumpPressed)
    {
        bool isGrounded = characterController.isGrounded;
        bool isAttacking = combatState != CombatState.None;

        // 1. Gravity & Jump
        if (isGrounded)
        {
            if (velocity.y < 0)
            {
                velocity.y = -2.0f;
            }

            if (jumpPressed && !isAttacking)
            {
                velocity.y = jumpHeight;
                if (EnsureAnimatorController())
                {
                    animator.SetTrigger("Jump");
                }
            }
        }
        else
        {
            if (combatState != CombatState.JumpAttack)
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        // 2. Horizontal Movement
        if (isAttacking && isGrounded)
        {
            velocity.x = 0f;
        }
        else if (isAttacking && !isGrounded && combatState == CombatState.JumpAttack)
        {
            velocity.x = horizontal * (speed * 0.4f);
        }
        else
        {
            velocity.x = horizontal * speed;
        }

        // 3. Facing Direction
        if (Mathf.Abs(horizontal) > 0.08f && !isAttacking)
        {
            float targetAngle = horizontal > 0 ? 90f : -90f;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }

        // 4. Move Controller
        velocity.z = 0f;
        characterController.Move(velocity * Time.deltaTime);

        // 5. Update Locomotion Animation when not attacking
        if (!isAttacking && EnsureAnimatorController())
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void LockZPosition()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.z - zPositionLock) > 0.001f)
        {
            pos.z = zPositionLock;
            transform.position = pos;
        }
    }
}
