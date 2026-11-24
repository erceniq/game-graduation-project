using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HUMAN.Combat
{
    /// <summary>
    /// Oyun hissi (Game-Feel / Combat Juice) merkezi kontrolcüsü.
    /// - Başarılı Parry anında: Hitstop (frame freeze), kamera sarsıntısı, altın/cyan kıvılcım patlaması ve ışık flaşı.
    /// - Poise Kırılması (Stagger) anında: Güçlü sarsıntı ve sersemleme geri bildirimi.
    /// - Vuruş ve Hasar anında: Düşman üzerinde beyaz 'Hit Flash' (MaterialPropertyBlock) ve darbe kıvılcımları.
    /// </summary>
    public class CombatFeedbackController : MonoBehaviour
    {
        private static CombatFeedbackController _instance;
        private static bool isQuitting = false;

        /// <summary>
        /// Sahne kapanırken veya uygulama çıkışında yeni bir GameObject spawn etmeden kontrol etmek için güvenli bayrak.
        /// </summary>
        public static bool HasInstance => _instance != null && !isQuitting;

        public static CombatFeedbackController Instance
        {
            get
            {
                if (isQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CombatFeedbackController>();
                    if (_instance == null)
                    {
                        if (isQuitting || !Application.isPlaying)
                        {
                            return null;
                        }

                        GameObject go = new GameObject("CombatFeedbackController");
                        _instance = go.AddComponent<CombatFeedbackController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Hitstop (Frame Freeze) Settings")]
        [SerializeField] private float parryHitstopDuration = 0.12f;
        [SerializeField] private float parryHitstopScale = 0.03f;
        [SerializeField] private float poiseBreakHitstopDuration = 0.10f;
        [SerializeField] private float poiseBreakHitstopScale = 0.05f;
        [SerializeField] private float hitHitstopDuration = 0.04f;
        [SerializeField] private float hitHitstopScale = 0.15f;

        [Header("Camera Shake Settings")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float parryShakeDuration = 0.18f;
        [SerializeField] private float parryShakeIntensity = 0.22f;
        [SerializeField] private float poiseBreakShakeDuration = 0.25f;
        [SerializeField] private float poiseBreakShakeIntensity = 0.25f;
        [SerializeField] private float hitShakeDuration = 0.08f;
        [SerializeField] private float hitShakeIntensity = 0.08f;

        [Header("Hit Flash Settings")]
        [SerializeField] private float hitFlashDuration = 0.09f;
        [SerializeField] private Color hitFlashColor = new Color(2.5f, 2.5f, 2.5f, 1f); // HDR White

        [Header("Slow Motion Visuals")]
        [SerializeField] private Color slowMoTintColor = new Color(0.25f, 0.65f, 1.0f, 1.0f); // Vibrant Cyan/Blue

        private readonly HashSet<GameObject> activeSlowMoTargets = new HashSet<GameObject>();

        private Coroutine hitstopCoroutine;
        private Coroutine shakeCoroutine;
        private Vector3 originalCameraPos;
        private bool isCameraShaking = false;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetTimeScaleBeforeSceneLoad()
        {
            Time.timeScale = 1.0f;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterPlayModeReset()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    isQuitting = true;
                    Time.timeScale = 1.0f;
                }
                else if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode ||
                         state == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {
                    isQuitting = false;
                    Time.timeScale = 1.0f;
                }
            };
        }
#endif

        private void Awake()
        {
            isQuitting = false;
            Time.timeScale = 1.0f;

            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDisable()
        {
            Time.timeScale = 1.0f;
            if (hitstopCoroutine != null)
            {
                StopCoroutine(hitstopCoroutine);
                hitstopCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1.0f;
            if (hitstopCoroutine != null)
            {
                StopCoroutine(hitstopCoroutine);
                hitstopCoroutine = null;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #region HITSTOP
        public void TriggerHitstop(float duration, float scale)
        {
            if (hitstopCoroutine != null)
            {
                StopCoroutine(hitstopCoroutine);
            }
            hitstopCoroutine = StartCoroutine(HitstopRoutine(duration, scale));
        }

        private IEnumerator HitstopRoutine(float duration, float scale)
        {
            try
            {
                Time.timeScale = Mathf.Clamp(scale, 0.01f, 1f);
                yield return new WaitForSecondsRealtime(duration);
            }
            finally
            {
                Time.timeScale = 1.0f;
                hitstopCoroutine = null;
            }
        }
        #endregion

        #region CAMERA SHAKE
        public void TriggerCameraShake(float duration, float intensity)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                if (isCameraShaking)
                {
                    targetCamera.transform.position = originalCameraPos;
                }
            }

            shakeCoroutine = StartCoroutine(CameraShakeRoutine(duration, intensity));
        }

        private IEnumerator CameraShakeRoutine(float duration, float intensity)
        {
            isCameraShaking = true;
            originalCameraPos = targetCamera.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float strength = Mathf.Lerp(intensity, 0f, elapsed / duration);
                // 2.5D için sarsıntıyı öncelikle X ve Y eksenlerinde uygula
                float offsetX = Random.Range(-1f, 1f) * strength;
                float offsetY = Random.Range(-1f, 1f) * strength * 0.7f;

                targetCamera.transform.position = new Vector3(
                    originalCameraPos.x + offsetX,
                    originalCameraPos.y + offsetY,
                    originalCameraPos.z
                );

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            targetCamera.transform.position = originalCameraPos;
            isCameraShaking = false;
            shakeCoroutine = null;
        }
        #endregion

        #region HIT FLASH
        public void TriggerHitFlash(GameObject target)
        {
            if (target == null) return;
            StartCoroutine(HitFlashRoutine(target, hitFlashDuration));
        }

        private IEnumerator HitFlashRoutine(GameObject target, float duration)
        {
            if (target == null) yield break;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) yield break;

            MaterialPropertyBlock flashBlock = new MaterialPropertyBlock();
            flashBlock.SetColor(BaseColorId, hitFlashColor);
            flashBlock.SetColor(ColorId, hitFlashColor);
            flashBlock.SetColor(EmissionColorId, hitFlashColor);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].SetPropertyBlock(flashBlock);
            }

            yield return new WaitForSecondsRealtime(duration);

            if (target != null)
            {
                if (activeSlowMoTargets.Contains(target))
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    block.SetColor(BaseColorId, slowMoTintColor);
                    block.SetColor(ColorId, slowMoTintColor);
                    block.SetColor(EmissionColorId, slowMoTintColor * 0.35f);
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] != null) renderers[i].SetPropertyBlock(block);
                    }
                }
                else
                {
                    MaterialPropertyBlock clearBlock = new MaterialPropertyBlock();
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] != null) renderers[i].SetPropertyBlock(clearBlock);
                    }
                }
            }
        }
        #endregion

        #region PARRY STANCE GLOW
        /// <summary>
        /// Düşman savunma/parry duruşuna geçtiğinde altın/amber renginde görsel bir parlama sağlar.
        /// </summary>
        public void TriggerParryStanceGlow(GameObject target, float duration)
        {
            if (target == null) return;
            StartCoroutine(ParryStanceGlowRoutine(target, duration));
        }

        private IEnumerator ParryStanceGlowRoutine(GameObject target, float duration)
        {
            if (target == null) yield break;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) yield break;

            // Altın / Turuncu (Amber HDR) Parlama
            Color parryGlowColor = new Color(2.8f, 1.6f, 0.2f, 1f);
            MaterialPropertyBlock glowBlock = new MaterialPropertyBlock();
            glowBlock.SetColor(BaseColorId, parryGlowColor);
            glowBlock.SetColor(ColorId, parryGlowColor);
            glowBlock.SetColor(EmissionColorId, parryGlowColor);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].SetPropertyBlock(glowBlock);
            }

            yield return new WaitForSecondsRealtime(duration);

            if (target != null)
            {
                if (activeSlowMoTargets.Contains(target))
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    block.SetColor(BaseColorId, slowMoTintColor);
                    block.SetColor(ColorId, slowMoTintColor);
                    block.SetColor(EmissionColorId, slowMoTintColor * 0.35f);
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] != null) renderers[i].SetPropertyBlock(block);
                    }
                }
                else
                {
                    MaterialPropertyBlock clearBlock = new MaterialPropertyBlock();
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] != null) renderers[i].SetPropertyBlock(clearBlock);
                    }
                }
            }
        }
        #endregion

        #region SLOW MOTION TINT
        /// <summary>
        /// Slow-motion / sersemleme ânında karakterin Renderer materyallerini belirgin mavimsi bir tona çevirir veya normale döndürür.
        /// URP _BaseColor, _Color ve _EmissionColor destekler.
        /// </summary>
        public void SetSlowMotionVisuals(GameObject target, bool active, Color? customColor = null)
        {
            if (target == null) return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            if (active)
            {
                activeSlowMoTargets.Add(target);
                Color tint = customColor ?? slowMoTintColor;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor(BaseColorId, tint);
                block.SetColor(ColorId, tint);
                block.SetColor(EmissionColorId, tint * 0.35f);

                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) renderers[i].SetPropertyBlock(block);
                }
            }
            else
            {
                activeSlowMoTargets.Remove(target);
                MaterialPropertyBlock clearBlock = new MaterialPropertyBlock();
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) renderers[i].SetPropertyBlock(clearBlock);
                }
            }
        }
        #endregion

        #region PROCEDURAL PARTICLES (PARRY & IMPACT SPARKS)
        public void SpawnParrySparks(Vector3 position)
        {
            if (isQuitting || !Application.isPlaying) return;

            GameObject sparkObj = new GameObject("ParrySparkVFX");
            sparkObj.transform.position = position;

            ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 12f;
            main.startSize = 0.12f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = false;

            // Altın & Elektrik Mavisi renk gradyanı
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.0f),
                    new GradientColorKey(new Color(0.1f, 0.8f, 1f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(grad);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 35) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = main.startColor;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1.0f);
            curve.AddKey(1f, 0.0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            // Anlık Işık Patlaması
            GameObject lightObj = new GameObject("ParryFlashLight");
            lightObj.transform.SetParent(sparkObj.transform, false);
            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(0.8f, 0.95f, 1f);
            pLight.range = 6.0f;
            pLight.intensity = 5.0f;

            ps.Play();
            StartCoroutine(FadeLightAndDestroy(sparkObj, pLight, 0.8f));
        }

        public void SpawnImpactSparks(Vector3 position, Vector3 direction)
        {
            if (isQuitting || !Application.isPlaying) return;

            GameObject sparkObj = new GameObject("HitSparkVFX");
            sparkObj.transform.position = position;

            ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.22f;
            main.startSpeed = 8f;
            main.startSize = 0.08f;
            main.startColor = new Color(1.0f, 0.6f, 0.2f, 1.0f); // Turuncu-kırmızı kıvılcım
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.1f;
            if (direction != Vector3.zero)
            {
                sparkObj.transform.rotation = Quaternion.LookRotation(direction);
            }

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1.0f);
            curve.AddKey(1f, 0.0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ps.Play();
            Destroy(sparkObj, 0.5f);
        }

        private IEnumerator FadeLightAndDestroy(GameObject root, Light pLight, float totalDuration)
        {
            float elapsed = 0f;
            float initialIntensity = pLight != null ? pLight.intensity : 0f;

            while (elapsed < 0.20f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (pLight != null)
                {
                    pLight.intensity = Mathf.Lerp(initialIntensity, 0f, elapsed / 0.20f);
                }
                yield return null;
            }

            if (pLight != null) pLight.enabled = false;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, totalDuration - 0.20f));
            if (root != null) Destroy(root);
        }
        #endregion

        #region HIGH-LEVEL FEEDBACK API
        /// <summary>
        /// Mükemmel savuşturma (Parry) ânında çağrılır:
        /// Hitstop + Kamera Sarsıntısı + Altın/Cyan Kıvılcım Patlaması + Işık Flaşı.
        /// </summary>
        public void TriggerParryFeedback(Vector3 hitPoint)
        {
            TriggerHitstop(parryHitstopDuration, parryHitstopScale);
            TriggerCameraShake(parryShakeDuration, parryShakeIntensity);
            SpawnParrySparks(hitPoint);
        }

        /// <summary>
        /// Düşmanın Poise'u kırıldığında (Stagger) çağrılır.
        /// </summary>
        public void TriggerPoiseBreakFeedback(Vector3 hitPoint)
        {
            TriggerHitstop(poiseBreakHitstopDuration, poiseBreakHitstopScale);
            TriggerCameraShake(poiseBreakShakeDuration, poiseBreakShakeIntensity);
            SpawnParrySparks(hitPoint);
        }

        /// <summary>
        /// Düşmana her normal vuruş yapıldığında çağrılır:
        /// Beyaz Hit Flash + Darbe Kıvılcımı + Hafif mikrosarsıntı ve hitstop.
        /// </summary>
        public void TriggerEnemyHitFeedback(GameObject target, Vector3 hitPoint, Vector3 hitDirection)
        {
            TriggerHitstop(hitHitstopDuration, hitHitstopScale);
            TriggerCameraShake(hitShakeDuration, hitShakeIntensity);
            TriggerHitFlash(target);
            SpawnImpactSparks(hitPoint, hitDirection);
        }
        #endregion
    }
}
