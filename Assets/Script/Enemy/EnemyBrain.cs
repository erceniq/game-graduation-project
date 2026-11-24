using System;
using UnityEngine;
using HUMAN.Core;
using HUMAN.Combat;
using HUMAN.Enemy.StateMachine;
using HUMAN.Enemy.StateMachine.States;

namespace HUMAN.Enemy
{
    /// <summary>
    /// Düşmanın merkezi koordinasyon ve karar bileşeni (Context).
    /// FSM durumlarını yönetir, can ve duruş (poise) mekaniklerini kontrol eder.
    /// </summary>
    [SelectionBase]
    public class EnemyBrain : MonoBehaviour, IDamageable, IPoiseDamageable
    {
        [Header("Health & Poise")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private PoiseSystem poiseSystem;

        [Header("Perception & Detection")]
        [Tooltip("Düşmanın hedefi (Genellikle Oyuncu)")]
        [SerializeField] private Transform target;
        [Tooltip("Oyuncuyu algılama yarıçapı")]
        [SerializeField] private float detectionRadius = 10f;
        [Tooltip("Saldırı mesafesi")]
        [SerializeField] private float attackRadius = 2.0f;
        [Tooltip("Hedefi tamamen kaybetme mesafesi")]
        [SerializeField] private float loseTargetRadius = 15f;

        [Header("Movement Settings")]
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float patrolSpeed = 1.8f;
        [Tooltip("Devriye noktaları (Opsiyonel)")]
        [SerializeField] private Transform[] patrolWaypoints;

        [Header("Combat Timers (Soulslike Game-Feel)")]
        [Tooltip("Duruş kırıldığında (Poise Broken) veya parry edildiğinde sersem kalma süresi")]
        [SerializeField] private float staggerDuration = 2.5f;

        [Header("Combo Attack Settings")]
        [Tooltip("Düşmanın oyuncu menzilde kaldığı sürece yapabileceği maksimum ardışık vuruş sayısı (1-3)")]
        [SerializeField] private int maxComboHits = 3;

        [Header("Combo 1 Timings (Attack 1 - Right Hook)")]
        [Tooltip("Yumruk ileri fırlayana kadar hazırlık süresi (Mixamo attack1 vuruş karesiyle senkronize)")]
        [SerializeField] private float combo1Telegraph = 0.60f;
        [Tooltip("Yumruğun en ileri uzandığı ve hasar verdiği pencere süresi")]
        [SerializeField] private float combo1Active = 0.20f;
        [Tooltip("Yumruğun geri çekilme toparlanma süresi")]
        [SerializeField] private float combo1Recovery = 0.45f;

        [Header("Combo 2 Timings (Attack 2 - Cross Strike)")]
        [SerializeField] private float combo2Telegraph = 0.50f;
        [SerializeField] private float combo2Active = 0.20f;
        [SerializeField] private float combo2Recovery = 0.40f;

        [Header("Combo 3 Timings (Attack 3 - Heavy Finisher)")]
        [SerializeField] private float combo3Telegraph = 0.70f;
        [SerializeField] private float combo3Active = 0.28f;
        [SerializeField] private float combo3Recovery = 0.60f;

        [Header("Combat Components")]
        [SerializeField] private Hitbox weaponHitbox;
        [SerializeField] private Hurtbox hurtbox;
        [SerializeField] private EnemyRagdollController ragdollController;

        // Çalışma zamanı durumları
        private float currentHealth;
        private bool isAlive = true;
        private Rigidbody rb;
        private float zPositionLock;
        public Vector3 LastHitDirection { get; private set; } = Vector3.left;

        // FSM
        public EnemyStateMachine StateMachine { get; private set; }
        public EnemyIdleState IdleState { get; private set; }
        public EnemyPatrolState PatrolState { get; private set; }
        public EnemyChaseState ChaseState { get; private set; }
        public EnemyAttackState AttackState { get; private set; }
        public EnemyStaggerState StaggerState { get; private set; }
        public EnemyDeathState DeathState { get; private set; }

        // Özellikler (Getters)
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => isAlive;
        public Transform Target => target;
        public float DetectionRadius => detectionRadius;
        public float AttackRadius => attackRadius;
        public float LoseTargetRadius => loseTargetRadius;
        public float ChaseSpeed => chaseSpeed;
        public float PatrolSpeed => patrolSpeed;
        public float TelegraphDuration => combo1Telegraph;
        public float AttackActiveDuration => combo1Active;
        public float AttackRecoveryDuration => combo1Recovery;
        public float StaggerDuration => staggerDuration;
        public int MaxComboHits => maxComboHits;
        public Transform[] PatrolWaypoints => patrolWaypoints;
        public Hitbox WeaponHitbox => weaponHitbox;
        public EnemyRagdollController RagdollController => ragdollController;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action<bool> OnAttackActive;
        public event Action<int> OnAttackComboStep;

        public void NotifyAttackActive(bool isActive) => OnAttackActive?.Invoke(isActive);
        public void NotifyAttackComboStep(int step) => OnAttackComboStep?.Invoke(step);

        public void GetComboTimers(int step, out float telegraph, out float active, out float recovery, out float damage, out float poiseDamage)
        {
            switch (step)
            {
                case 2:
                    telegraph = combo2Telegraph;
                    active = combo2Active;
                    recovery = combo2Recovery;
                    damage = 20f;
                    poiseDamage = 20f;
                    break;
                case 3:
                    telegraph = combo3Telegraph;
                    active = combo3Active;
                    recovery = combo3Recovery;
                    damage = 30f;
                    poiseDamage = 35f;
                    break;
                default: // step 1
                    telegraph = combo1Telegraph;
                    active = combo1Active;
                    recovery = combo1Recovery;
                    damage = 15f;
                    poiseDamage = 15f;
                    break;
            }
        }

        public void EnsureInitialized()
        {
            if (StateMachine != null) return;

            currentHealth = maxHealth;
            isAlive = true;
            rb = GetComponent<Rigidbody>();
            zPositionLock = transform.position.z;

            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.FreezePositionZ |
                                 RigidbodyConstraints.FreezeRotationX |
                                 RigidbodyConstraints.FreezeRotationY |
                                 RigidbodyConstraints.FreezeRotationZ;
            }

            if (poiseSystem == null)
            {
                poiseSystem = GetComponent<PoiseSystem>();
            }

            if (poiseSystem != null)
            {
                poiseSystem.EnsureInitialized();
                poiseSystem.OnPoiseBroken -= HandlePoiseBroken;
                poiseSystem.OnPoiseBroken += HandlePoiseBroken;
            }

            if (ragdollController == null)
            {
                ragdollController = GetComponent<EnemyRagdollController>();
            }

            StateMachine = new EnemyStateMachine();
            IdleState = new EnemyIdleState(this, StateMachine);
            PatrolState = new EnemyPatrolState(this, StateMachine);
            ChaseState = new EnemyChaseState(this, StateMachine);
            AttackState = new EnemyAttackState(this, StateMachine);
            StaggerState = new EnemyStaggerState(this, StateMachine);
            DeathState = new EnemyDeathState(this, StateMachine);

            StateMachine.ChangeState(patrolWaypoints != null && patrolWaypoints.Length > 0 ? PatrolState : IdleState);
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (poiseSystem == null)
            {
                poiseSystem = GetComponent<PoiseSystem>();
            }

            if (poiseSystem != null)
            {
                poiseSystem.EnsureInitialized();
                poiseSystem.OnPoiseBroken -= HandlePoiseBroken;
                poiseSystem.OnPoiseBroken += HandlePoiseBroken;
            }

            if (poiseSystem != null)
            {
                poiseSystem.OnPoiseBroken -= HandlePoiseBroken;
                poiseSystem.OnPoiseBroken += HandlePoiseBroken;
            }
        }

