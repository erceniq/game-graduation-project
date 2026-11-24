using UnityEngine;
using UnityEngine.UI;
using HUMAN.Enemy.StateMachine;
using HUMAN.Enemy.StateMachine.States;
using HUMAN.Combat;

namespace HUMAN.Enemy.UI
{
    /// <summary>
    /// Düşmanın başının üzerinde yer alan ve kameraya bakan (Billboard) World-Space Can ve Poise barı.
    /// - Üst Bar: Kırmızı Can Barı + Gecikmeli Hasar İzi (Damage Ghost Bar).
    /// - Alt Bar: Altın Sarısı Poise (Duruş) Barı; Duruş kırıldığında (Stagger) parlak elektrik mavisine döner.
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    public class EnemyWorldUI : MonoBehaviour
    {
        [Header("Position & Dynamic Height")]
        [Tooltip("Karakterin görsel sınırlarına (Renderer bounds.max.y) göre dinamik dikey pozisyon hesaplansın mı?")]
        [SerializeField] private bool useDynamicBoundsHeight = true;

        [Tooltip("Dinamik yükseklik aktifken modelin en üst noktası (baş/miğfer tepesi) üzerine eklenecek dikey mesafe")]
        [SerializeField] private float extraHeadPadding = 0.45f;

        [Tooltip("Dinamik hesaplama kapalıyken veya yedek olarak kullanılacak dikey yükseklik")]
        [SerializeField] private float defaultHeightOffset = 4.2f;

        [Tooltip("Barın 3D mesh içine girmemesi ve kameraya doğru önde durması için Z ekseni ötelemesi")]
        [SerializeField] private float forwardZOffset = -0.3f;

        [Tooltip("Yatay (X) eksenindeki ince ayar")]
        [SerializeField] private float horizontalOffset = 0f;

        [SerializeField] private Vector2 canvasSize = new Vector2(110f, 22f);
        [SerializeField] private float canvasScale = 0.01f;

        [Tooltip("Kameranın rotasyonuna tam paralel (Billboard) olsun mu? Kapalıysa dünya düzlemine (Quaternion.identity) kilitlenir.")]
        [SerializeField] private bool matchCameraRotation = true;

        [Header("Colors")]
        [SerializeField] private Color healthColor = new Color(0.85f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color ghostHealthColor = new Color(0.95f, 0.55f, 0.15f, 0.95f);
        [SerializeField] private Color poiseColor = new Color(1.0f, 0.82f, 0.15f, 1f);
        [SerializeField] private Color staggerColor = new Color(0.0f, 0.9f, 1.0f, 1f);

        [Header("Timing")]
        [SerializeField] private float ghostDelay = 0.4f;
        [SerializeField] private float ghostDrainSpeed = 3.0f;

        // References
        private EnemyBrain brain;
        private PoiseSystem poise;
        private Camera targetCamera;

        private RectTransform canvasRect;
        private CanvasGroup canvasGroup;
        private Image healthBarFill;
        private Image ghostHealthBarFill;
        private Image poiseBarFill;

        private float targetHealthRatio = 1f;
        private float currentHealthRatio = 1f;
        private float ghostHealthRatio = 1f;
        private float ghostTimer = 0f;

        private float targetPoiseRatio = 1f;
        private float currentPoiseRatio = 1f;
        private bool isStaggered = false;

        private Renderer[] modelRenderers;

        public void RefreshModelRenderers()
        {
            var allRenderers = GetComponentsInChildren<Renderer>(true);
            var list = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && !allRenderers[i].name.Contains("EnemyWorldUI"))
                {
                    list.Add(allRenderers[i]);
                }
            }
            modelRenderers = list.ToArray();
        }

        public float GetTopY()
        {
            if (modelRenderers == null || modelRenderers.Length == 0)
            {
                RefreshModelRenderers();
            }

            float maxY = float.MinValue;
            if (modelRenderers != null && modelRenderers.Length > 0)
            {
                for (int i = 0; i < modelRenderers.Length; i++)
                {
                    if (modelRenderers[i] != null && modelRenderers[i].enabled)
                    {
                        float rMaxY = modelRenderers[i].bounds.max.y;
                        if (rMaxY > maxY) maxY = rMaxY;
                    }
                }
            }

            if (maxY <= float.MinValue + 1f)
            {
                return transform.position.y + defaultHeightOffset;
            }

            return maxY + extraHeadPadding;
        }

