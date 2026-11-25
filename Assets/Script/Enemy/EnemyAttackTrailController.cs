using UnityEngine;

namespace HUMAN.Enemy
{
    /// <summary>
    /// Düşmanın humanoid iskeletindeki el kemiklerine (RightHand / LeftHand) bağlı
    /// TrailRenderer tabanlı saldırı izi kontrolcüsü.
    /// - Yalnızca EnemyAttackState'in "Active" (hasar penceresi) evresinde iz bırakır.
    /// - 1. ve 2. vuruşlarda: Adam'ın soğuk makinelerini temsil eden camgöbeği/mavi (Cyan/Cold Blue).
    /// - 3. bitirici vuruşta (Finisher): Tehlike uyarısı veren paslı turuncu/kırmızı (Rusty Red/Orange).
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    public class EnemyAttackTrailController : MonoBehaviour
    {
        [Header("Hand Bone Transforms")]
        [SerializeField] private Transform rightHandTransform;
        [SerializeField] private Transform leftHandTransform;

        [Header("Trail Renderers")]
        [SerializeField] private TrailRenderer rightHandTrail;
        [SerializeField] private TrailRenderer leftHandTrail;

        [Header("Trail Parameters")]
        [SerializeField] private float trailTime = 0.22f;
        [SerializeField] private float startWidth = 0.18f;
        [SerializeField] private float endWidth = 0.02f;
        [SerializeField] private Material trailMaterial;

        [Header("GDD Color Palettes")]
        [Tooltip("1. ve 2. normal yumruklar için soğuk camgöbeği/mavi (Adam'ın makineleri)")]
        [SerializeField] private Color normalPunchColor = new Color(0.15f, 0.85f, 1.0f, 0.9f);
        [Tooltip("3. Finisher ağır vuruşu için paslı kırmızı/turuncu (Tehlike uyarısı)")]
        [SerializeField] private Color finisherPunchColor = new Color(1.0f, 0.35f, 0.05f, 1.0f);

        private EnemyBrain brain;
        private int currentComboStep = 1;
        private Gradient normalGradient;
        private Gradient finisherGradient;

        public TrailRenderer RightHandTrail => rightHandTrail;
        public TrailRenderer LeftHandTrail => leftHandTrail;
        public int CurrentComboStep => currentComboStep;

        private void Awake()
        {
            InitializeGradients();
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (brain == null) brain = GetComponent<EnemyBrain>();

            // Kemikleri otomatik bul
            if (rightHandTransform == null || leftHandTransform == null)
            {
                LocateHandBones();
            }

            // TrailRenderer'ları oluştur veya bağla
            EnsureTrails();

            // Olay abonelikleri
            if (brain != null)
            {
                brain.OnAttackComboStep -= HandleComboStep;
                brain.OnAttackComboStep += HandleComboStep;

                brain.OnAttackActive -= HandleAttackActive;
                brain.OnAttackActive += HandleAttackActive;
            }

            SetTrailsEmitting(false);
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (brain != null)
            {
                brain.OnAttackComboStep -= HandleComboStep;
                brain.OnAttackActive -= HandleAttackActive;
            }
            SetTrailsEmitting(false);
        }

        private void InitializeGradients()
        {
            // Soğuk Camgöbeği/Mavi (Normal)
            normalGradient = new Gradient();
            normalGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(normalPunchColor, 0.0f),
                    new GradientColorKey(new Color(0.05f, 0.45f, 0.95f), 1.0f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.85f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );

            // Paslı Kırmızı/Turuncu (Finisher)
            finisherGradient = new Gradient();
            finisherGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(finisherPunchColor, 0.0f),
                    new GradientColorKey(new Color(0.75f, 0.1f, 0.02f), 1.0f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.95f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
        }

        private void LocateHandBones()
        {
            var anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                rightHandTransform = anim.GetBoneTransform(HumanBodyBones.RightHand);
                leftHandTransform = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            }

            // İsim bazlı arama (Mixamo humanoid skeleton fallback)
            if (rightHandTransform == null || leftHandTransform == null)
            {
                var allTransforms = GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    string lowName = t.name.ToLower();
                    if (rightHandTransform == null && (lowName.Contains("righthand") || lowName.Contains("right_hand") || lowName.Contains("hand.r")) && !lowName.Contains("index") && !lowName.Contains("thumb"))
                    {
                        rightHandTransform = t;
                    }
                    if (leftHandTransform == null && (lowName.Contains("lefthand") || lowName.Contains("left_hand") || lowName.Contains("hand.l")) && !lowName.Contains("index") && !lowName.Contains("thumb"))
                    {
                        leftHandTransform = t;
                    }
                }
            }
        }

