using System.Collections.Generic;
using UnityEngine;
using HUMAN.Core;

namespace HUMAN.Combat
{
    /// <summary>
    /// Saldırı anında hasar veren vuruş alanı (Silah, pençe, mermi alanı).
    /// Belirlenen aktif pencere boyunca temas ettiği Hurtbox'lara DamageData iletir.
    /// Tek bir savurmada aynı hedefe birden fazla hasar vermemek için hedef listesi tutar.
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Temas anında verilecek taban hasar")]
        [SerializeField] private float damageAmount = 20f;

        [Tooltip("Temas anında verilecek duruş (poise) hasarı")]
        [SerializeField] private float poiseDamageAmount = 15f;

        [Tooltip("Bu vuruş parry ile savuşturulabilir mi?")]
        [SerializeField] private bool isParryable = true;

        [Header("Target Filtering")]
        [Tooltip("Saldırıyı başlatan sahip (Kendi kendini vurmayı önler)")]
        [SerializeField] private GameObject ownerRoot;

        [Tooltip("Vurulabilecek katmanlar (Layer Mask)")]
        [SerializeField] private LayerMask targetLayers = ~0;

        private Collider hitboxCollider;
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        private bool isHitboxActive = false;

        public bool IsHitboxActive => isHitboxActive;
        public bool IsParryable => isParryable;

        private void Awake()
        {
            hitboxCollider = GetComponent<Collider>();
            if (hitboxCollider != null)
            {
                hitboxCollider.isTrigger = true;
                hitboxCollider.enabled = false;
            }

            if (ownerRoot == null)
            {
                ownerRoot = transform.root.gameObject;
            }
        }

        public void SetDamage(float damage, float poiseDamage)
        {
            damageAmount = damage;
            poiseDamageAmount = poiseDamage;
        }

        public void ActivateHitbox()
        {
            hitTargets.Clear();
            isHitboxActive = true;
            if (hitboxCollider != null) hitboxCollider.enabled = true;
        }

        public void DeactivateHitbox()
        {
            isHitboxActive = false;
            if (hitboxCollider != null) hitboxCollider.enabled = false;
            hitTargets.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isHitboxActive) return;

            // Katman filtresi
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

            // Kendi kendini vurmayı engelle
            if (ownerRoot != null && other.transform.IsChildOf(ownerRoot.transform)) return;

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && !hitTargets.Contains(damageable))
            {
                hitTargets.Add(damageable);

                // Savuşturma (Parry) kontrolü
                var parryable = other.GetComponent<IParryable>();
                if (parryable == null)
                {
                    parryable = other.GetComponentInParent<IParryable>();
                }
                Vector3 originPos = ownerRoot != null ? ownerRoot.transform.position : transform.position;
                Vector3 hitDirection = (other.transform.position - originPos).normalized;

                DamageData damage = new DamageData
                {
                    Amount = damageAmount,
                    PoiseDamage = poiseDamageAmount,
                    HitPoint = other.ClosestPoint(transform.position),
                    HitDirection = hitDirection,
                    Instigator = ownerRoot,
                    IsParryable = isParryable
                };

                if (parryable != null && isParryable && parryable.TryParry(damage))
                {
                    // Saldırı başarıyla parry edildi, saldırgan sarsılmalıdır
                    OnParried();
                    return;
                }

                damageable.TakeDamage(damage);

                // Temas anında darbe kıvılcımı ve görsel geri bildirim
                if (CombatFeedbackController.Instance != null)
                {
                    CombatFeedbackController.Instance.SpawnImpactSparks(damage.HitPoint, hitDirection);
                }
            }
        }

        /// <summary>
        /// Saldırı parry edildiğinde tetiklenecek geri bildirim (Örn: Düşman Stagger'a geçer).
        /// </summary>
        protected virtual void OnParried()
        {
            DeactivateHitbox();
        }

        public void SetDamage(float amount, float poise = 15f, bool parryable = true)
        {
            damageAmount = amount;
            poiseDamageAmount = poise;
            isParryable = parryable;
        }
    }
}
