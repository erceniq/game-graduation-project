using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// HUD özelliklerini test etmek için kontrolcü script.
/// - Sol Tık: Enerjiyi 1 azaltır.
/// - Sağ Tık: Enerjiyi 1 arttırır.
/// - M Tuşu: Canı 20 azaltır (hasar alır, yumuşak geçiş).
/// - N Tuşu: Canı 20 arttırır (iyileşir, yumuşak geçiş).
/// - U Tuşu: Şarjörden 1 mermi harcar (azaltır).
/// - J Tuşu: Şarjöre 1 mermi ekler (max 15'e kadar, fazlası yedeğe eklenir).
/// - Y Tuşu: Toplam mermiye 100 ekler.
/// - R Tuşu: Şarjörü yedek mermiden doldurur (Reload).
/// </summary>
public class HUDTester : MonoBehaviour
{
    [Header("Test Mode")]
    [Tooltip("Test modunu açar/kapatır. Sadece açıkken (true) test tuşları çalışır.")]
    public bool test = false;

    [Header("Health Test Settings")]
    [Tooltip("M tuşuna basıldığında azaltılacak can miktarı")]
    public float healthDecreaseAmount = 20f;

    [Tooltip("N tuşuna basıldığında arttırılacak can miktarı")]
    public float healthIncreaseAmount = 20f;

    [Header("Magazine & Ammo Settings")]
    [Tooltip("Şarjörün tek seferde alabileceği maksimum mermi sayısı")]
    public int maxMagazineAmmo = 15;

    [Tooltip("Şu an şarjörde bulunan anlık mermi sayısı")]
    public int currentAmmo = 15;

    [Tooltip("Çantada / yedekte bulunan toplam mermi sayısı")]
    public int totalAmmo = 60;

    [Header("Debug Info")]
    [SerializeField] private bool logToConsole = true;

