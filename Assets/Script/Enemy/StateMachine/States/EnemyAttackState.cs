using UnityEngine;

namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Soulslike çoklu saldırı kombo durumu (Attack1 -> Attack2 -> Attack3):
    /// Her vuruş kendi 3 aşamasına sahiptir:
    /// 1. Telegraph (Hazırlık/İpucu): Oyuncuya parry veya dodge zamanlamasını okuma fırsatı verir.
    /// 2. Active (Vuruş): Hitbox açılır, saldırı savuşturulabilir (parryable).
    /// 3. Recovery (Toparlanma): Hitbox kapanır. Oyuncu menzildeyse ve poise kırılmadıysa kombo devam eder.
    /// Eğer oyuncu herhangi bir vuruşu parry ederse, kombo anında kesilir ve düşman Stagger durumuna geçer.
    /// </summary>
    public class EnemyAttackState : EnemyBaseState
    {
        private enum AttackPhase
        {
            Telegraph,
            Active,
            Recovery
        }

        private AttackPhase currentPhase;
        private float phaseTimer;
        private int currentComboStep = 1;
        private bool isRiposte = false;

        public int CurrentComboStep => currentComboStep;
        public bool IsRiposte => isRiposte;

        public EnemyAttackState(EnemyBrain brain, EnemyStateMachine stateMachine) : base(brain, stateMachine) { }

        public void TriggerRiposte()
        {
            isRiposte = true;
            if (stateMachine.CurrentState == this)
            {
                isRiposte = false;
                StartRiposteStep();
            }
            else
            {
                stateMachine.ChangeState(this);
            }
        }

        public override void Enter()
        {
            brain.StopMovement();
            currentComboStep = 1;

            if (isRiposte)
            {
                isRiposte = false;
                StartRiposteStep();
            }
            else
            {
                StartComboStep(currentComboStep);
            }
        }

        private void StartRiposteStep()
        {
            currentComboStep = 1;
            currentPhase = AttackPhase.Telegraph;
            // Hızlı karşı saldırı (0.28s hazırlık süresi, oyuncunun refleksle parry yapabilmesi için adil pencere)
            phaseTimer = 0.28f;

            if (brain.WeaponHitbox != null)
            {
                brain.WeaponHitbox.SetDamage(25f, 25f, parryable: true);
            }

            brain.LookAtTarget();
            brain.NotifyAttackComboStep(1);
        }

        private void StartComboStep(int step)
        {
            currentComboStep = step;
            currentPhase = AttackPhase.Telegraph;

            brain.GetComboTimers(step, out float telegraph, out _, out _, out float dmg, out float poiseDmg);
            phaseTimer = telegraph;

            // Silah hasarını kombo adımına göre güncelle
            if (brain.WeaponHitbox != null)
            {
                brain.WeaponHitbox.SetDamage(dmg, poiseDmg);
            }

            // Düşmanın her kombo vuruşunun başında hedefe dönmesi
            brain.LookAtTarget();

            // Animasyon köprüsüne kombo adımını bildir (Attack1, Attack2 veya Attack3)
            brain.NotifyAttackComboStep(step);
        }

        public override void Tick(float deltaTime)
        {
            phaseTimer -= deltaTime;

            switch (currentPhase)
            {
                case AttackPhase.Telegraph:
                    brain.LookAtTarget();
                    if (phaseTimer <= 0f)
                    {
                        // Active aşamasına geç, Hitbox'ı etkinleştir
                        currentPhase = AttackPhase.Active;
                        brain.GetComboTimers(currentComboStep, out _, out float active, out _, out _, out _);
                        phaseTimer = active;

                        brain.NotifyAttackActive(true);
                        if (brain.WeaponHitbox != null)
                        {
                            brain.WeaponHitbox.ActivateHitbox();
                        }
                    }
                    break;

                case AttackPhase.Active:
                    if (phaseTimer <= 0f)
                    {
                        // Recovery aşamasına geç, Hitbox'ı kapat
                        currentPhase = AttackPhase.Recovery;
                        brain.GetComboTimers(currentComboStep, out _, out _, out float recovery, out _, out _);
                        phaseTimer = recovery;

                        brain.NotifyAttackActive(false);
                        if (brain.WeaponHitbox != null)
                        {
                            brain.WeaponHitbox.DeactivateHitbox();
                        }
                    }
                    break;

                case AttackPhase.Recovery:
                    if (phaseTimer <= 0f)
                    {
                        CheckComboProgression();
                    }
                    break;
            }
        }

        private void CheckComboProgression()
        {
            // Hedef kaybolduysa veya düşman öldüyse saldırıyı sonlandır
            if (brain.Target == null || !brain.IsAlive)
            {
                stateMachine.ChangeState(brain.IdleState);
                return;
            }

            // Düşmanın duruşu (Poise) kırıldıysa kombo kesilir, sersemleme devreye girer
            if (brain.PoiseSystem != null && brain.PoiseSystem.IsPoiseBroken)
            {
                stateMachine.ChangeState(brain.StaggerState);
                return;
            }

            float hDist = brain.GetHorizontalDistanceToTarget();

            // Kombo adımı tamamlanmadıysa (1. veya 2. vuruş) ve oyuncu hala menzildeyse 3. vuruşa (Finisher) kadar devam et
            if (currentComboStep < brain.MaxComboHits && hDist <= brain.AttackRadius * 1.35f)
            {
                StartComboStep(currentComboStep + 1);
            }
            else
            {
                // Kombo bitti (3 vuruş tamamlandı veya oyuncu menzilden çıktı)
                if (hDist <= brain.AttackRadius)
                {
                    // Oyuncu hala yakınsa kısa toparlanma/idle sonrası tekrar komboya başlayabilir
                    stateMachine.ChangeState(brain.IdleState);
                }
                else
                {
                    stateMachine.ChangeState(brain.ChaseState);
                }
            }
        }

        public override void Exit()
        {
            currentComboStep = 1;
            brain.NotifyAttackActive(false);
            if (brain.WeaponHitbox != null)
            {
                brain.WeaponHitbox.DeactivateHitbox();
            }
        }
    }
}
