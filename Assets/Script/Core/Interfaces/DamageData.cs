using UnityEngine;

namespace HUMAN.Core
{
    /// <summary>
    /// Saldırı anında taşınan hasar, duruş (poise) ve savrulma (knockback) veri paketi.
    /// Hem oyuncu hem düşman sistemleri tarafından ortak kullanılır.
    /// </summary>
    [System.Serializable]
    public struct DamageData
    {
        public float Amount;
        public float PoiseDamage;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public GameObject Instigator;
        public bool IsParryable;
        public bool IgnoresInvulnerability;

        public DamageData(float amount, float poiseDamage = 10f, GameObject instigator = null, bool isParryable = true)
        {
            Amount = amount;
            PoiseDamage = poiseDamage;
            HitPoint = Vector3.zero;
            HitDirection = Vector3.zero;
            Instigator = instigator;
            IsParryable = isParryable;
            IgnoresInvulnerability = false;
        }
    }
}
