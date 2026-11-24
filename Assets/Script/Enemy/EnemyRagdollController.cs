using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HUMAN.Combat;
using HUMAN.Enemy.UI;

namespace HUMAN.Enemy
{
    /// <summary>
    /// Düşmanın ölüm ânında (EnemyDeathState) animasyonu devre dışı bırakıp
    /// kemik bazlı fiziksel Ragdoll modunu etkinleştiren kontrolcü bileşen.
    /// - Yüksek sönümleme ve CharacterJoint projeksiyonuyla titremeyi (jitter) önler.
    /// - 1.2s sonra kemikleri dondurarak (Freeze/Sleep) yere düştüğü pozda sabitler.
    /// - 2.5s sonra URP Transparent moda geçip 1.5s içinde solarak (fade-out) yok olur.
    /// </summary>
    public class EnemyRagdollController : MonoBehaviour
    {
        [Header("Root References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Collider mainCollider;
        [SerializeField] private Rigidbody mainRigidbody;
        [SerializeField] private Rigidbody hipsRigidbody;

        [Header("Ragdoll Bones Data")]
        [SerializeField] private List<Rigidbody> boneRigidbodies = new List<Rigidbody>();
        [SerializeField] private List<Collider> boneColliders = new List<Collider>();
        [SerializeField] private List<CharacterJoint> boneJoints = new List<CharacterJoint>();

        [Header("2.5D Ragdoll Settings")]
        [Tooltip("Kalça ve omurga kemiklerinin 2.5D derinlikten (Z ekseni) kaymaması için Z ekseni kilitlensin mi?")]
        [SerializeField] private bool lockZPosition = true;

        [Tooltip("Öldürücü darbe ânında kalçaya uygulanacak savrulma kuvveti")]
        [SerializeField] private float defaultImpulseForce = 12f;

        [Tooltip("Ragdoll aktifleştiğinde ek olarak uygulanacak yukarı yönlü sıçrama oranı")]
        [SerializeField] private float upwardImpulseRatio = 0.35f;

        [Header("Physics Stability Settings (Jitter Prevention)")]
        [Tooltip("Kemiklerin doğrusal hareket sönümlemesi")]
        [SerializeField] private float boneLinearDamping = 1.5f;

        [Tooltip("Kemiklerin açısal dönme sönümlemesi")]
        [SerializeField] private float boneAngularDamping = 2.5f;

        [Tooltip("Fizik motoru çözücü hassasiyeti (Jitter ve eklem patlamasını engeller)")]
        [SerializeField] private int boneSolverIterations = 12;
        [SerializeField] private int boneSolverVelocityIterations = 10;

        [Tooltip("CharacterJoint projeksiyon mesafesi")]
        [SerializeField] private float jointProjectionDistance = 0.05f;

        [Header("Freeze & Settle Settings")]
        [Tooltip("Düşüş tamamlandıktan sonra kemiklerin dondurulması için beklenecek minimum süre (sn)")]
        [SerializeField] private float freezeDelay = 1.2f;

        [Tooltip("Kemik hızlarının altına düştüğünde dondurulacağı hız eşiği (sqrMagnitude)")]
        [SerializeField] private float sleepVelocityThreshold = 0.08f;

        [Header("Death Fade & Cleanup")]
        [Tooltip("Ölümden kaç saniye sonra solma efekti başlayacak?")]
        [SerializeField] private float fadeDelay = 2.5f;

        [Tooltip("Solma süresi (sn)")]
        [SerializeField] private float fadeDuration = 1.5f;

        [Tooltip("Solma tamamlandığında objeyi sahneden yok et (Destroy)")]
        [SerializeField] private bool destroyOnFadeComplete = true;

        private bool isRagdollActive = false;
        private Hitbox[] hitboxes;
        private Hurtbox[] hurtboxes;
        private EnemyWorldUI worldUI;
        private Coroutine freezeCoroutine;
        private Coroutine fadeCoroutine;

        public bool IsRagdollActive => isRagdollActive;

        private void Awake()
        {
            InitializeReferences();
            ResetRagdoll();
        }

        public void InitializeReferences()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (mainCollider == null) mainCollider = GetComponent<Collider>();
            if (mainRigidbody == null) mainRigidbody = GetComponent<Rigidbody>();
            if (worldUI == null) worldUI = GetComponent<EnemyWorldUI>();

            hitboxes = GetComponentsInChildren<Hitbox>(true);
            hurtboxes = GetComponentsInChildren<Hurtbox>(true);

            // Eğer kemik listeleri boşsa otomatik topla
            if (boneRigidbodies.Count == 0 || boneColliders.Count == 0 || boneJoints.Count == 0)
            {
                CollectRagdollBones();
            }
            else
            {
                ApplyStabilitySettings();
            }
        }

        /// <summary>
        /// Modelin altındaki kemik Rigidbody, Collider ve CharacterJoint'lerini otomatik toplar
        /// ve stabilite ayarlarını uygular.
        /// </summary>
        public void CollectRagdollBones()
        {
            boneRigidbodies.Clear();
            boneColliders.Clear();
            boneJoints.Clear();

            var rbs = GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                if (rb == mainRigidbody || rb.gameObject == gameObject) continue;
                boneRigidbodies.Add(rb);

                if (rb.name.Contains("Hips") && hipsRigidbody == null)
                {
                    hipsRigidbody = rb;
                }
            }

            var cols = GetComponentsInChildren<Collider>(true);
            foreach (var col in cols)
            {
                if (col == mainCollider || col.gameObject == gameObject) continue;
                // Hitbox ve Hurtbox trigger alanlarını ragdoll kemik collider'ı olarak sayma
                if (col.GetComponent<Hitbox>() != null || col.GetComponent<Hurtbox>() != null) continue;
                boneColliders.Add(col);
            }

            var joints = GetComponentsInChildren<CharacterJoint>(true);
            boneJoints.AddRange(joints);

            ApplyStabilitySettings();
        }

