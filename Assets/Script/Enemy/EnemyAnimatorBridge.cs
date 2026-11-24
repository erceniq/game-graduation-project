using System.Collections.Generic;
using UnityEngine;
using HUMAN.Enemy.StateMachine;
using HUMAN.Enemy.StateMachine.States;

namespace HUMAN.Enemy
{
    /// <summary>
    /// Düşmanın FSM durumlarını (Idle, Patrol, Chase, Attack, Stagger, Death)
    /// MidasController / Mixamo Animator bileşeniyle bağlayan köprü scripti.
    /// - Idle: Speed = 0, Idle animasyonu
    /// - Patrol: Speed = 0.5, Run animasyonu
    /// - Chase: Speed = 1.0, Hızlı koşma animasyonu
    /// - Attack: Attack1 animasyonu (Telegraph -> Strike -> Recovery akışıyla senkron)
    /// - Stagger: Vuruş sarsılma / sersemleme tepkisi
    /// - Death: Ölüm duruşu
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    public class EnemyAnimatorBridge : MonoBehaviour
    {
        [Header("Animator Setup")]
        [SerializeField] private Animator animator;

        [Header("Animation State Names")]
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string attack1StateName = "Attack1";
        [SerializeField] private string attack2StateName = "Attack2";
        [SerializeField] private string attack3StateName = "Attack3";

        [Header("Transition Durations")]
        [SerializeField] private float locomotionCrossfade = 0.12f;
        [SerializeField] private float attackCrossfade = 0.05f;

        private EnemyBrain brain;
        private readonly HashSet<string> availableParams = new HashSet<string>();

        public Animator TargetAnimator => animator;

        public void EnsureInitialized()
        {
            if (brain == null)
            {
                brain = GetComponent<EnemyBrain>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                availableParams.Clear();
                foreach (var param in animator.parameters)
                {
                    availableParams.Add(param.name);
                }
            }

            if (brain != null)
            {
                if (brain.StateMachine != null)
                {
                    brain.StateMachine.OnStateChanged -= HandleStateChanged;
                    brain.StateMachine.OnStateChanged += HandleStateChanged;
                }
                brain.OnAttackComboStep -= HandleAttackComboStep;
                brain.OnAttackComboStep += HandleAttackComboStep;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();

            if (brain != null)
            {
                if (brain.StateMachine != null)
                {
                    brain.StateMachine.OnStateChanged -= HandleStateChanged;
                    brain.StateMachine.OnStateChanged += HandleStateChanged;
                }
                brain.OnAttackComboStep -= HandleAttackComboStep;
                brain.OnAttackComboStep += HandleAttackComboStep;
            }
        }

        private void OnDisable()
        {
            if (brain != null)
            {
                if (brain.StateMachine != null)
                {
                    brain.StateMachine.OnStateChanged -= HandleStateChanged;
                }
                brain.OnAttackComboStep -= HandleAttackComboStep;
            }

            if (animator != null)
            {
                animator.speed = 1.0f;
            }
        }

        private void Start()
        {
            EnsureInitialized();

            if (brain != null && brain.StateMachine != null)
            {
                brain.StateMachine.OnStateChanged -= HandleStateChanged;
                brain.StateMachine.OnStateChanged += HandleStateChanged;

                if (brain.StateMachine.CurrentState != null)
                {
                    HandleStateChanged(brain.StateMachine.CurrentState);
                }
            }
        }

        public void SetTargetAnimator(Animator newAnimator)
        {
            animator = newAnimator;
            EnsureInitialized();
        }

        private void HandleStateChanged(IEnemyState newState)
        {
            if (animator == null || newState == null) return;

            // Her durumda zemin durumunu güncelle
            SetParameterBool("IsGrounded", true);

            // Stagger dışındaki durumlarda animator hızını normale döndür
            if (!(newState is EnemyStaggerState))
            {
                animator.speed = 1.0f;
            }

            if (newState is EnemyIdleState)
            {
                SetParameterFloat("Speed", 0f);
                CrossFadeSafe(idleStateName, locomotionCrossfade);
            }
            else if (newState is EnemyPatrolState)
            {
                SetParameterFloat("Speed", 0.5f);
                CrossFadeSafe(runStateName, locomotionCrossfade);
            }
            else if (newState is EnemyChaseState)
            {
                SetParameterFloat("Speed", 1.0f);
                CrossFadeSafe(runStateName, locomotionCrossfade);
            }
            else if (newState is EnemyAttackState attackState)
            {
                SetParameterFloat("Speed", 0f);
                HandleAttackComboStep(attackState.CurrentComboStep);
            }
            else if (newState is EnemyStaggerState)
            {
                SetParameterFloat("Speed", 0f);
                SetParameterTrigger("Stagger");
                // Sersemleme ânında darbe yemiş ağırlığı hissettirmek için animasyon hızını yavaşlat
                animator.speed = 0.2f;
            }
            else if (newState is EnemyDeathState)
            {
                SetParameterFloat("Speed", 0f);
                SetParameterBool("IsDead", true);
                SetParameterTrigger("Death");
                animator.speed = 0.1f;
            }
        }

        public void HandleAttackComboStep(int step)
        {
            if (animator == null) return;

            SetParameterFloat("Speed", 0f);
            SetParameterTrigger("Attack");

            switch (step)
            {
                case 1:
                    CrossFadeSafe(attack1StateName, attackCrossfade);
                    break;
                case 2:
                    CrossFadeSafe(attack2StateName, attackCrossfade);
                    break;
                case 3:
                    CrossFadeSafe(attack3StateName, attackCrossfade);
                    break;
                default:
                    CrossFadeSafe(attack1StateName, attackCrossfade);
                    break;
            }
        }

        private void CrossFadeSafe(string stateName, float duration)
        {
            if (animator != null && animator.isActiveAndEnabled && !string.IsNullOrEmpty(stateName))
            {
                animator.CrossFadeInFixedTime(stateName, duration);
            }
        }

        private void SetParameterFloat(string paramName, float value)
        {
            if (animator != null && availableParams.Contains(paramName))
            {
                animator.SetFloat(paramName, value);
            }
        }

        private void SetParameterBool(string paramName, bool value)
        {
            if (animator != null && availableParams.Contains(paramName))
            {
                animator.SetBool(paramName, value);
            }
        }

        private void SetParameterTrigger(string paramName)
        {
            if (animator != null && availableParams.Contains(paramName))
            {
                animator.SetTrigger(paramName);
            }
        }
    }
}
