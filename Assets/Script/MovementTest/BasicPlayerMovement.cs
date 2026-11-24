using System;
using UnityEngine;
using UnityEngine.InputSystem;
using HUMAN.Enemy.Testing;

[RequireComponent(typeof(CharacterController))]
public class LeftRightJump : MonoBehaviour
{
    [Header("Locomotion Settings")]
    public float speed = 5.0f;
    public float jumpHeight = 7.0f;
    public float gravity = -20.0f;

    [Header("2.5D Constraints")]
    [Tooltip("2.5D düzleminde Z ekseni kilit pozisyonu (Varsayılan: Objenin başlangıç Z pozisyonu)")]
    [SerializeField] private float zPositionLock = 0f;
    [Tooltip("Karakterin baktığı yön: +1 = Sağa (+X), -1 = Sola (-X)")]
    [SerializeField] private float facingDirection = 1f;

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
    // Attack 1: Hit impact at 0.60s (Right hand peak reach), Strike window finishes at 0.85s, Total duration 1.25s
    [SerializeField] private float attack1HitTime = 0.60f;
    [SerializeField] private float attack1StrikeTime = 0.85f;
    [SerializeField] private float attack1TotalTime = 1.25f;

    // Attack 2: Hit impact at 0.50s (Right hand peak reach), Strike window finishes at 0.78s, Total duration 1.15s
    [SerializeField] private float attack2HitTime = 0.50f;
    [SerializeField] private float attack2StrikeTime = 0.78f;
    [SerializeField] private float attack2TotalTime = 1.15f;

    // Attack 3: Hit impact at 0.72s (Finisher strike peak reach), Strike window finishes at 1.15s, Total duration 1.55s
    [SerializeField] private float attack3HitTime = 0.72f;
    [SerializeField] private float attack3StrikeTime = 1.15f;
    [SerializeField] private float attack3TotalTime = 1.55f;

    // Jump Attack: Slam speed & landing recovery
    [SerializeField] private float jumpAttackSlamSpeed = -14.0f;
    [SerializeField] private float jumpAttackLandRecovery = 0.25f;

    [Header("2.5D Player Knockback")]
    [Tooltip("Geri tepme sönümlenme katsayısı")]
    [SerializeField] private float knockbackDecay = 14f;
    private float knockbackVelocityX = 0f;

    public void ApplyKnockback(float directionX, float force)
    {
        float dir = Mathf.Abs(directionX) > 0.01f ? Mathf.Sign(directionX) : -facingDirection;
        knockbackVelocityX = dir * force;
    }

    // Events & Callbacks
    /// <summary>
    /// Saldırı vuruş ânında (Hitbox aktifleşme anı) tetiklenir: (CombatState durum, int komboNo)
    /// </summary>
    public event Action<CombatState, int> OnAttackStrike;

    // Internal State
    private CharacterController characterController;
    private Animator animator;
    private InputMap inputMap;
    private PlayerCombatTestBridge combatBridge;

    private Vector3 velocity = Vector3.zero;
    private CombatState combatState = CombatState.None;
    private float stateTimer = 0f;
    private bool inputBuffered = false;
    private bool hasStruckThisState = false;
    private bool jumpAttackLanded = false;
    private float landTimer = 0f;

    // Public Properties
    public bool IsAttacking => combatState != CombatState.None;
    public bool CanParry => combatState == CombatState.None;
    public CombatState CurrentCombatState => combatState;
    public float FacingDirection => facingDirection;
    public bool IsFacingRight => facingDirection > 0f;
    public float ZPositionLock { get => zPositionLock; set => zPositionLock = value; }

    private void EnsureInputMap()
    {
        if (inputMap == null)
        {
            inputMap = new InputMap();
        }
    }

    void Awake()
    {
        Time.timeScale = 1.0f;
        EnsureInputMap();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        combatBridge = GetComponent<PlayerCombatTestBridge>();

        // 2.5D Z kilit başlangıcı
        zPositionLock = transform.position.z;

        // Mevcut rotasyona göre başlangıç yönünü belirle ve kilitle
        float currentY = transform.eulerAngles.y;
        facingDirection = (currentY > 45f && currentY < 135f) ? 1f : -1f;
        SetFacingDirection(facingDirection);

        EnsureAnimatorController();
    }

    void OnEnable()
    {
        EnsureInputMap();
        inputMap.Player.Enable();
    }

    void OnDisable()
    {
        if (inputMap != null)
        {
            inputMap.Player.Disable();
        }
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

    public void SetFacingDirection(float dir)
    {
        if (Mathf.Abs(dir) < 0.01f) return;
        facingDirection = dir > 0f ? 1f : -1f;
        transform.rotation = Quaternion.Euler(0f, facingDirection > 0f ? 90f : -90f, 0f);
    }

    void Update()
    {
        EnsureInputMap();
        EnsureAnimatorController();

        if (combatBridge == null)
        {
            combatBridge = GetComponent<PlayerCombatTestBridge>();
        }

        bool isParrying = combatBridge != null && combatBridge.IsParryActive;

        float horizontal = inputMap.Player.LeftRight.ReadValue<float>();
        bool attackPressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                          || (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame);
        bool jumpPressed = inputMap.Player.Jump.WasPressedThisFrame();

        // Savuşturma (Parry) esnasında saldırı veya zıplama başlatılmaz
        if (!isParrying)
        {
            UpdateCombat(attackPressed, horizontal, jumpPressed);
        }

        UpdateLocomotion(horizontal, jumpPressed, isParrying);
        LockZPosition();
    }

    void LateUpdate()
    {
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
                        // Saldırıya başlarken yön girdisi varsa o yöne dön
                        if (Mathf.Abs(horizontal) > 0.1f)
                        {
                            SetFacingDirection(horizontal > 0f ? 1f : -1f);
                        }
                        StartAttack1();
                    }
                }
                break;

