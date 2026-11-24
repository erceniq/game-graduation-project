using UnityEngine;
using HUMAN.Combat;

namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Düşmanın canı sıfıra indiğinde girdiği kalıcı ölüm durumu.
    /// Tüm saldırı, hareket girdilerini keser ve kök çarpışma alanlarını kapatır.
    /// </summary>
    public class EnemyDeathState : EnemyBaseState
    {
        public EnemyDeathState(EnemyBrain brain, EnemyStateMachine stateMachine) : base(brain, stateMachine) { }

        public override void Enter()
        {
            brain.StopMovement();

            // 1. Kök obje üzerindeki CharacterController ve ana Collider bileşenlerini devre dışı bırak
            var cc = brain.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var rootCols = brain.GetComponents<Collider>();
            for (int i = 0; i < rootCols.Length; i++)
            {
                if (rootCols[i] != null) rootCols[i].enabled = false;
            }

            // 2. Weapon_Hitbox ve Hurtbox alanlarını tamamen kapat
            if (brain.WeaponHitbox != null)
            {
                brain.WeaponHitbox.DeactivateHitbox();
                brain.WeaponHitbox.enabled = false;
            }

            var hitboxes = brain.GetComponentsInChildren<Hitbox>(true);
            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i] != null)
                {
                    hitboxes[i].DeactivateHitbox();
                    hitboxes[i].enabled = false;
                }
            }

            var hurtboxes = brain.GetComponentsInChildren<Hurtbox>(true);
            for (int i = 0; i < hurtboxes.Length; i++)
            {
                if (hurtboxes[i] != null)
                {
                    hurtboxes[i].enabled = false;
                    hurtboxes[i].gameObject.SetActive(false);
                }
            }

            // Sersemlemeden ölüm durumuna geçildiyse slow-motion efektini temizle
            if (CombatFeedbackController.HasInstance)
            {
                CombatFeedbackController.Instance.SetSlowMotionVisuals(brain.gameObject, false);
            }

            // 3. Fiziksel Ragdoll'u aktifleştir
            if (brain.RagdollController != null)
            {
                brain.RagdollController.ActivateRagdoll(brain.LastHitDirection);
            }
        }

        public override void Tick(float deltaTime)
        {
            // Ölüm durumunda ek bir mantık çalışmaz
        }
    }
}