        /// <summary>
        /// Titremeyi önlemek için Rigidbody ve CharacterJoint parametrelerini günceller.
        /// </summary>
        public void ApplyStabilitySettings()
        {
            for (int i = 0; i < boneRigidbodies.Count; i++)
            {
                var rb = boneRigidbodies[i];
                if (rb != null)
                {
                    rb.linearDamping = boneLinearDamping;
                    rb.angularDamping = boneAngularDamping;
                    rb.solverIterations = boneSolverIterations;
                    rb.solverVelocityIterations = boneSolverVelocityIterations;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                }
            }

            for (int i = 0; i < boneJoints.Count; i++)
            {
                var joint = boneJoints[i];
                if (joint != null)
                {
                    joint.enableProjection = true;
                    joint.projectionDistance = jointProjectionDistance;
                    joint.enablePreprocessing = false; // Eklemlerin birbirini itip titremesini önler
                }
            }
        }

        /// <summary>
        /// Başlangıç durumunda veya canlanmada ragdoll kemiklerini pasifleştirir.
        /// </summary>
        public void ResetRagdoll()
        {
            isRagdollActive = false;

            if (freezeCoroutine != null)
            {
                StopCoroutine(freezeCoroutine);
                freezeCoroutine = null;
            }
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (animator != null) animator.enabled = true;
            if (mainCollider != null) mainCollider.enabled = true;
            if (mainRigidbody != null)
            {
                mainRigidbody.isKinematic = false;
            }

            // Tüm kemik fiziklerini kapat
            for (int i = 0; i < boneRigidbodies.Count; i++)
            {
                var rb = boneRigidbodies[i];
                if (rb != null)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    rb.isKinematic = true;
                }
            }

            for (int i = 0; i < boneColliders.Count; i++)
            {
                if (boneColliders[i] != null)
                {
                    boneColliders[i].enabled = false;
                }
            }
        }

        /// <summary>
        /// Ölüm ânında fizik tabanlı Ragdoll'u devreye sokar ve öldürücü darbe kuvveti uygular.
        /// Ardından dondurma (freeze) ve solma (fade-out) akışlarını başlatır.
        /// </summary>
        public void ActivateRagdoll(Vector3 hitDirection, float impulseForce = -1f)
        {
            if (isRagdollActive) return;
            isRagdollActive = true;

            if (impulseForce < 0f) impulseForce = defaultImpulseForce;

            // 1. Ana animatör ve hareket bileşenlerini kapat
            if (animator != null) animator.enabled = false;
            if (mainCollider != null) mainCollider.enabled = false;
            if (mainRigidbody != null)
            {
                mainRigidbody.linearVelocity = Vector3.zero;
                mainRigidbody.isKinematic = true;
            }

            // 2. Saldırı ve hasar alanlarını devre dışı bırak
            if (hitboxes != null)
            {
                for (int i = 0; i < hitboxes.Length; i++)
                {
                    if (hitboxes[i] != null) hitboxes[i].DeactivateHitbox();
                }
            }

            if (hurtboxes != null)
            {
                for (int i = 0; i < hurtboxes.Length; i++)
                {
                    if (hurtboxes[i] != null) hurtboxes[i].gameObject.SetActive(false);
                }
            }

            // 3. Kemik fiziğini ve çarpışmalarını aç
            for (int i = 0; i < boneColliders.Count; i++)
            {
                if (boneColliders[i] != null)
                {
                    boneColliders[i].enabled = true;
                }
            }

            for (int i = 0; i < boneRigidbodies.Count; i++)
            {
                Rigidbody rb = boneRigidbodies[i];
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    if (lockZPosition)
                    {
                        // 2.5D için Z derinliğine savrulmayı engelle
                        rb.constraints = RigidbodyConstraints.FreezePositionZ;
                    }
                }
            }