        private void OnDisable()
        {
            if (poiseSystem != null)
            {
                poiseSystem.OnPoiseBroken -= HandlePoiseBroken;
            }
        }

        private void Start()
        {
            // Hedef otomatik atanmamışsa Player etiketli nesneyi bul
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            StateMachine.ChangeState(patrolWaypoints != null && patrolWaypoints.Length > 0 ? PatrolState : IdleState);
        }

        private void Update()
        {
            if (!isAlive) return;
            StateMachine.Tick(Time.deltaTime);
            LockZPosition();
        }

        private void FixedUpdate()
        {
            if (!isAlive) return;
            StateMachine.FixedTick(Time.fixedDeltaTime);
            LockZPosition();
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

        #region 2.5D MOVEMENT & DISTANCE HELPERS
        /// <summary>
        /// 2.5D Düzleminde (yalnızca X ve Y eksenlerinde) hedefe olan 2 boyutlu mesafeyi döner.
        /// Z derinlik ekseni hesaplamaya dahil edilmez.
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (target == null) return float.MaxValue;
            Vector2 myPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 targetPos2D = new Vector2(target.position.x, target.position.y);
            return Vector2.Distance(myPos2D, targetPos2D);
        }

        /// <summary>
        /// Yalnızca yatay (X) eksendeki mesafeyi döner.
        /// </summary>
        public float GetHorizontalDistanceToTarget()
        {
            if (target == null) return float.MaxValue;
            return Mathf.Abs(target.position.x - transform.position.x);
        }