    private void Start()
    {
        if (!test) return;

        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxMagazineAmmo);
        SyncAmmoWithHUD();
    }

    private void Update()
    {
        // Test modu kapalıysa hiçbir tuş veya nesne kontrolü yapılmaz (0 CPU / 0 GC)
        if (!test) return;

        PlayerHUD hud = PlayerHUD.Instance;
        if (hud == null) return;

        HandleHealthInput(hud);
        HandleEnergyInput(hud);
        HandleAmmoInput(hud);
    }

    private void HandleHealthInput(PlayerHUD hud)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // M Tuşu: Canı Azalt (-20)
        if (kb.mKey.wasPressedThisFrame)
        {
            hud.DecreaseHealth(healthDecreaseAmount);
            if (logToConsole)
            {
                Debug.Log($"<color=red>[HUDTester] M Tuşu: Can {healthDecreaseAmount} azaltıldı. Hedef Can: {hud.TargetHealth:F0} / {hud.MaxHealth:F0}</color>");
            }
        }
        // N Tuşu: Canı Arttır (+20)
        else if (kb.nKey.wasPressedThisFrame)
        {
            hud.IncreaseHealth(healthIncreaseAmount);
            if (logToConsole)
            {
                Debug.Log($"<color=green>[HUDTester] N Tuşu: Can {healthIncreaseAmount} arttırıldı. Hedef Can: {hud.TargetHealth:F0} / {hud.MaxHealth:F0}</color>");
            }
        }
    }

    private void HandleEnergyInput(PlayerHUD hud)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Sol Tık: Enerjiyi Azalt (-1)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            hud.ConsumeEnergy(1);
            if (logToConsole)
            {
                Debug.Log($"<color=orange>[HUDTester] Sol Tık: Enerji azaltıldı (-1). Mevcut Enerji: {hud.CurrentEnergy}</color>");
            }
        }
        // Sağ Tık: Enerjiyi Arttır (+1)
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            hud.AddEnergy(1);
            if (logToConsole)
            {
                Debug.Log($"<color=cyan>[HUDTester] Sağ Tık: Enerji arttırıldı (+1). Mevcut Enerji: {hud.CurrentEnergy}</color>");
            }
        }
    }

    private void HandleAmmoInput(PlayerHUD hud)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // U Tuşu: Mermiyi Azalt (-1)
        if (kb.uKey.wasPressedThisFrame)
        {
            ConsumeAmmo(hud);
        }
        // J Tuşu: Mermiyi Arttır (+1)
        else if (kb.jKey.wasPressedThisFrame)
        {
            AddAmmo(hud);
        }
        // Y Tuşu: 100 Mermi Ekle (+100)
        else if (kb.yKey.wasPressedThisFrame)
        {
            AddHundredAmmo(hud);
        }
        // R Tuşu: Şarjörü Doldur (Reload)
        else if (kb.rKey.wasPressedThisFrame)
        {
            ReloadMagazine(hud);
        }
    }

    #region HEALTH LOGIC
    [ContextMenu("Decrease Health (-20)")]
    public void TestDamage() => PlayerHUD.Instance?.DecreaseHealth(healthDecreaseAmount);

    [ContextMenu("Increase Health (+20)")]
    public void TestHeal() => PlayerHUD.Instance?.IncreaseHealth(healthIncreaseAmount);
    #endregion

    #region AMMO LOGIC
    [ContextMenu("Consume Ammo (-1)")]
    public void ConsumeAmmo() => ConsumeAmmo(PlayerHUD.Instance);

    public void ConsumeAmmo(PlayerHUD hud)
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            SyncAmmoWithHUD(hud);
            if (logToConsole)
            {
                Debug.Log($"<color=yellow>[HUDTester] U Tuşu: 1 mermi harcandı. Mermi: {currentAmmo} / {totalAmmo}</color>");
            }
        }
        else if (logToConsole)
        {
            Debug.LogWarning("[HUDTester] Şarjör boş! (0 mermi). R tuşuna basarak doldurabilirsiniz.");
        }
    }

    [ContextMenu("Add Ammo (+1)")]
    public void AddAmmo() => AddAmmo(PlayerHUD.Instance);

    public void AddAmmo(PlayerHUD hud)
    {
        if (currentAmmo < maxMagazineAmmo)
        {
            currentAmmo++;
        }
        else
        {
            totalAmmo++;
        }

        SyncAmmoWithHUD(hud);
        if (logToConsole)
        {
            Debug.Log($"<color=green>[HUDTester] J Tuşu: 1 mermi eklendi. Mermi: {currentAmmo} / {totalAmmo}</color>");
        }
    }

    [ContextMenu("Add 100 Ammo (+100)")]
    public void AddHundredAmmo() => AddHundredAmmo(PlayerHUD.Instance);

    public void AddHundredAmmo(PlayerHUD hud)
    {
        totalAmmo += 100;
        SyncAmmoWithHUD(hud);
        if (logToConsole)
        {
            Debug.Log($"<color=green>[HUDTester] Y Tuşu: 100 mermi eklendi! Mermi: {currentAmmo} / {totalAmmo}</color>");
        }
    }

    [ContextMenu("Reload Magazine")]
    public void ReloadMagazine() => ReloadMagazine(PlayerHUD.Instance);

    public void ReloadMagazine(PlayerHUD hud)
    {
        int needed = maxMagazineAmmo - currentAmmo;
        if (needed <= 0)
        {
            if (logToConsole) Debug.Log("[HUDTester] Şarjör zaten tam dolu (15/15).");
            return;
        }

        int ammoToLoad = Mathf.Min(needed, totalAmmo);
        currentAmmo += ammoToLoad;
        totalAmmo -= ammoToLoad;

        SyncAmmoWithHUD(hud);
        if (logToConsole)
        {
            Debug.Log($"<color=lightblue>[HUDTester] R Tuşu: Şarjör dolduruldu! Mermi: {currentAmmo} / {totalAmmo}</color>");
        }
    }

    private void SyncAmmoWithHUD(PlayerHUD hud = null)
    {
        if (hud == null) hud = PlayerHUD.Instance;
        if (hud != null)
        {
            hud.UpdateMagazine(currentAmmo, totalAmmo);
        }
    }
    #endregion
}
