using System.Collections.Generic;
using UnityEngine;
using HUMAN.Enemy.StateMachine;
using HUMAN.Enemy.StateMachine.States;

namespace HUMAN.Enemy.Testing
{
    /// <summary>
    /// Düşmanın durum makinesi geçişlerini (Özellikle Soulslike Telegraph ve Stagger pencerelerini)
    /// görsel olarak renk değişimleriyle izlemeyi sağlayan test bileşeni.
    /// Yeni 3D SkinnedMeshRenderer hiyerarşisiyle MaterialPropertyBlock kullanarak tam uyumlu çalışır.
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    public class EnemyVisualDebug : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("Görsel durum renk değişimleri aktif mi?")]
        [SerializeField] private bool enableVisualDebug = false;

        [Header("State Colors")]
        [SerializeField] private Color idleColor = new Color(0.45f, 0.15f, 0.15f); // Varsayılan Koyu Kırmızı Düşman Tonu
        [SerializeField] private Color patrolColor = new Color(0.2f, 0.6f, 0.3f);  // Yeşilimsi
        [SerializeField] private Color chaseColor = new Color(0.9f, 0.45f, 0.0f);  // Turuncu
        [SerializeField] private Color telegraphColor = new Color(1.0f, 0.9f, 0.1f); // Sarı (Saldırı Hazırlık Uyarısı)
        [SerializeField] private Color activeAttackColor = Color.red;              // Parlak Kırmızı (Hitbox Açık)
        [SerializeField] private Color staggerColor = new Color(0.0f, 0.85f, 1.0f); // Elektrik Mavisi (Savunmasız)
        [SerializeField] private Color deathColor = new Color(0.15f, 0.15f, 0.15f); // Koyu Gri

        private EnemyBrain brain;
        private Renderer[] renderers;
        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            brain = GetComponent<EnemyBrain>();
            propBlock = new MaterialPropertyBlock();
            RefreshRenderers();
        }

        public void RefreshRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        private void Start()
        {
            if (brain != null)
            {
                if (brain.StateMachine != null)
                {
                    brain.StateMachine.OnStateChanged += HandleStateChanged;
                    UpdateVisual(brain.StateMachine.CurrentState);
                }
                brain.OnAttackActive += SetActiveAttackVisual;
            }
        }

        private void OnDestroy()
        {
            if (brain != null)
            {
                if (brain.StateMachine != null)
                {
                    brain.StateMachine.OnStateChanged -= HandleStateChanged;
                }
                brain.OnAttackActive -= SetActiveAttackVisual;
            }
        }

        private void HandleStateChanged(IEnemyState newState)
        {
            UpdateVisual(newState);
        }

        private void UpdateVisual(IEnemyState state)
        {
            if (!enableVisualDebug || state == null) return;

            Color targetColor = idleColor;

            if (state is EnemyIdleState)
            {
                targetColor = idleColor;
            }
            else if (state is EnemyPatrolState)
            {
                targetColor = patrolColor;
            }
            else if (state is EnemyChaseState)
            {
                targetColor = chaseColor;
            }
            else if (state is EnemyAttackState)
            {
                targetColor = telegraphColor;
            }
            else if (state is EnemyStaggerState)
            {
                targetColor = staggerColor;
            }
            else if (state is EnemyDeathState)
            {
                targetColor = deathColor;
            }

            ApplyColor(targetColor);
        }

        private void SetActiveAttackVisual(bool isActive)
        {
            if (!enableVisualDebug) return;

            if (isActive)
            {
                ApplyColor(activeAttackColor);
            }
            else if (brain != null && brain.StateMachine != null)
            {
                UpdateVisual(brain.StateMachine.CurrentState);
            }
        }

        public void ApplyColor(Color col)
        {
            if (renderers == null || renderers.Length == 0)
            {
                RefreshRenderers();
            }

            if (renderers == null || renderers.Length == 0) return;

            if (propBlock == null)
            {
                propBlock = new MaterialPropertyBlock();
            }

            propBlock.SetColor(BaseColorId, col);
            propBlock.SetColor(ColorId, col);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].SetPropertyBlock(propBlock);
                }
            }
        }

        public void ResetToDefault()
        {
            ApplyColor(idleColor);
        }
    }
}