        /// <summary>
        /// 2.5D Düzleminde yalnızca X ekseni boyunca hedefe doğru hareket eder.
        /// Z ekseni kilitli kalır ve yüz yalnızca Sağa (+X) veya Sola (-X) döner.
        /// </summary>
        public void MoveTowards(Vector3 destination, float speed)
        {
            float deltaX = destination.x - transform.position.x;

            if (Mathf.Abs(deltaX) > 0.05f)
            {
                // 2.5D Yüz Dönüşü: Sağa (+90 Y) veya Sola (-90 Y) bakış
                float targetAngle = deltaX > 0 ? 90f : -90f;
                transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);

                float moveDir = deltaX > 0 ? 1f : -1f;

                if (rb != null && !rb.isKinematic)
                {
                    Vector3 moveVelocity = new Vector3(moveDir * speed, rb.linearVelocity.y, 0f);
                    rb.linearVelocity = moveVelocity;
                }
                else
                {
                    float newX = Mathf.MoveTowards(transform.position.x, destination.x, speed * Time.deltaTime);
                    transform.position = new Vector3(newX, transform.position.y, zPositionLock);
                }
            }
            else
            {
                StopMovement();
            }

            LockZPosition();
        }

        public void StopMovement()
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }

        /// <summary>
        /// 2.5D Düzleminde hedef neredeyse (+X veya -X) yalnızca oraya bakar.
        /// </summary>
        public void LookAtTarget()
        {
            if (target == null) return;
            float deltaX = target.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) > 0.05f)
            {
                float targetAngle = deltaX > 0 ? 90f : -90f;
                transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
            }
        }
        #endregion

        #region COMBAT & DAMAGE
        public void TakeDamage(DamageData damage)
        {
            if (!isAlive) return;

            if (damage.HitDirection != Vector3.zero)
            {
                LastHitDirection = damage.HitDirection;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void TakePoiseDamage(float poiseDamage)
        {
            if (poiseSystem != null)
            {
                poiseSystem.TakePoiseDamage(poiseDamage);
            }
        }

        private void HandlePoiseBroken()
        {
            if (!isAlive) return;
            // Duruş kırıldığında hemen Stagger durumuna geç
            StateMachine.ChangeState(StaggerState);
        }

        /// <summary>
        /// Düşmanın saldırısı oyuncu tarafından parry edildiğinde çağrılır.
        /// </summary>
        public void TriggerParried()
        {
            if (!isAlive) return;
            StateMachine.ChangeState(StaggerState);
        }

        private void Die()
        {
            if (!isAlive) return;
            isAlive = false;

            if (weaponHitbox != null) weaponHitbox.DeactivateHitbox();
            if (hurtbox != null) hurtbox.gameObject.SetActive(false);

            StopMovement();
            OnDeath?.Invoke();
            StateMachine.ChangeState(DeathState);
        }
        #endregion

        public void SetTarget(Transform newTarget) => target = newTarget;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);

            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, loseTargetRadius);
        }
#endif
    }
}
