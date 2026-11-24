using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using HUMAN.Core;
using HUMAN.Combat;

namespace HUMAN.Enemy.Testing
{
    /// <summary>
    /// 2.5D Düşman AI ve Kombat sistemini sahnede doğrudan test edebilmek için
    /// 'Player' nesnesi üzerinde çalışan ve 'LeftRightJump' ile tam senkronize çalışan köprü bileşeni.
    /// - Sol Tık / J: LeftRightJump animasyonlu kombo vuruşlarını tetikler, vuruş ânında senkronize hasar uygular.
    /// - Sağ Tık / K: 0.35s süreli savuşturma (Parry) penceresi açar ve oyuncuyu savunma duruşuna geçirir.
    /// - 2.5D Düzlemi: Saldırı alanı ve yön hesaplaması yalnızca X ve Y eksenlerinde çalışır.
    /// </summary>
    public class PlayerCombatTestBridge : MonoBehaviour, IDamageable, IParryable
    {
        [Header("Player Combat Stats")]
        [Tooltip("Oyuncunun temel saldırı hasarı")]
        [SerializeField] private float attackDamage = 25f;

        [Tooltip("Oyuncunun vuruş başına verdiği duruş (poise) hasarı")]
        [SerializeField] private float poiseDamage = 25f;

        [Tooltip("Saldırı menzili yarıçapı")]
        [SerializeField] private float attackRange = 1.6f;

        [Tooltip("Saldırının etkileyeceği katmanlar")]
        [SerializeField] private LayerMask attackLayers = ~0;

        [Header("Parry Settings")]
        [Tooltip("Parry penceresinin açık kalma süresi (saniye)")]
        [SerializeField] private float parryWindowDuration = 0.35f;

        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;

        private float currentHealth;
        private bool isParryActive = false;
        private float parryTimer = 0f;
        private LeftRightJump leftRightJump;

        public bool IsAlive
        {
            get
            {
                if (currentHealth <= 0f && maxHealth > 0f) EnsureInitialized();
                return currentHealth > 0f;
            }
        }
        public bool IsParryActive => isParryActive;
        public float CurrentHealth
        {
            get
            {
                if (currentHealth <= 0f && maxHealth > 0f) EnsureInitialized();
                return currentHealth;
            }
        }

        public event Action<float, float> OnHealthChanged;

        public void EnsureInitialized()
        {
            if (currentHealth <= 0f && maxHealth > 0f)
            {
                currentHealth = maxHealth;
            }

            if (leftRightJump == null)
            {
                leftRightJump = GetComponent<LeftRightJump>();
            }

            // Düşmanın Player etiketini algılayabilmesini garantiye al
            if (!gameObject.CompareTag("Player"))
            {
                try { gameObject.tag = "Player"; } catch { }
            }
        }

        private void Awake()
        {
            Time.timeScale = 1.0f;
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (leftRightJump != null)
            {
                leftRightJump.OnAttackStrike += HandleAttackStrike;
            }
        }

        private void OnDisable()
        {
            if (leftRightJump != null)
            {
                leftRightJump.OnAttackStrike -= HandleAttackStrike;
            }
        }

        private void Update()
        {
            HandleParryTimer();
            HandleInput();
        }

        private void HandleInput()
        {
            bool parryPressed = false;
            bool attackPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                attackPressed = Mouse.current.leftButton.wasPressedThisFrame;
                parryPressed = Mouse.current.rightButton.wasPressedThisFrame;
            }
            if (Keyboard.current != null)
            {
                if (Keyboard.current.kKey.wasPressedThisFrame) parryPressed = true;
                if (Keyboard.current.jKey.wasPressedThisFrame) attackPressed = true;
            }
#else
            attackPressed = Input.GetMouseButtonDown(0);
            parryPressed = Input.GetMouseButtonDown(1);
#endif

            // Sağ Tık ile savuşturma (Parry) penceresini aç
            // Animation Commitment: Oyuncu bir saldırı animasyonu oynatırken (IsAttacking) parry yapılamaz
            bool isAttacking = leftRightJump != null && leftRightJump.IsAttacking;
            if (parryPressed && !isParryActive && !isAttacking)
            {
                ActivateParry();
            }

