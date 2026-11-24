using System;
using UnityEngine;
using HUMAN.Core;

namespace HUMAN.Combat
{
    /// <summary>
    /// Karakterin veya düşmanın hasar alan gövde/uzuv çarpışma alanı (Hurtbox).
    /// IDamageable ve IParryable arayüzlerini uygular, gelen vuruş ve savuşturma çağrılarını sahibine iletir.
    /// </summary>
    public class Hurtbox : MonoBehaviour, IDamageable, IPoiseDamageable, IParryable
    {
        [Header("Hurtbox Settings")]
        [Tooltip("Bu hurtbox'a gelen hasar için çarpan (Örn: zayıf nokta için 1.5, zırhlı bölge için 0.75)")]
        [SerializeField] private float damageMultiplier = 1.0f;

        [Tooltip("Hurtbox'ın bağlı olduğu ana gövde nesnesi (Belirtilmezse parent aranır)")]
        [SerializeField] private GameObject ownerRoot;

        private IDamageable ownerDamageable;
        private IPoiseDamageable ownerPoise;
        private IParryable ownerParryable;

        public event Action<DamageData> OnHurt;

        public bool IsAlive => ownerDamageable != null ? ownerDamageable.IsAlive : true;
        public GameObject Owner => ownerRoot != null ? ownerRoot : gameObject;

        public void EnsureInitialized()
        {
            if (ownerRoot == null)
            {
                ownerRoot = transform.root.gameObject;
            }

            if (ownerDamageable == null && ownerRoot != null)
            {
                var damageables = ownerRoot.GetComponents<IDamageable>();
                for (int i = 0; i < damageables.Length; i++)
                {
                    if (!ReferenceEquals(damageables[i], this))
                    {
                        ownerDamageable = damageables[i];
                        break;
                    }
                }
            }

            if (ownerPoise == null && ownerRoot != null)
            {
                var poises = ownerRoot.GetComponents<IPoiseDamageable>();
                for (int i = 0; i < poises.Length; i++)
                {
                    if (!ReferenceEquals(poises[i], this))
                    {
                        ownerPoise = poises[i];
                        break;
                    }
                }
            }

            if (ownerParryable == null && ownerRoot != null)
            {
                var parryables = ownerRoot.GetComponents<IParryable>();
                for (int i = 0; i < parryables.Length; i++)
                {
                    if (!ReferenceEquals(parryables[i], this))
                    {
                        ownerParryable = parryables[i];
                        break;
                    }
                }
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        public void TakeDamage(DamageData damage)
        {
            damage.Amount *= damageMultiplier;
            damage.HitPoint = transform.position;

            OnHurt?.Invoke(damage);

            if (ownerDamageable == null || ownerPoise == null)
            {
                EnsureInitialized();
            }

            if (ownerDamageable != null)
            {
                ownerDamageable.TakeDamage(damage);
            }

            if (ownerPoise != null && damage.PoiseDamage > 0f)
            {
                ownerPoise.TakePoiseDamage(damage.PoiseDamage);
            }
        }

        public void TakePoiseDamage(float poiseDamage)
        {
            if (ownerPoise == null)
            {
                EnsureInitialized();
            }

            if (ownerPoise != null)
            {
                ownerPoise.TakePoiseDamage(poiseDamage * damageMultiplier);
            }
        }

        public bool TryParry(DamageData incomingDamage)
        {
            if (ownerParryable == null)
            {
                EnsureInitialized();
            }

            if (ownerParryable != null)
            {
                return ownerParryable.TryParry(incomingDamage);
            }

            return false;
        }
    }
}
