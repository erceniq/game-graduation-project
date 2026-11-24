using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using HUMAN.Core;
using HUMAN.Enemy;

namespace HUMAN.Enemy.Testing
{
    /// <summary>
    /// Ekip arkadaşlarının geliştirdiği Player sistemine dokunmadan,
    /// Düşman AI ve Combat sistemlerini izole test edebilmek için hafif 'Kukla Oyuncu' kontrolcüsü.
    /// - WASD: Hareket
    /// - Sol Tık: Düşmana saldırı (DamageData iletir)
    /// - Sağ Tık: Parry penceresini açar (Düşman saldırırsa savuşturur ve düşmanı Stagger eder)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DummyPlayerTester : MonoBehaviour, IDamageable, IParryable
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float rotationSpeed = 10.0f;

        [Header("Combat - Attack (Sol Tık)")]
        [Tooltip("Oyuncunun saldırı hasarı")]
        [SerializeField] private float attackDamage = 25.0f;
        [Tooltip("Oyuncunun vuruş başına verdiği poise (duruş) hasarı")]
        [SerializeField] private float poiseDamage = 25.0f;
        [Tooltip("Vuruş menzili yarıçapı")]
        [SerializeField] private float attackRange = 1.8f;
        [Tooltip("Vuruş katmanları")]
        [SerializeField] private LayerMask attackLayers = ~0;

        [Header("Combat - Parry (Sağ Tık)")]
        [Tooltip("Parry penceresinin açık kaldığı süre (saniye)")]
        [SerializeField] private float parryWindowDuration = 0.35f;

        [Header("Health")]
        [SerializeField] private float maxHealth = 100.0f;

        private CharacterController controller;
        private float currentHealth;
        private bool isParryActive = false;
        private float parryTimer = 0.0f;
        private Renderer playerRenderer;
        private Material playerMaterial;
        private Color originalColor;

        public bool IsAlive => currentHealth > 0f;
        public bool IsParryActive => isParryActive;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentHealth = maxHealth;

            playerRenderer = GetComponentInChildren<Renderer>();
            if (playerRenderer != null)
            {
                playerMaterial = playerRenderer.material;
                originalColor = playerMaterial.color;
            }

            // Düşmanın otomatik hedef olarak bulabilmesi için Player tag'i ver
            if (!gameObject.CompareTag("Player"))
            {
                try { gameObject.tag = "Player"; } catch { }
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleParryTimer();
            HandleInput();
        }

        private void HandleMovement()
        {
            Vector3 inputDir = GetMovementInput();

            if (inputDir.sqrMagnitude > 0.001f)
            {
                Vector3 move = inputDir.normalized * (moveSpeed * Time.deltaTime);
                move.y = -9.81f * Time.deltaTime; // Yerçekimi
                controller.Move(move);

                Quaternion targetRot = Quaternion.LookRotation(inputDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
            else
            {
                controller.Move(new Vector3(0f, -9.81f * Time.deltaTime, 0f));
            }
        }

        private Vector3 GetMovementInput()
        {
            float h = 0f;
            float v = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            }
#else
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
#endif
            return new Vector3(h, 0f, v);
        }

        private void HandleInput()
        {
            bool attackPressed = false;
            bool parryPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                attackPressed = Mouse.current.leftButton.wasPressedThisFrame;
                parryPressed = Mouse.current.rightButton.wasPressedThisFrame;
            }
#else
            attackPressed = Input.GetMouseButtonDown(0);
            parryPressed = Input.GetMouseButtonDown(1);
#endif

            if (attackPressed)
            {
                PerformAttack();
            }

            if (parryPressed && !isParryActive)
            {
                PerformParry();
            }
        }

        private void PerformAttack()
        {
            Vector3 attackPoint = transform.position + transform.forward * 1.0f + Vector3.up * 0.5f;
            Collider[] hits = Physics.OverlapSphere(attackPoint, attackRange, attackLayers);

            bool hitAny = false;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit.transform.IsChildOf(transform)) continue;

                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DamageData dmg = new DamageData
                    {
                        Amount = attackDamage,
                        PoiseDamage = poiseDamage,
                        HitPoint = hit.ClosestPoint(attackPoint),
                        HitDirection = transform.forward,
                        Instigator = gameObject,
                        IsParryable = false
                    };

                    damageable.TakeDamage(dmg);
                    hitAny = true;
                    Debug.Log($"<color=yellow>[DummyPlayer] Saldırı başarılı: {hit.name} nesnesine {attackDamage} hasar, {poiseDamage} poise hasarı verildi!</color>");
                }
            }

            if (!hitAny)
            {
                Debug.Log("[DummyPlayer] Saldırı yapıldı ancak menzilde hedef yok.");
            }
        }

        private void PerformParry()
        {
            isParryActive = true;
            parryTimer = parryWindowDuration;

            if (playerMaterial != null)
            {
                playerMaterial.color = Color.yellow; // Parry açıkken sarı yanar
            }

            Debug.Log($"<color=cyan>[DummyPlayer] Parry penceresi açıldı! ({parryWindowDuration}s süreyle gelen saldırıları savuşturur)</color>");
        }

        private void HandleParryTimer()
        {
            if (!isParryActive) return;

            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0f)
            {
                isParryActive = false;
                if (playerMaterial != null)
                {
                    playerMaterial.color = originalColor;
                }
            }
        }

        #region INTERFACES IMPLEMENTATION
        public bool TryParry(DamageData incomingDamage)
        {
            if (isParryActive && incomingDamage.IsParryable)
            {
                Debug.Log("<color=green><b>[DummyPlayer] MÜKEMMEL PARRY! Gelen darbe savuşturuldu!</b></color>");

                // Saldıran düşmanı anında sersemlet (Stagger)
                if (incomingDamage.Instigator != null)
                {
                    var enemy = incomingDamage.Instigator.GetComponent<EnemyBrain>();
                    if (enemy != null)
                    {
                        enemy.TriggerParried();
                    }
                }

                // Başarılı parry sonrası parry durumunu kapat
                isParryActive = false;
                if (playerMaterial != null) playerMaterial.color = Color.green;
                return true;
            }

            return false;
        }

        public void TakeDamage(DamageData damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            Debug.Log($"<color=red>[DummyPlayer] Hasar alındı! Miktar: {damage.Amount}, Kalan Can: {currentHealth}/{maxHealth}</color>");

            if (currentHealth <= 0f)
            {
                Debug.LogWarning("[DummyPlayer] Oyuncu öldü!");
            }
        }
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 attackPoint = transform.position + transform.forward * 1.0f + Vector3.up * 0.5f;
            Gizmos.DrawWireSphere(attackPoint, attackRange);
        }
#endif
    }
}