            // Eğer LeftRightJump bağlı DEĞİLSE bağımsız test için doğrudan saldırı yap
            if (leftRightJump == null && attackPressed)
            {
                PerformAttack(1.0f, 1.0f, 1);
            }
        }

        /// <summary>
        /// LeftRightJump vuruş ânı tetiklendiğinde (OnAttackStrike) çağrılır.
        /// Kombo aşamasına göre hasar ve sarsma çarpanı uygular.
        /// </summary>
        private void HandleAttackStrike(LeftRightJump.CombatState state, int comboIndex)
        {
            float dmgMultiplier = 1.0f;
            float poiseMultiplier = 1.0f;

            switch (comboIndex)
            {
                case 1:
                    dmgMultiplier = 1.0f;
                    poiseMultiplier = 1.0f;
                    break;
                case 2:
                    dmgMultiplier = 1.25f;
                    poiseMultiplier = 1.2f;
                    break;
                case 3:
                    dmgMultiplier = 1.75f;
                    poiseMultiplier = 1.8f; // Finisher darbesi yüksek poise hasarı verir
                    break;
                case 4: // Jump Attack
                    dmgMultiplier = 1.5f;
                    poiseMultiplier = 2.0f; // Yere çakılma ezici darbe
                    break;
            }

            PerformAttack(dmgMultiplier, poiseMultiplier, comboIndex);
        }

        private void PerformAttack(float damageMultiplier, float poiseMultiplier, int comboStep)
        {
            // 2.5D Bakış yönünü LeftRightJump'tan doğrudan al, yoksa transform rotasyonundan hesapla
            float facingDir = leftRightJump != null 
                ? leftRightJump.FacingDirection 
                : ((transform.eulerAngles.y > 45f && transform.eulerAngles.y < 135f) ? 1f : -1f);

            // Saldırı merkezini oyuncunun önünde ve Z düzlemine kilitli olarak konumlandır
            Vector3 attackCenter = transform.position + new Vector3(facingDir * 1.15f, 1.0f, 0f);
            attackCenter.z = transform.position.z;

            Collider[] hits = Physics.OverlapSphere(attackCenter, attackRange, attackLayers);
            bool hitAny = false;

            // Tek bir savurmada aynı kök nesneye (Hurtbox + Root Collider) çift hasar vermeyi engelleyen filtre
            HashSet<GameObject> damagedRoots = new HashSet<GameObject>();

            float calculatedDmg = attackDamage * damageMultiplier;
            float calculatedPoise = poiseDamage * poiseMultiplier;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit.transform.IsChildOf(transform)) continue;

                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    GameObject rootObj = hit.transform.root.gameObject;
                    if (damagedRoots.Contains(rootObj)) continue;
                    damagedRoots.Add(rootObj);

                    Vector3 contactPoint = hit.ClosestPoint(attackCenter);
                    if (contactPoint == Vector3.zero || float.IsNaN(contactPoint.x))
                    {
                        contactPoint = attackCenter;
                    }

                    DamageData dmg = new DamageData
                    {
                        Amount = calculatedDmg,
                        PoiseDamage = calculatedPoise,
                        HitPoint = contactPoint,
                        HitDirection = new Vector3(facingDir, 0f, 0f),
                        Instigator = gameObject,
                        IsParryable = false
                    };

                    var enemyBrain = hit.GetComponentInParent<EnemyBrain>();
                    bool enemyParrying = enemyBrain != null && enemyBrain.IsParryStanceActive;

                    damageable.TakeDamage(dmg);
                    hitAny = true;

                    // Görsel & Hissiyat Geri Bildirimi: Yalnızca düşman parry yapmadığı durumlarda normal Hit Flash tetikle
                    if (!enemyParrying && CombatFeedbackController.Instance != null)
                    {
                        CombatFeedbackController.Instance.TriggerEnemyHitFeedback(rootObj, contactPoint, dmg.HitDirection);
                    }