        private void EnsureTrails()
        {
            if (rightHandTrail == null && rightHandTransform != null)
            {
                rightHandTrail = CreateHandTrail(rightHandTransform, "RightHand_AttackTrail");
            }

            if (leftHandTrail == null && leftHandTransform != null)
            {
                leftHandTrail = CreateHandTrail(leftHandTransform, "LeftHand_AttackTrail");
            }
        }

        private TrailRenderer CreateHandTrail(Transform parentHand, string trailObjName)
        {
            // Varsa mevcut olanı bul
            Transform existing = parentHand.Find(trailObjName);
            if (existing != null)
            {
                var tr = existing.GetComponent<TrailRenderer>();
                if (tr != null) return tr;
            }

            GameObject trailObj = new GameObject(trailObjName);
            trailObj.transform.SetParent(parentHand, false);
            trailObj.transform.localPosition = new Vector3(0f, 0.08f, 0f); // Elin uç/yumruk kısmı
            trailObj.transform.localRotation = Quaternion.identity;

            TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = startWidth;
            trail.endWidth = endWidth;
            trail.minVertexDistance = 0.02f;
            trail.alignment = LineAlignment.TransformZ;
            trail.autodestruct = false;
            trail.emitting = false;

            // URP Uyumlu varsayılan trail materyali
            if (trailMaterial != null)
            {
                trail.material = trailMaterial;
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.name = "M_AttackTrail_Procedural";
                    trail.material = mat;
                }
            }

            return trail;
        }

        private void HandleComboStep(int step)
        {
            currentComboStep = step;
            ApplyPaletteForStep(step);
        }

        private void HandleAttackActive(bool isActive)
        {
            if (isActive)
            {
                ApplyPaletteForStep(currentComboStep);

                // Saldırı adımına göre vuran elin izini (veya Finisher'da iki eli de) aktifleştir
                if (currentComboStep == 1)
                {
                    // 1. Vuruş: Sağ Yumruk
                    if (rightHandTrail != null)
                    {
                        rightHandTrail.Clear();
                        rightHandTrail.emitting = true;
                    }
                }
                else if (currentComboStep == 2)
                {
                    // 2. Vuruş: Sol Yumruk
                    if (leftHandTrail != null)
                    {
                        leftHandTrail.Clear();
                        leftHandTrail.emitting = true;
                    }
                }
                else
                {
                    // 3. Vuruş (Finisher): Ağır darbe için her iki el de alev saçsın
                    if (rightHandTrail != null)
                    {
                        rightHandTrail.Clear();
                        rightHandTrail.emitting = true;
                    }
                    if (leftHandTrail != null)
                    {
                        leftHandTrail.Clear();
                        leftHandTrail.emitting = true;
                    }
                }
            }
            else
            {
                SetTrailsEmitting(false);
            }
        }

        public void ApplyPaletteForStep(int step)
        {
            if (normalGradient == null || finisherGradient == null)
            {
                InitializeGradients();
            }

            Gradient selectedGrad = (step >= 3) ? finisherGradient : normalGradient;

            if (rightHandTrail != null)
            {
                rightHandTrail.colorGradient = selectedGrad;
            }
            if (leftHandTrail != null)
            {
                leftHandTrail.colorGradient = selectedGrad;
            }
        }

        public void SetTrailsEmitting(bool emitting)
        {
            if (rightHandTrail != null) rightHandTrail.emitting = emitting;
            if (leftHandTrail != null) leftHandTrail.emitting = emitting;
        }
    }
}