            case CombatState.Attack1:
                stateTimer += Time.deltaTime;

                // Vuruş ânı kontrolü
                if (!hasStruckThisState && stateTimer >= attack1HitTime)
                {
                    hasStruckThisState = true;
                    OnAttackStrike?.Invoke(CombatState.Attack1, 1);
                }

                if (attackPressed)
                {
                    inputBuffered = true;
                }

                // Darbe tamamlandıktan sonra kombo veya iptal penceresi
                if (stateTimer >= attack1StrikeTime)
                {
                    if (inputBuffered)
                    {
                        if (Mathf.Abs(horizontal) > 0.1f)
                        {
                            SetFacingDirection(horizontal > 0f ? 1f : -1f);
                        }
                        StartAttack2();
                        return;
                    }

                    if (Mathf.Abs(horizontal) > 0.1f || jumpPressed)
                    {
                        EndAttackToLocomotion(horizontal);
                        return;
                    }
                }

                if (stateTimer >= attack1TotalTime)
                {
                    EndAttackToLocomotion(horizontal);
                }
                break;

            case CombatState.Attack2:
                stateTimer += Time.deltaTime;

                if (!hasStruckThisState && stateTimer >= attack2HitTime)
                {
                    hasStruckThisState = true;
                    OnAttackStrike?.Invoke(CombatState.Attack2, 2);
                }

                if (attackPressed)
                {
                    inputBuffered = true;
                }

                if (stateTimer >= attack2StrikeTime)
                {
                    if (inputBuffered)
                    {
                        if (Mathf.Abs(horizontal) > 0.1f)
                        {
                            SetFacingDirection(horizontal > 0f ? 1f : -1f);
                        }
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

                if (!hasStruckThisState && stateTimer >= attack3HitTime)
                {
                    hasStruckThisState = true;
                    OnAttackStrike?.Invoke(CombatState.Attack3, 3);
                }

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

                        // Yere çakılma ânında alan hasarı tetikle
                        if (!hasStruckThisState)
                        {
                            hasStruckThisState = true;
                            OnAttackStrike?.Invoke(CombatState.JumpAttack, 4);
                        }
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
        hasStruckThisState = false;

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
        hasStruckThisState = false;

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
        hasStruckThisState = false;

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
        hasStruckThisState = false;
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
        hasStruckThisState = false;
        jumpAttackLanded = false;
        landTimer = 0f;
    }

    private void UpdateLocomotion(float horizontal, bool jumpPressed, bool isParrying)
    {
        bool isGrounded = characterController.isGrounded;
        bool isAttacking = combatState != CombatState.None;

        // 1. Düşey Yerçekimi ve Zıplama
        if (isGrounded)
        {
            if (velocity.y < 0)
            {
                velocity.y = -2.0f;
            }

            if (jumpPressed && !isAttacking && !isParrying)
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

        // 2. Yatay Hareket (X Ekseni)
        if (isParrying)
        {
            // Savuşturma esnasında ayakları yere sabitle (Brace stance)
            velocity.x = 0f;
        }
        else if (isAttacking && isGrounded)
        {
            // Yerde saldırı sırasında kaymayı önle
            velocity.x = 0f;
        }
        else if (isAttacking && !isGrounded && combatState == CombatState.JumpAttack)
        {
            // Havadan yere iniş saldırısında hafif hava kontrolü
            velocity.x = horizontal * (speed * 0.4f);
        }
        else
        {
            velocity.x = horizontal * speed;
        }

        // Knockback ekle ve eksponansiyel sönümle
        if (Mathf.Abs(knockbackVelocityX) > 0.05f)
        {
            velocity.x += knockbackVelocityX;
            knockbackVelocityX = Mathf.Lerp(knockbackVelocityX, 0f, knockbackDecay * Time.deltaTime);
            if (Mathf.Abs(knockbackVelocityX) < 0.05f) knockbackVelocityX = 0f;
        }

        // 3. 2.5D Baktığı Yön (+X / -X Snapping)
        if (Mathf.Abs(horizontal) > 0.08f && !isAttacking && !isParrying)
        {
            SetFacingDirection(horizontal > 0f ? 1f : -1f);
        }

        // 4. CharacterController Hareketi (Z her zaman 0)
        velocity.z = 0f;
        characterController.Move(velocity * Time.deltaTime);

        // 5. Animasyon Parametreleri
        if (!isAttacking && EnsureAnimatorController())
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void LockZPosition()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.z - zPositionLock) > 0.0005f)
        {
            pos.z = zPositionLock;
            transform.position = pos;
        }
    }
}