        public void EnsureInitialized()
        {
            if (brain == null) brain = GetComponent<EnemyBrain>();
            if (poise == null) poise = GetComponent<PoiseSystem>();
            if (targetCamera == null) targetCamera = Camera.main;

            RefreshModelRenderers();

            Transform existing = transform.Find("EnemyWorldUI_Canvas");
            if (existing == null)
            {
                BuildWorldSpaceUI();
            }
            else
            {
                BindExistingUI(existing);
            }
        }

        private void BindExistingUI(Transform existing)
        {
            canvasRect = existing.GetComponent<RectTransform>();
            canvasGroup = existing.GetComponent<CanvasGroup>();
            var healthFillTransform = existing.Find("HealthRoot/HealthFill");
            if (healthFillTransform != null) healthBarFill = healthFillTransform.GetComponent<Image>();
            var ghostTransform = existing.Find("HealthRoot/GhostHealthFill");
            if (ghostTransform != null) ghostHealthBarFill = ghostTransform.GetComponent<Image>();
            var poiseFillTransform = existing.Find("PoiseRoot/PoiseFill");
            if (poiseFillTransform != null) poiseBarFill = poiseFillTransform.GetComponent<Image>();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (brain != null)
            {
                brain.OnHealthChanged += HandleHealthChanged;
                brain.OnDeath += HandleDeath;

                if (brain.StateMachine != null)
                {
                    brain.StateMachine.OnStateChanged += HandleStateChanged;
                }
            }

            if (poise != null)
            {
                poise.OnPoiseChanged += HandlePoiseChanged;
                poise.OnPoiseBroken += HandlePoiseBroken;
            }
        }

        private void OnDisable()
        {
            if (brain != null)
            {
                brain.OnHealthChanged -= HandleHealthChanged;
                brain.OnDeath -= HandleDeath;

                if (brain.StateMachine != null)
                {
                    brain.StateMachine.OnStateChanged -= HandleStateChanged;
                }
            }

            if (poise != null)
            {
                poise.OnPoiseChanged -= HandlePoiseChanged;
                poise.OnPoiseBroken -= HandlePoiseBroken;
            }
        }

        private void Start()
        {
            if (brain != null)
            {
                brain.EnsureInitialized();
                targetHealthRatio = brain.MaxHealth > 0 ? (brain.CurrentHealth / brain.MaxHealth) : 1f;
                currentHealthRatio = targetHealthRatio;
                ghostHealthRatio = targetHealthRatio;
            }

            if (poise != null)
            {
                poise.EnsureInitialized();
                targetPoiseRatio = poise.MaxPoise > 0 ? (poise.CurrentPoise / poise.MaxPoise) : 1f;
                currentPoiseRatio = targetPoiseRatio;
            }

            UpdateBarVisuals();
        }

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            if (canvasRect != null)
            {
                // 1. Dünya Pozisyonu: Modelin en üst noktasının 0.45f üzerinde ve Z ekseninde kameraya doğru önde (-0.3f)
                float targetY = useDynamicBoundsHeight ? GetTopY() : (transform.position.y + defaultHeightOffset);
                float targetZ = transform.position.z + forwardZOffset;
                canvasRect.position = new Vector3(transform.position.x + horizontalOffset, targetY, targetZ);

                // 2. Dünya Rotasyonu: Düşmanın Y rotasyonundan (sağa/sola bakışından) tamamen bağımsız
                Quaternion targetRot = (matchCameraRotation && targetCamera != null) 
                    ? targetCamera.transform.rotation 
                    : Quaternion.identity;
                canvasRect.rotation = targetRot;

                // 3. Negatif Ebeveyn Ölçeği (scale.x < 0) Telafisi:
                // Ebeveyn sola dönerken scale.x negatif yapılırsa barın ve dolumunun ters dönmesini önler
                float parentSignX = transform.lossyScale.x < 0f ? -1f : 1f;
                float parentSignY = transform.lossyScale.y < 0f ? -1f : 1f;
                canvasRect.localScale = new Vector3(parentSignX * canvasScale, parentSignY * canvasScale, canvasScale);
            }