                    Debug.Log($"<color=yellow>[PlayerCombatTest] Düşmana vuruldu (Kombo #{comboStep}): {hit.name} -> {calculatedDmg:F1} Hasar, {calculatedPoise:F1} Poise!</color>");
                }
            }

            if (!hitAny)
            {
                Debug.Log($"[PlayerCombatTest] Kombo #{comboStep} savurması yapıldı (Menzilde hedef yok).");
            }
        }

        private void ActivateParry()
        {
            isParryActive = true;
            parryTimer = parryWindowDuration;
            Debug.Log($"<color=cyan>[PlayerCombatTest] PARRY DURAĞI AKTİF! ({parryWindowDuration}s boyunca gelen saldırılar savuşturulur)</color>");
        }

        private void HandleParryTimer()
        {
            if (!isParryActive) return;

            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0f)
            {
                isParryActive = false;
            }
        }

        #region INTERFACES IMPLEMENTATION
        public bool TryParry(DamageData incomingDamage)
        {
            if (isParryActive && incomingDamage.IsParryable)
            {
                Debug.Log("<color=green><b>[PlayerCombatTest] MÜKEMMEL PARRY! Düşmanın saldırısı başarıyla savuşturuldu!</b></color>");

                // 1. Oyun Hissi: Hitstop + Kamera Sarsıntısı + Altın/Cyan Kıvılcım Patlaması + Işık Flaşı
                Vector3 sparkPos = incomingDamage.HitPoint != Vector3.zero 
                    ? incomingDamage.HitPoint 
                    : (transform.position + transform.forward * 1.0f + Vector3.up * 1.2f);
                CombatFeedbackController.Instance.TriggerParryFeedback(sparkPos);

                // 2. Oyuncuya hafif karşılıklı tepme (Parry Recoil Knockback)
                if (leftRightJump != null)
                {
                    float recoilDir = (incomingDamage.Instigator != null)
                        ? Mathf.Sign(transform.position.x - incomingDamage.Instigator.transform.position.x)
                        : -leftRightJump.FacingDirection;
                    leftRightJump.ApplyKnockback(recoilDir, 3.0f);
                }

                // 3. PlayerHUD Entegrasyonu: Fist Energy'ye +2 Enerji ekle ve parlama efekti oynat
                if (PlayerHUD.Instance != null)
                {
                    PlayerHUD.Instance.AddEnergy(2);
                    PlayerHUD.Instance.TriggerParryEnergyPulse();
                }

                // 4. Saldıran düşmanı anında sersemlet (Stagger)
                if (incomingDamage.Instigator != null)
                {
                    var enemy = incomingDamage.Instigator.GetComponent<EnemyBrain>();
                    if (enemy == null)
                    {
                        enemy = incomingDamage.Instigator.GetComponentInParent<EnemyBrain>();
                    }

                    if (enemy != null)
                    {
                        enemy.TriggerParried();
                    }
                }

                isParryActive = false;
                return true;
            }

            return false;
        }

        public void TakeDamage(DamageData damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // PlayerHUD Can Barı Güncellemesi
            if (PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.SetHealth(currentHealth);
            }

            // Geri tepme (Knockback)
            if (leftRightJump != null)
            {
                float pushDir = damage.HitDirection.x != 0f
                    ? Mathf.Sign(damage.HitDirection.x)
                    : (damage.Instigator != null ? Mathf.Sign(transform.position.x - damage.Instigator.transform.position.x) : -leftRightJump.FacingDirection);
                leftRightJump.ApplyKnockback(pushDir, 5.0f);
            }

            // Hasar Geri Bildirimi: Ekran sarsıntısı ve oyuncu hit flash
            CombatFeedbackController.Instance.TriggerHitstop(0.06f, 0.15f);
            CombatFeedbackController.Instance.TriggerCameraShake(0.12f, 0.14f);
            CombatFeedbackController.Instance.TriggerHitFlash(gameObject);

            Debug.Log($"<color=red>[PlayerCombatTest] Hasar alındı! Miktar: {damage.Amount}, Kalan Can: {currentHealth}/{maxHealth}</color>");

            if (currentHealth <= 0f)
            {
                Debug.LogWarning("[PlayerCombatTest] Oyuncu yenildi!");
            }
        }
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isParryActive ? Color.cyan : Color.yellow;
            float facingDir = leftRightJump != null 
                ? leftRightJump.FacingDirection 
                : ((transform.eulerAngles.y > 45f && transform.eulerAngles.y < 135f) ? 1f : -1f);

            Vector3 attackCenter = transform.position + new Vector3(facingDir * 1.15f, 1.0f, 0f);
            attackCenter.z = transform.position.z;
            Gizmos.DrawWireSphere(attackCenter, attackRange);
        }
#endif
    }
}