            // 4. Kalça ve gövdeye darbe yönünde savrulma kuvveti (Impulse) uygula
            if (hipsRigidbody != null)
            {
                float dirX = Mathf.Abs(hitDirection.x) > 0.05f ? Mathf.Sign(hitDirection.x) : 1f;
                Vector3 impulseVector = new Vector3(dirX, upwardImpulseRatio, 0f).normalized * impulseForce;
                hipsRigidbody.AddForce(impulseVector, ForceMode.Impulse);
                hipsRigidbody.AddTorque(new Vector3(0f, 0f, -dirX * impulseForce * 0.5f), ForceMode.Impulse);
            }

            // 5. Ragdoll titremesini durdurup dondurma ve solup yok olma coroutine'lerini başlat
            if (gameObject.activeInHierarchy)
            {
                freezeCoroutine = StartCoroutine(FreezeRagdollRoutine());
                fadeCoroutine = StartCoroutine(FadeOutAndDestroyRoutine());
            }
        }

        /// <summary>
        /// Yere düştükten sonra kemikleri uyutur (Sleep) ve isKinematic yaparak sıfır titremeyle dondurur.
        /// </summary>
        private IEnumerator FreezeRagdollRoutine()
        {
            float elapsed = 0f;
            while (elapsed < freezeDelay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Hız eşiği kontrolü (en fazla 2.3 saniyeye kadar yerleşmeyi bekle)
            float maxWait = 2.3f;
            while (elapsed < maxWait)
            {
                bool isSettled = true;
                for (int i = 0; i < boneRigidbodies.Count; i++)
                {
                    var rb = boneRigidbodies[i];
                    if (rb != null && !rb.isKinematic)
                    {
                        if (rb.linearVelocity.sqrMagnitude > sleepVelocityThreshold)
                        {
                            isSettled = false;
                            break;
                        }
                    }
                }

                if (isSettled) break;

                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            // Tüm kemikleri uyut ve kinematic yap (Titremeyi tamamen bitir)
            for (int i = 0; i < boneRigidbodies.Count; i++)
            {
                var rb = boneRigidbodies[i];
                if (rb != null)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    rb.Sleep();
                    rb.isKinematic = true;
                }
            }
        }

        /// <summary>
        /// Düşman öldükten fadeDelay saniye sonra model materyallerini URP Transparent moda geçirir,
        /// alpha değerini yumuşakça 0'a çekerek nesneyi sahneden temizler.
        /// </summary>
        private IEnumerator FadeOutAndDestroyRoutine()
        {
            yield return new WaitForSeconds(fadeDelay);

            // World UI barını gizle
            if (worldUI != null)
            {
                worldUI.gameObject.SetActive(false);
            }

            // Model altındaki Renderer bileşenlerini ve materyallerini topla
            var renderers = GetComponentsInChildren<Renderer>(true);
            var materialInstances = new List<Material>();

            for (int r = 0; r < renderers.Length; r++)
            {
                var rend = renderers[r];
                if (rend == null) continue;
                if (rend.GetComponent<ParticleSystem>() != null) continue;

                var mats = rend.materials; // Materyalleri kopyalar (instance oluşturur)
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat != null)
                    {
                        SetupURPTransparent(mat);
                        materialInstances.Add(mat);
                    }
                }
            }

            // Alpha'yı 1'den 0'a yumuşakça düşür (Fade-out)
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (timer / fadeDuration));

                for (int i = 0; i < materialInstances.Count; i++)
                {
                    var mat = materialInstances[i];
                    if (mat != null)
                    {
                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor");
                            c.a = alpha;
                            mat.SetColor("_BaseColor", c);
                        }
                        else if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.GetColor("_Color");
                            c.a = alpha;
                            mat.SetColor("_Color", c);
                        }
                    }
                }

                yield return null;
            }

            if (destroyOnFadeComplete)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// URP Lit materyalini çalışma zamanında Transparent Render moduna dönüştürür.
        /// </summary>
        private void SetupURPTransparent(Material mat)
        {
            if (mat == null) return;

            // 1: Transparent, 0: Opaque
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0.0f); // Alpha blend

            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
