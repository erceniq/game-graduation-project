using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Canvas içindeki HUD bileşenlerini (Can Barı, Hasar İzi, Silah, Şarjör, Stamina, Fist Energy) yöneten merkezi script.
/// Sıfır gereksiz GC tahsisi (string & mesh caching), optimize edilmiş havuzlama ve UI performansı ile çalışır.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    private static PlayerHUD _instance;
    public static PlayerHUD Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerHUD>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("UI Element References")]
    [Tooltip("Can barını temsil eden Image (Filled olarak doldurulur)")]
    [SerializeField] private Image healthBarImage;

    [Tooltip("Hasar izi / gölge barını temsil eden Image (Dark Souls tarzı gecikmeli azalan bar)")]
    [SerializeField] private Image ghostHealthBarImage;

    [Tooltip("İsteğe bağlı can değerini sayısal gösteren TextMeshPro")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Tooltip("Kullanılan silahın ikonunu gösteren Image")]
    [SerializeField] private Image currentWeaponImage;

    [Tooltip("Mermi sayısını gösteren TextMeshPro ('mermi sayısı / toplam mermi sayısı')")]
    [SerializeField] private TextMeshProUGUI magazineText;

    [Tooltip("Stamina değerini gösteren TextMeshPro")]
    [SerializeField] private TextMeshProUGUI staminaText;

    [Tooltip("Enerji parçası şablon nesnesi (FirstEnergy / FistEnergy)")]
    [SerializeField] private RectTransform fistEnergyRect;

    [Header("Health Settings")]
    [Tooltip("Toplam / Maksimum can miktarı")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Oyun başladığındaki can miktarı")]
    [SerializeField] private float initialHealth = 100f;

    [Tooltip("Can değişimlerinde (azalma/artma) yumuşak geçiş (Lerp) yapılsın mı?")]
    [SerializeField] private bool smoothHealthTransition = true;

    [Tooltip("Ana can barı geçiş hızı")]
    [SerializeField] private float healthTransitionSpeed = 12f;

    [Header("Damage Ghost Bar (Hasar İzi / Game-Feel)")]
    [Tooltip("Hasar izi (Damage Ghost Bar) aktif edilsin mi?")]
    [SerializeField] private bool useGhostBar = true;

    [Tooltip("Hasar alındıktan sonra gölge barın erimeye başlaması için bekleme süresi (saniye)")]
    [SerializeField] private float ghostDelay = 0.5f;

    [Tooltip("Gölge barın ana bara yetişme erime hızı")]
    [SerializeField] private float ghostDrainSpeed = 2.5f;

    [Header("Low Health Warning (Kritik Can Uyarısı)")]
    [Tooltip("Can kritik seviyeye indiğinde kırmızı yanıp sönme efekti aktif edilsin mi?")]
    [SerializeField] private bool useLowHealthFlash = true;

    [Tooltip("Kritik can eşiği (Örn: 0.20 = %20 ve altı)")]
    [Range(0.05f, 0.50f)]
    [SerializeField] private float lowHealthThreshold = 0.20f;

    [Tooltip("Yanıp sönme sırasında geçilecek kırmızı uyarı rengi")]
    [SerializeField] private Color lowHealthFlashColor = new Color(0.95f, 0.15f, 0.15f, 1f);

    [Tooltip("Yanıp sönme hızı")]
    [SerializeField] private float lowHealthFlashSpeed = 4f;

    [Header("Weapon Settings")]
    [Tooltip("Varsayılan veya başlangıç silah görseli")]
    [SerializeField] private Sprite defaultWeaponSprite;

    [Header("Magazine / Ammo Settings")]
    [Tooltip("Mermi formatı ({0} = anlık mermi, {1} = toplam/şarjör mermisi)")]
    [SerializeField] private string ammoFormat = "{0} / {1}";
    [Tooltip("Tek haneli sayıların önüne '0' eklensin mi? (Örn: 05 / 30)")]
    [SerializeField] private bool padWithZero = true;
    [Tooltip("Oyun başladığında gösterilecek varsayılan mermi sayısı")]
    [SerializeField] private int initialCurrentAmmo = 0;
    [Tooltip("Oyun başladığında gösterilecek varsayılan toplam mermi sayısı")]
    [SerializeField] private int initialTotalAmmo = 0;

    [Header("Stamina Settings")]
    [Tooltip("Başlangıçta stamina yazısı boş kalsın mı?")]
    [SerializeField] private bool clearStaminaOnStart = true;

    [Header("Fist Energy (Dynamic Spacing & Alignment)")]
    [Tooltip("İlk parçanın başlangıç X ve Y pozisyonu sahnedeki FirstEnergy nesnesinden otomatik alınsın")]
    [SerializeField] private bool useCurrentPositionAsStart = true;

    [Tooltip("İlk enerji nesnesinin başlangıç X pozisyonu (otomatik alınmıyorsa)")]
    [SerializeField] private float energyStartX = -181.5f;

    [Tooltip("Boşlukların nesnenin güncel genişliğine ve ölçeğine (scale) göre dinamik olarak hesaplanması")]
    [SerializeField] private bool useDynamicSpacing = true;

    [Tooltip("Nesnenin toplam genişliğine oran (0.35 - 0.40 parça sprite'ının genişlik oranına göre tam yan yana dizer)")]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float spacingWidthRatio = 0.36f;

    [Tooltip("Dinamik mesafeye eklenecek/çıkarılacak ekstra piksel boşluğu")]
    [SerializeField] private float extraGap = 0f;

    [Tooltip("Dinamik mod kapalıyken kullanılacak sabit X mesafesi")]
    [SerializeField] private float manualSpacingX = 23f;

    [Tooltip("Yeni enerjilerin eklenme yönü: 1 = Sağa doğru, -1 = Sola doğru")]
    [SerializeField] private float directionX = 1f;

    [Tooltip("Maksimum birikebilecek enerji birimi sayısı")]
    [SerializeField] private int maxEnergyUnits = 10;

    [Tooltip("Maksimum X sınırı kullanılsın mı?")]
    [SerializeField] private bool useMaxLimitX = false;

    [Tooltip("Yeni enerji parçalarının geçemeyeceği maksimum X pozisyonu")]
    [SerializeField] private float maxLimitX = 100f;

    [Tooltip("Belirlenen max limite (maxLimitX) sığacak şekilde parçaları otomatik sıkıştır")]
    [SerializeField] private bool autoFitInsideBounds = false;

    [Tooltip("Oyun başladığında verilecek başlangıç enerji sayısı")]
    [SerializeField] private int initialEnergy = 3;

    [Header("Empty Pip Slots (Boş Enerji Yuvaları)")]
    [Tooltip("Arka planda maksimum kapasiteyi gösteren boş enerji yuvaları çizilsin mi?")]
    [SerializeField] private bool showEmptySlots = true;

    [Tooltip("Boş enerji yuvalarının silik / gölge rengi")]
    [SerializeField] private Color emptySlotColor = new Color(0.18f, 0.22f, 0.26f, 0.40f);

    [Header("Raycast Optimization (Performans & Tıklama Engeli)")]
    [Tooltip("HUD üzerindeki tüm dekoratif resim ve yazıların Raycast Target özelliğini kapatır. Böylece fare tıklamaları oyun dünyasına engelsiz geçer.")]
    [SerializeField] private bool disableAllRaycasts = true;

    // Health Durum Değişkenleri
    private float currentHealth = 100f;
    private float targetHealth = 100f;
    private float currentDisplayedFill = 1f;
    private float ghostDisplayedFill = 1f;
    private float ghostTimer = 0f;
    private Color originalHealthColor;
    private bool isFlashingLowHealth = false;

    // Health UI Caching (Update() içinde GC string tahsisini sıfıra indirir)
    private int lastDisplayedTargetHealth = -1;
    private int lastDisplayedMaxHealth = -1;

    // Magazine & Stamina Caching (GC string tahsisini önler)
    private int currentAmmoCount = 0;
    private int totalAmmoCount = 0;
    private int lastMagazineCurrent = -1;
    private int lastMagazineTotal = -1;
    private int lastStaminaValue = -99999;

    // Fist Energy Durum Değişkenleri
    private int currentEnergyUnits = 0;
    private float baseY = 0f;
    private readonly List<RectTransform> energyPipPool = new List<RectTransform>();
    private readonly List<RectTransform> emptySlotPool = new List<RectTransform>();

    // Dynamic Tracking Değişkenleri
    private Vector3 lastPipScale;
    private Vector2 lastPipSize;
    private Vector2 lastPipPos;

    // Public Özellikler (Dışarıdan okuma)
    public float CurrentHealth => currentHealth;
    public float TargetHealth => targetHealth;
    public float MaxHealth => maxHealth;
    public int CurrentAmmo => currentAmmoCount;
    public int TotalAmmo => totalAmmoCount;
    public int CurrentEnergy => currentEnergyUnits;
    public int MaxEnergyUnits => maxEnergyUnits;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        AutoAssignReferences();
        InitEnergyPool();
        DisableAllRaycasts();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Start()
    {
        // 1. Can Barı Başlangıç Kurulumu
        targetHealth = Mathf.Clamp(initialHealth, 0f, maxHealth);
        currentHealth = targetHealth;
        currentDisplayedFill = maxHealth > 0f ? (currentHealth / maxHealth) : 1f;
        ghostDisplayedFill = currentDisplayedFill;
        ghostTimer = 0f;

        EnsureHealthBarFilledType(healthBarImage);
        EnsureHealthBarFilledType(ghostHealthBarImage);

        if (healthBarImage != null)
        {
            originalHealthColor = healthBarImage.color;
            healthBarImage.fillAmount = currentDisplayedFill;
        }

        if (ghostHealthBarImage != null)
        {
            ghostHealthBarImage.fillAmount = ghostDisplayedFill;
            ghostHealthBarImage.gameObject.SetActive(useGhostBar);
        }

        UpdateHealthText(force: true);

        // 2. Silah İkonu
        if (defaultWeaponSprite != null && currentWeaponImage != null)
        {
            SetWeapon(defaultWeaponSprite);
        }

        // 3. Mermi Sayısı
        UpdateMagazine(initialCurrentAmmo, initialTotalAmmo);

        // 4. Stamina
        if (clearStaminaOnStart && staminaText != null)
        {
            staminaText.text = "";
        }

        // 5. Başlangıç Enerjisi & Boş Yuvalar
        SetEnergy(initialEnergy);
    }

    private void Update()
    {
        UpdateHealthBar();

        // FirstEnergy boyutu veya ölçeği değiştiğinde pips'leri güncelle
        if (fistEnergyRect != null)
        {
            Vector3 currentScale = fistEnergyRect.localScale;
            Vector2 currentSize = fistEnergyRect.sizeDelta;
            Vector2 currentPos = fistEnergyRect.anchoredPosition;

            if (currentScale != lastPipScale ||
                currentSize != lastPipSize ||
                (useCurrentPositionAsStart && currentPos != lastPipPos))
            {
                RepositionAllPips();
            }
        }
    }

    #region HEALTH & GAME-FEEL LOGIC
    private void EnsureHealthBarFilledType(Image img)
    {
        if (img == null) return;

        // Unity UI Image requires an active sprite for Type.Filled to generate a clipped mesh
        if (img.sprite == null)
        {
            Sprite white = Resources.Load<Sprite>("WhiteBar");
            if (white == null)
            {
                Texture2D wt = Texture2D.whiteTexture;
                white = Sprite.Create(wt, new Rect(0, 0, wt.width, wt.height), Vector2.one * 0.5f);
            }
            img.sprite = white;
        }

        if (img.type != Image.Type.Filled || img.fillMethod != Image.FillMethod.Horizontal)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarImage == null) return;

        float targetFill = maxHealth > 0f ? (targetHealth / maxHealth) : 0f;

        // 1. Ana can barı yumuşak geçişi
        if (smoothHealthTransition)
        {
            if (Mathf.Abs(currentDisplayedFill - targetFill) > 0.0005f)
            {
                currentDisplayedFill = Mathf.Lerp(currentDisplayedFill, targetFill, Time.deltaTime * healthTransitionSpeed);
            }
            else
            {
                currentDisplayedFill = targetFill;
            }
        }
        else
        {
            currentDisplayedFill = targetFill;
        }

        if (Mathf.Abs(healthBarImage.fillAmount - currentDisplayedFill) > 0.0001f)
        {
            healthBarImage.fillAmount = currentDisplayedFill;
            currentHealth = currentDisplayedFill * maxHealth;
        }

        // 2. Hasar İzi (Damage Ghost Bar) Mantığı
        if (ghostHealthBarImage != null && useGhostBar)
        {
            if (!ghostHealthBarImage.gameObject.activeSelf)
            {
                ghostHealthBarImage.gameObject.SetActive(true);
            }

            if (ghostDisplayedFill > currentDisplayedFill)
            {
                ghostTimer -= Time.deltaTime;
                if (ghostTimer <= 0f)
                {
                    ghostDisplayedFill = Mathf.Lerp(ghostDisplayedFill, currentDisplayedFill, Time.deltaTime * ghostDrainSpeed);
                    if (Mathf.Abs(ghostDisplayedFill - currentDisplayedFill) < 0.002f)
                    {
                        ghostDisplayedFill = currentDisplayedFill;
                    }
                }
            }
            else
            {
                ghostDisplayedFill = currentDisplayedFill;
                ghostTimer = 0f;
            }

            if (Mathf.Abs(ghostHealthBarImage.fillAmount - ghostDisplayedFill) > 0.0001f)
            {
                ghostHealthBarImage.fillAmount = ghostDisplayedFill;
            }
        }
        else if (ghostHealthBarImage != null && !useGhostBar && ghostHealthBarImage.gameObject.activeSelf)
        {
            ghostHealthBarImage.gameObject.SetActive(false);
        }

        // 3. Kritik Can Yanıp Sönmesi (Low Health Flash)
        if (useLowHealthFlash)
        {
            float healthRatio = maxHealth > 0f ? (targetHealth / maxHealth) : 0f;
            if (healthRatio <= lowHealthThreshold && targetHealth > 0f)
            {
                isFlashingLowHealth = true;
                float pingPong = Mathf.PingPong(Time.time * lowHealthFlashSpeed, 1f);
                healthBarImage.color = Color.Lerp(originalHealthColor, lowHealthFlashColor, pingPong);
            }
            else if (isFlashingLowHealth)
            {
                isFlashingLowHealth = false;
                healthBarImage.color = originalHealthColor;
            }
        }
        else if (isFlashingLowHealth)
        {
            isFlashingLowHealth = false;
            healthBarImage.color = originalHealthColor;
        }

        // Değer değişmediği sürece string üretmez (0 GC)
        UpdateHealthText();
    }

    private void UpdateHealthText(bool force = false)
    {
        if (healthText == null) return;

        int roundTarget = Mathf.RoundToInt(targetHealth);
        int roundMax = Mathf.RoundToInt(maxHealth);

        if (force || roundTarget != lastDisplayedTargetHealth || roundMax != lastDisplayedMaxHealth)
        {
            lastDisplayedTargetHealth = roundTarget;
            lastDisplayedMaxHealth = roundMax;
            healthText.text = $"{roundTarget} / {roundMax}";
        }
    }

    /// <summary>
    /// Can miktarını doğrudan ayarlar.
    /// </summary>
    /// <param name="health">Hedef can miktarı</param>
    /// <param name="instant">Yumuşak geçişi ve hasar izini atlayıp anında mı doldursun</param>
    public void SetHealth(float health, bool instant = false)
    {
        float previousTarget = targetHealth;
        targetHealth = Mathf.Clamp(health, 0f, maxHealth);

        if (targetHealth < previousTarget)
        {
            ghostTimer = ghostDelay;
        }

        if (instant || !smoothHealthTransition)
        {
            currentHealth = targetHealth;
            currentDisplayedFill = maxHealth > 0f ? (currentHealth / maxHealth) : 0f;
            ghostDisplayedFill = currentDisplayedFill;
            ghostTimer = 0f;

            if (healthBarImage != null)
            {
                healthBarImage.fillAmount = currentDisplayedFill;
            }

            if (ghostHealthBarImage != null)
            {
                ghostHealthBarImage.fillAmount = ghostDisplayedFill;
            }
        }

        UpdateHealthText();
    }

    public void DecreaseHealth(float amount) => SetHealth(targetHealth - amount);
    public void IncreaseHealth(float amount) => SetHealth(targetHealth + amount);
    public void TakeDamage(float damage) => DecreaseHealth(damage);
    public void Heal(float amount) => IncreaseHealth(amount);

    public void SetMaxHealth(float newMax)
    {
        maxHealth = Mathf.Max(1f, newMax);
        SetHealth(targetHealth);
        UpdateHealthText(force: true);
    }
    #endregion

    #region WEAPON LOGIC
    public void SetWeapon(Sprite weaponSprite)
    {
        if (currentWeaponImage == null) return;

        if (weaponSprite != null)
        {
            currentWeaponImage.sprite = weaponSprite;
            currentWeaponImage.enabled = true;
        }
        else
        {
            currentWeaponImage.enabled = false;
        }
    }
    #endregion

    #region MAGAZINE / AMMO LOGIC
    public void UpdateMagazine(int currentAmmo, int totalAmmo)
    {
        currentAmmoCount = Mathf.Max(0, currentAmmo);
        totalAmmoCount = Mathf.Max(0, totalAmmo);

        if (magazineText == null) return;

        // Önceki değerlerle aynıysa gereksiz string birleştirme ve TMP güncellemeyi engelle
        if (currentAmmoCount == lastMagazineCurrent && totalAmmoCount == lastMagazineTotal)
            return;

        lastMagazineCurrent = currentAmmoCount;
        lastMagazineTotal = totalAmmoCount;

        string currentStr = padWithZero ? currentAmmoCount.ToString("D2") : currentAmmoCount.ToString();
        string totalStr = padWithZero ? totalAmmoCount.ToString("D2") : totalAmmoCount.ToString();

        magazineText.text = string.Format(ammoFormat, currentStr, totalStr);
    }
    #endregion

    #region STAMINA LOGIC
    public void UpdateStamina(int stamina)
    {
        if (staminaText == null) return;
        if (stamina == lastStaminaValue) return;

        lastStaminaValue = stamina;
        staminaText.text = padWithZero ? stamina.ToString("D2") : stamina.ToString();
    }

    public void SetStaminaText(string text)
    {
        if (staminaText == null) return;
        staminaText.text = text ?? "";
        lastStaminaValue = -99999;
    }
    #endregion

    #region FIST ENERGY & EMPTY SLOTS (DİNAMİK BOŞLUKLU DİSCRETE SİSTEM)
    private void InitEnergyPool()
    {
        if (fistEnergyRect == null) return;

        energyPipPool.Clear();
        emptySlotPool.Clear();

        if (useCurrentPositionAsStart)
        {
            energyStartX = fistEnergyRect.anchoredPosition.x;
        }
        baseY = fistEnergyRect.anchoredPosition.y;

        fistEnergyRect.anchoredPosition = new Vector2(energyStartX, baseY);
        energyPipPool.Add(fistEnergyRect);

        // Sahnedeki mevcut klonları ve slotları topla
        if (fistEnergyRect.parent != null)
        {
            string pipPrefix = fistEnergyRect.gameObject.name + "_";
            string slotPrefix = fistEnergyRect.gameObject.name + "_Slot_";

            Transform parent = fistEnergyRect.parent;
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.StartsWith(slotPrefix))
                {
                    RectTransform rt = child.GetComponent<RectTransform>();
                    if (rt != null && !emptySlotPool.Contains(rt)) emptySlotPool.Add(rt);
                }
                else if (child.name.StartsWith(pipPrefix))
                {
                    RectTransform rt = child.GetComponent<RectTransform>();
                    if (rt != null && !energyPipPool.Contains(rt)) energyPipPool.Add(rt);
                }
            }
        }

        lastPipScale = fistEnergyRect.localScale;
        lastPipSize = fistEnergyRect.sizeDelta;
        lastPipPos = fistEnergyRect.anchoredPosition;

        RepositionAllPips();
    }

    public float GetDynamicSpacing()
    {
        if (fistEnergyRect == null) return manualSpacingX;

        float calculatedSpacing;

        if (useDynamicSpacing)
        {
            float actualWidth = fistEnergyRect.rect.width * Mathf.Abs(fistEnergyRect.localScale.x);
            calculatedSpacing = (actualWidth * spacingWidthRatio) + extraGap;
        }
        else
        {
            calculatedSpacing = manualSpacingX;
        }

        if (autoFitInsideBounds && useMaxLimitX && currentEnergyUnits > 1)
        {
            float availableWidth = directionX >= 0 ? (maxLimitX - energyStartX) : (energyStartX - maxLimitX);
            if (availableWidth > 0f)
            {
                float fitSpacing = availableWidth / (currentEnergyUnits - 1);
                calculatedSpacing = Mathf.Min(calculatedSpacing, fitSpacing);
            }
        }

        return calculatedSpacing;
    }

    private void UpdateEmptySlots()
    {
        if (fistEnergyRect == null) return;

        if (!showEmptySlots)
        {
            for (int i = 0; i < emptySlotPool.Count; i++)
            {
                if (emptySlotPool[i] != null && emptySlotPool[i].gameObject.activeSelf)
                    emptySlotPool[i].gameObject.SetActive(false);
            }
            return;
        }

        float spacing = GetDynamicSpacing();
        Vector3 currentScale = fistEnergyRect.localScale;
        Vector2 currentSize = fistEnergyRect.sizeDelta;

        // maxEnergyUnits kadar boş yuva oluştur
        while (emptySlotPool.Count < maxEnergyUnits)
        {
            int index = emptySlotPool.Count;
            GameObject slotObj = Instantiate(fistEnergyRect.gameObject, fistEnergyRect.parent);
            slotObj.name = $"{fistEnergyRect.gameObject.name}_Slot_{index}";

            // Boş yuvaları hiyerarşide aktif parçaların arkasına al
            slotObj.transform.SetSiblingIndex(fistEnergyRect.GetSiblingIndex());

            Image img = slotObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = emptySlotColor;
                img.raycastTarget = false;
            }

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            emptySlotPool.Add(rt);
        }

        // Boş yuvaları konumlandır ve aktif et
        for (int i = 0; i < emptySlotPool.Count; i++)
        {
            RectTransform rt = emptySlotPool[i];
            if (rt == null) continue;

            bool shouldBeActive = i < maxEnergyUnits;
            if (rt.gameObject.activeSelf != shouldBeActive)
            {
                rt.gameObject.SetActive(shouldBeActive);
            }

            if (shouldBeActive)
            {
                float x = energyStartX + (i * spacing * directionX);
                rt.anchoredPosition = new Vector2(x, baseY);
                rt.localScale = currentScale;
                rt.sizeDelta = currentSize;
            }
        }
    }

    public void SetEnergy(int units)
    {
        if (energyPipPool.Count == 0 && fistEnergyRect != null)
        {
            InitEnergyPool();
        }

        currentEnergyUnits = Mathf.Clamp(units, 0, maxEnergyUnits);
        float spacing = GetDynamicSpacing();

        if (useMaxLimitX && !autoFitInsideBounds && spacing > 0f)
        {
            float availableWidth = directionX >= 0 ? (maxLimitX - energyStartX) : (energyStartX - maxLimitX);
            int maxPossible = Mathf.Max(0, Mathf.FloorToInt(availableWidth / spacing) + 1);
            currentEnergyUnits = Mathf.Min(currentEnergyUnits, maxPossible);
        }

        if (fistEnergyRect == null) return;

        // Eksik pips varsa instantiate et
        while (energyPipPool.Count < currentEnergyUnits)
        {
            int index = energyPipPool.Count;
            GameObject newPip = Instantiate(fistEnergyRect.gameObject, fistEnergyRect.parent);
            newPip.name = $"{fistEnergyRect.gameObject.name}_{index}";
            RectTransform rect = newPip.GetComponent<RectTransform>();

            Image img = newPip.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;

            energyPipPool.Add(rect);
        }

        // Tek geçişte pozisyonları, boyutları ve aktiflik durumunu güncelle
        Vector3 currentScale = fistEnergyRect.localScale;
        Vector2 currentSize = fistEnergyRect.sizeDelta;

        for (int i = 0; i < energyPipPool.Count; i++)
        {
            RectTransform pip = energyPipPool[i];
            if (pip == null) continue;

            bool shouldBeActive = i < currentEnergyUnits;
            if (pip.gameObject.activeSelf != shouldBeActive)
            {
                pip.gameObject.SetActive(shouldBeActive);
            }

            if (shouldBeActive)
            {
                float x = energyStartX + (i * spacing * directionX);
                pip.anchoredPosition = new Vector2(x, baseY);
                if (i > 0)
                {
                    pip.localScale = currentScale;
                    pip.sizeDelta = currentSize;
                }
            }
        }

        if (Application.isPlaying)
        {
            UpdateEmptySlots();
        }

        lastPipScale = currentScale;
        lastPipSize = currentSize;
        lastPipPos = fistEnergyRect.anchoredPosition;
    }

    public void AddEnergy(int amount = 1) => SetEnergy(currentEnergyUnits + amount);
    public void ConsumeEnergy(int amount = 1) => SetEnergy(currentEnergyUnits - amount);
    public void ResetEnergy() => SetEnergy(0);

    public void SetMaxLimitX(float limitX, bool enableLimit = true)
    {
        useMaxLimitX = enableLimit;
        maxLimitX = limitX;
        SetEnergy(currentEnergyUnits);
    }

    public void SetSpacingWidthRatio(float ratio)
    {
        spacingWidthRatio = ratio;
        RepositionAllPips();
    }

    public void SetExtraGap(float gap)
    {
        extraGap = gap;
        RepositionAllPips();
    }

    public void RepositionAllPips()
    {
        if (fistEnergyRect == null) return;

        if (useCurrentPositionAsStart && !Application.isPlaying)
        {
            energyStartX = fistEnergyRect.anchoredPosition.x;
            baseY = fistEnergyRect.anchoredPosition.y;
        }

        float spacing = GetDynamicSpacing();
        Vector3 currentScale = fistEnergyRect.localScale;
        Vector2 currentSize = fistEnergyRect.sizeDelta;

        for (int i = 0; i < energyPipPool.Count; i++)
        {
            RectTransform pip = energyPipPool[i];
            if (pip != null)
            {
                float x = energyStartX + (i * spacing * directionX);
                pip.anchoredPosition = new Vector2(x, baseY);

                if (i > 0)
                {
                    pip.localScale = currentScale;
                    pip.sizeDelta = currentSize;
                }
            }
        }

        if (Application.isPlaying)
        {
            UpdateEmptySlots();
        }

        lastPipScale = currentScale;
        lastPipSize = currentSize;
        lastPipPos = fistEnergyRect.anchoredPosition;
    }
    #endregion

    #region EDITOR & CONTEXT MENU
    [ContextMenu("Auto Assign References")]
    public void AutoAssignReferences()
    {
        if (healthBarImage == null)
        {
            Transform t = transform.Find("HealthBackground/HealthBar")
                       ?? transform.Find("HealthBar")
                       ?? transform.Find("HealthBackground");
            if (t != null) healthBarImage = t.GetComponent<Image>();
        }
        EnsureHealthBarFilledType(healthBarImage);
        if (healthBarImage != null) originalHealthColor = healthBarImage.color;

        if (ghostHealthBarImage == null)
        {
            Transform t = transform.Find("HealthBackground/GhostHealthBar")
                       ?? transform.Find("GhostHealthBar");
            if (t != null) ghostHealthBarImage = t.GetComponent<Image>();
        }
        EnsureHealthBarFilledType(ghostHealthBarImage);

        if (currentWeaponImage == null)
        {
            Transform t = transform.Find("CurrentWeapon");
            if (t != null) currentWeaponImage = t.GetComponent<Image>();
        }

        if (magazineText == null)
        {
            Transform t = transform.Find("Magazine");
            if (t != null) magazineText = t.GetComponent<TextMeshProUGUI>();
        }

        if (staminaText == null)
        {
            Transform t = transform.Find("Stamina Number");
            if (t != null) staminaText = t.GetComponent<TextMeshProUGUI>();
        }

        if (fistEnergyRect == null)
        {
            Transform t = transform.Find("FirstEnergy") ?? transform.Find("FistEnergy");
            if (t != null) fistEnergyRect = t.GetComponent<RectTransform>();
        }

        if (defaultWeaponSprite == null && currentWeaponImage != null && currentWeaponImage.sprite != null)
        {
            defaultWeaponSprite = currentWeaponImage.sprite;
        }

        DisableAllRaycasts();
    }

    [ContextMenu("Disable All Raycast Targets")]
    public void DisableAllRaycasts()
    {
        if (!disableAllRaycasts) return;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g != null && g.raycastTarget)
            {
                g.raycastTarget = false;
            }
        }
    }

    [ContextMenu("Test: Decrease Health (-20)")]
    private void TestDecreaseHealth() => DecreaseHealth(20f);

    [ContextMenu("Test: Increase Health (+20)")]
    private void TestIncreaseHealth() => IncreaseHealth(20f);

    [ContextMenu("Test: Add Energy (+1)")]
    private void TestAddEnergy() => AddEnergy(1);

    [ContextMenu("Test: Consume Energy (-1)")]
    private void TestConsumeEnergy() => ConsumeEnergy(1);

    [ContextMenu("Test: Reset Energy (0)")]
    private void TestResetEnergy() => ResetEnergy();

    [ContextMenu("Test: Set Ammo (30 / 120)")]
    private void TestAmmo() => UpdateMagazine(30, 120);

    private void Reset()
    {
        AutoAssignReferences();
        if (fistEnergyRect != null)
        {
            energyStartX = fistEnergyRect.anchoredPosition.x;
            baseY = fistEnergyRect.anchoredPosition.y;
        }
    }

    private void OnValidate()
    {
        EnsureHealthBarFilledType(healthBarImage);
        EnsureHealthBarFilledType(ghostHealthBarImage);

        if (healthBarImage != null && !isFlashingLowHealth)
        {
            originalHealthColor = healthBarImage.color;
        }

        DisableAllRaycasts();

        if (fistEnergyRect != null && !Application.isPlaying)
        {
            if (useCurrentPositionAsStart)
            {
                energyStartX = fistEnergyRect.anchoredPosition.x;
                baseY = fistEnergyRect.anchoredPosition.y;
            }
            RepositionAllPips();
        }
    }
    #endregion
}