            UpdateSmoothFills();
        }

        private void UpdateSmoothFills()
        {
            // 1. Can Barı & Hasar İzi
            currentHealthRatio = Mathf.Lerp(currentHealthRatio, targetHealthRatio, Time.deltaTime * 14f);
            if (healthBarFill != null) healthBarFill.fillAmount = currentHealthRatio;

            if (ghostHealthRatio > currentHealthRatio)
            {
                ghostTimer -= Time.deltaTime;
                if (ghostTimer <= 0f)
                {
                    ghostHealthRatio = Mathf.Lerp(ghostHealthRatio, currentHealthRatio, Time.deltaTime * ghostDrainSpeed);
                }
            }
            else
            {
                ghostHealthRatio = currentHealthRatio;
            }
            if (ghostHealthBarFill != null) ghostHealthBarFill.fillAmount = ghostHealthRatio;

            // 2. Poise Barı
            currentPoiseRatio = Mathf.Lerp(currentPoiseRatio, targetPoiseRatio, Time.deltaTime * 12f);
            if (poiseBarFill != null)
            {
                poiseBarFill.fillAmount = currentPoiseRatio;
                if (isStaggered)
                {
                    // Stagger iken elektrik mavisi puls efekti
                    float pulse = Mathf.PingPong(Time.time * 6f, 1f);
                    poiseBarFill.color = Color.Lerp(staggerColor, Color.white, pulse * 0.5f);
                }
                else
                {
                    poiseBarFill.color = poiseColor;
                }
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float newRatio = max > 0f ? (current / max) : 0f;
            if (newRatio < targetHealthRatio)
            {
                ghostTimer = ghostDelay;
            }
            targetHealthRatio = newRatio;
        }

        private void HandlePoiseChanged(float current, float max)
        {
            targetPoiseRatio = max > 0f ? (current / max) : 0f;
        }

        private void HandlePoiseBroken()
        {
            isStaggered = true;
        }

        private void HandleStateChanged(IEnemyState newState)
        {
            if (newState is EnemyStaggerState)
            {
                isStaggered = true;
            }
            else if (isStaggered)
            {
                isStaggered = false;
            }

            if (newState is EnemyDeathState)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void UpdateBarVisuals()
        {
            if (healthBarFill != null) healthBarFill.fillAmount = currentHealthRatio;
            if (ghostHealthBarFill != null) ghostHealthBarFill.fillAmount = ghostHealthRatio;
            if (poiseBarFill != null) poiseBarFill.fillAmount = currentPoiseRatio;
        }

        #region PROCEDURAL CANVAS SETUP
        private void BuildWorldSpaceUI()
        {
            Transform existing = transform.Find("EnemyWorldUI_Canvas");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            GameObject cObj = new GameObject("EnemyWorldUI_Canvas");
            cObj.transform.SetParent(transform, false);
            cObj.transform.localPosition = new Vector3(horizontalOffset, defaultHeightOffset, forwardZOffset);

            Canvas canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            cObj.AddComponent<CanvasScaler>();

            canvasGroup = cObj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            canvasRect = cObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = canvasSize;
            canvasRect.localScale = Vector3.one * canvasScale;

            Sprite whiteSprite = GetWhiteSprite();

            // 1. Arka Plan Çerçevesi
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(cObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = whiteSprite;
            bgImg.color = new Color(0.06f, 0.06f, 0.08f, 0.85f);
            bgImg.raycastTarget = false;

            // 2. Can Barı Bölmesi (Üst %65)
            GameObject hpRoot = new GameObject("HealthRoot");
            hpRoot.transform.SetParent(cObj.transform, false);
            RectTransform hpRootRect = hpRoot.AddComponent<RectTransform>();
            hpRootRect.anchorMin = new Vector2(0.03f, 0.38f);
            hpRootRect.anchorMax = new Vector2(0.97f, 0.92f);
            hpRootRect.offsetMin = Vector2.zero;
            hpRootRect.offsetMax = Vector2.zero;

            // Health BG
            GameObject hpBg = new GameObject("HealthBG");
            hpBg.transform.SetParent(hpRoot.transform, false);
            RectTransform hpBgRect = hpBg.AddComponent<RectTransform>();
            hpBgRect.anchorMin = Vector2.zero;
            hpBgRect.anchorMax = Vector2.one;
            hpBgRect.offsetMin = Vector2.zero;
            hpBgRect.offsetMax = Vector2.zero;
            Image hpBgImg = hpBg.AddComponent<Image>();
            hpBgImg.sprite = whiteSprite;
            hpBgImg.color = new Color(0.22f, 0.04f, 0.04f, 1f);
            hpBgImg.raycastTarget = false;

            // Ghost Health Fill
            GameObject ghostObj = new GameObject("GhostHealthFill");
            ghostObj.transform.SetParent(hpRoot.transform, false);
            RectTransform ghostRect = ghostObj.AddComponent<RectTransform>();
            ghostRect.anchorMin = Vector2.zero;
            ghostRect.anchorMax = Vector2.one;
            ghostRect.offsetMin = Vector2.zero;
            ghostRect.offsetMax = Vector2.zero;
            ghostHealthBarFill = ghostObj.AddComponent<Image>();
            ghostHealthBarFill.sprite = whiteSprite;
            ghostHealthBarFill.type = Image.Type.Filled;
            ghostHealthBarFill.fillMethod = Image.FillMethod.Horizontal;
            ghostHealthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            ghostHealthBarFill.color = ghostHealthColor;
            ghostHealthBarFill.raycastTarget = false;

            // Main Health Fill
            GameObject hpObj = new GameObject("HealthFill");
            hpObj.transform.SetParent(hpRoot.transform, false);
            RectTransform hpFillRect = hpObj.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            healthBarFill = hpObj.AddComponent<Image>();
            healthBarFill.sprite = whiteSprite;
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthBarFill.color = healthColor;
            healthBarFill.raycastTarget = false;

            // 3. Poise Barı Bölmesi (Alt %30)
            GameObject poiseRoot = new GameObject("PoiseRoot");
            poiseRoot.transform.SetParent(cObj.transform, false);
            RectTransform poiseRootRect = poiseRoot.AddComponent<RectTransform>();
            poiseRootRect.anchorMin = new Vector2(0.03f, 0.08f);
            poiseRootRect.anchorMax = new Vector2(0.97f, 0.32f);
            poiseRootRect.offsetMin = Vector2.zero;
            poiseRootRect.offsetMax = Vector2.zero;

            // Poise BG
            GameObject poiseBg = new GameObject("PoiseBG");
            poiseBg.transform.SetParent(poiseRoot.transform, false);
            RectTransform poiseBgRect = poiseBg.AddComponent<RectTransform>();
            poiseBgRect.anchorMin = Vector2.zero;
            poiseBgRect.anchorMax = Vector2.one;
            poiseBgRect.offsetMin = Vector2.zero;
            poiseBgRect.offsetMax = Vector2.zero;
            Image poiseBgImg = poiseBg.AddComponent<Image>();
            poiseBgImg.sprite = whiteSprite;
            poiseBgImg.color = new Color(0.12f, 0.14f, 0.16f, 1f);
            poiseBgImg.raycastTarget = false;

            // Poise Fill
            GameObject poiseObj = new GameObject("PoiseFill");
            poiseObj.transform.SetParent(poiseRoot.transform, false);
            RectTransform poiseFillRect = poiseObj.AddComponent<RectTransform>();
            poiseFillRect.anchorMin = Vector2.zero;
            poiseFillRect.anchorMax = Vector2.one;
            poiseFillRect.offsetMin = Vector2.zero;
            poiseFillRect.offsetMax = Vector2.zero;
            poiseBarFill = poiseObj.AddComponent<Image>();
            poiseBarFill.sprite = whiteSprite;
            poiseBarFill.type = Image.Type.Filled;
            poiseBarFill.fillMethod = Image.FillMethod.Horizontal;
            poiseBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            poiseBarFill.color = poiseColor;
            poiseBarFill.raycastTarget = false;
        }

        private Sprite GetWhiteSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
        }
        #endregion
    }
}
