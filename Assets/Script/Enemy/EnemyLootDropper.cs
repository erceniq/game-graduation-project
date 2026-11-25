using UnityEngine;

namespace HUMAN.Enemy
{
    /// <summary>
    /// Düşman öldüğünde (OnDeath) GDD kurallarına uygun olarak para (Naika/Hurda) ve
    /// opsiyonel Core parçalarını 2.5D düzleme saçan bileşen.
    /// - Normal Biped düşman: %100 garantili 20-28 arası para (Currency Shards).
    /// - Shard'lar yukarı ve yana fırlar (0.35s), ardından oyuncuya mıknatıs gibi çekilir.
    /// - HUD üzerindeki Stamina alanını doğrudan günceller.
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    public class EnemyLootDropper : MonoBehaviour
    {
        [Header("Currency Drop Settings (GDD B2)")]
        [Tooltip("Minimum düşecek toplam para miktarı")]
        [SerializeField] private int minCurrency = 20;

        [Tooltip("Maksimum düşecek toplam para miktarı")]
        [SerializeField] private int maxCurrency = 28;

        [Tooltip("Paranın kaç parçaya (shard) bölüneceği")]
        [Range(1, 8)]
        [SerializeField] private int shardCount = 4;

        [Header("Core Part Drop Settings")]
        [Tooltip("Core parça düşürme ihtimali (0.0 = %0, 1.0 = %100). Standart düşmanlar için varsayılan 0.")]
        [Range(0f, 1f)]
        [SerializeField] private float coreDropChance = 0.0f;

        [Header("Prefabs & Spawn Settings")]
        [Tooltip("Özel Shard Prefab'ı (Boş bırakılırsa prosedürel CurrencyPickup oluşturulur)")]
        [SerializeField] private GameObject currencyPickupPrefab;

        [Tooltip("Düşmanın kökünden yukarı spawn ofseti")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.1f, 0f);

        [Tooltip("Saçılma X ekseni kuvvet aralığı")]
        [SerializeField] private Vector2 popForceX = new Vector2(2.0f, 4.5f);

        [Tooltip("Saçılma Y ekseni yukarı fırlatma kuvvet aralığı")]
        [SerializeField] private Vector2 popForceY = new Vector2(4.0f, 6.5f);

        private EnemyBrain brain;
        private bool hasDropped = false;

        public bool HasDropped => hasDropped;
        public int MinCurrency => minCurrency;
        public int MaxCurrency => maxCurrency;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (brain == null) brain = GetComponent<EnemyBrain>();

            if (brain != null)
            {
                brain.OnDeath -= HandleDeath;
                brain.OnDeath += HandleDeath;
            }
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (brain != null)
            {
                brain.OnDeath -= HandleDeath;
            }
        }

        private void HandleDeath()
        {
            DropLoot();
        }

        /// <summary>
        /// Düşman öldüğünde ganimet saçılmasını tetikler.
        /// </summary>
        public void DropLoot()
        {
            if (hasDropped) return;
            hasDropped = true;

            int totalCurrency = Random.Range(minCurrency, maxCurrency + 1);
            SpawnCurrencyShards(totalCurrency);

            // Opsiyonel Core parçası düşürme kontrolü
            if (coreDropChance > 0f && Random.value <= coreDropChance)
            {
                SpawnCorePart();
            }
        }

        private void SpawnCurrencyShards(int totalCurrency)
        {
            if (totalCurrency <= 0 || shardCount <= 0) return;

            int count = Mathf.Min(shardCount, totalCurrency);
            int baseValue = totalCurrency / count;
            int remainder = totalCurrency % count;

            Vector3 spawnPos = transform.position + spawnOffset;
            float zLock = transform.position.z;
            spawnPos.z = zLock;

            Transform playerTarget = brain != null && brain.Target != null 
                ? brain.Target 
                : GameObject.FindGameObjectWithTag("Player")?.transform;

            for (int i = 0; i < count; i++)
            {
                int shardValue = baseValue + (i < remainder ? 1 : 0);
                if (shardValue <= 0) continue;

                // X yönünü her shard için dengeli dağıt (sol/sağ fan açılımı)
                float sideSign = (i % 2 == 0) ? 1f : -1f;
                if (count == 1) sideSign = Random.value > 0.5f ? 1f : -1f;

                float forceX = sideSign * Random.Range(popForceX.x, popForceX.y) * Random.Range(0.85f, 1.15f);
                float forceY = Random.Range(popForceY.x, popForceY.y) * Random.Range(0.9f, 1.1f);
                Vector3 initialVelocity = new Vector3(forceX, forceY, 0f);

                GameObject shardObj;
                CurrencyPickup pickup;

                if (currencyPickupPrefab != null)
                {
                    shardObj = Instantiate(currencyPickupPrefab, spawnPos, Quaternion.identity);
                    pickup = shardObj.GetComponent<CurrencyPickup>();
                    if (pickup == null) pickup = shardObj.AddComponent<CurrencyPickup>();
                }
                else
                {
                    shardObj = new GameObject($"CurrencyShard_{i + 1}");
                    shardObj.transform.position = spawnPos;
                    pickup = shardObj.AddComponent<CurrencyPickup>();
                }

                pickup.Initialize(shardValue, initialVelocity, playerTarget, zLock);
            }
        }

        private void SpawnCorePart()
        {
            // İleride Core crafting veya yükseltme parçası için genişletilebilir
            Debug.Log($"<color=#00FFAA>[EnemyLootDropper]</color> {gameObject.name} nadir bir Core parçası düşürdü!");
        }

        /// <summary>
        /// Test veya yeniden canlandırma (respawn) durumlarında drop bayrağını sıfırlar.
        /// </summary>
        public void ResetDrop()
        {
            hasDropped = false;
        }
    }
}
