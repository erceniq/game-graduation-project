using System;
using UnityEngine;
using HUMAN.Core;

namespace HUMAN.Combat
{
    /// <summary>
    /// Karakterlerin ve düşmanların duruş (poise/stagger) mekaniğini yöneten modüler bileşen.
    /// Soulslike hissi için poise sıfırlandığında OnPoiseBroken tetiklenir.
    /// </summary>
    public class PoiseSystem : MonoBehaviour, IPoiseDamageable
    {
        [Header("Poise Settings")]
        [Tooltip("Maksimum duruş değeri")]
        [SerializeField] private float maxPoise = 50f;

        [Tooltip("Duruşun saniyedeki yenilenme hızı")]
        [SerializeField] private float poiseRegenRate = 10f;

        [Tooltip("Hasar alındıktan sonra yenilenmenin başlaması için bekleme süresi")]
        [SerializeField] private float regenDelay = 2.0f;

        private float currentPoise;
        private float delayTimer;

        public float CurrentPoise => currentPoise;
        public float MaxPoise => maxPoise;
        public bool IsPoiseBroken => currentPoise <= 0.001f;

        /// <summary>
        /// Poise tamamen tükendiğinde (duruş kırıldığında) tetiklenen olay.
        /// </summary>
        public event Action OnPoiseBroken;

        /// <summary>
        /// Poise değeri her değiştiğinde tetiklenir (current, max).
        /// </summary>
        public event Action<float, float> OnPoiseChanged;

        public void EnsureInitialized()
        {
            if (currentPoise <= 0f)
            {
                currentPoise = maxPoise;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            EnsureInitialized();
            if (currentPoise >= maxPoise) return;

            if (delayTimer > 0f)
            {
                delayTimer -= Time.deltaTime;
                return;
            }

            currentPoise = Mathf.Min(maxPoise, currentPoise + (poiseRegenRate * Time.deltaTime));
            OnPoiseChanged?.Invoke(currentPoise, maxPoise);
        }

        public void TakePoiseDamage(float poiseDamage)
        {
            if (poiseDamage <= 0f) return;
            EnsureInitialized();

            currentPoise = Mathf.Max(0f, currentPoise - poiseDamage);
            delayTimer = regenDelay;
            OnPoiseChanged?.Invoke(currentPoise, maxPoise);

            if (currentPoise <= 0.001f)
            {
                currentPoise = maxPoise; // Duruş kırıldıktan sonra poise sıfırlanır ve dolmaya başlar
                if (CombatFeedbackController.HasInstance)
                {
                    CombatFeedbackController.Instance.TriggerPoiseBreakFeedback(transform.position + Vector3.up * 1.5f);
                }
                OnPoiseBroken?.Invoke();
            }
        }

        public void ResetPoise()
        {
            currentPoise = maxPoise;
            delayTimer = 0f;
            OnPoiseChanged?.Invoke(currentPoise, maxPoise);
        }
    }
}
