using UnityEngine;
using HUMAN.Combat;

namespace HUMAN.Enemy
{
    /// <summary>
    /// Düşmandan düşen ve Soulslike tarzında oyuncuya doğru çekilerek toplanan para/kaynak (Naika/Shard) parçası.
    /// 2.5D düzlemde (Z ekseni kilitli) çalışır:
    /// 1. Pop & Scatter (0.35s): Düşmandan yukarı ve yana doğru fırlar, yerçekimiyle tatmin edici bir yay çizer.
    /// 2. Magnetize: Oyuncunun gövdesine doğru hızlanarak uçar.
    /// 3. Collect: Oyuncuya ulaştığında PlayerHUD üzerindeki Stamina alanını (currency) günceller ve yok olur.
    /// </summary>
    public class CurrencyPickup : MonoBehaviour
    {
        [Header("Currency Settings")]
        [Tooltip("Bu parçanın taşıdığı para/enerji miktarı")]
        [SerializeField] private int currencyValue = 5;

        [Header("Movement Settings")]
        [Tooltip("İlk saçılma (pop & scatter) süresi (saniye)")]
        [SerializeField] private float scatterDuration = 0.35f;

        [Tooltip("Saçılma esnasındaki yerçekimi ivmesi")]
        [SerializeField] private float gravity = 18f;

        [Tooltip("Oyuncuya doğru çekilme başlangıç hızı")]
        [SerializeField] private float initialMagnetSpeed = 10f;

        [Tooltip("Oyuncuya doğru çekilme maksimum hızı")]
        [SerializeField] private float maxMagnetSpeed = 30f;

        [Tooltip("Mıknatıs ivmesi")]
        [SerializeField] private float magnetAcceleration = 40f;

        [Tooltip("Toplama temas mesafesi")]
        [SerializeField] private float collectDistance = 0.55f;

        [Tooltip("Z ekseni kilit pozisyonu (2.5D derinlik)")]
        [SerializeField] private float zPositionLock = 0f;

        [Header("Visual Settings")]
        [Tooltip("Dönme hızı (derece/saniye)")]
        [SerializeField] private Vector3 rotationSpeed = new Vector3(180f, 320f, 90f);

        [Tooltip("Amber/Altın URP Parlama Rengi")]
        [SerializeField] private Color shardColor = new Color(1.0f, 0.72f, 0.15f, 1.0f);

        [SerializeField] private Color emissionColor = new Color(2.8f, 1.8f, 0.4f, 1.0f);

        private enum State
        {
            Scattering,
            Magnetizing,
            Collected
        }

        private State currentState = State.Scattering;
        private float stateTimer = 0f;
        private Vector3 velocity;
        private float currentMagnetSpeed = 0f;
        private Transform playerTarget;
        private MeshRenderer meshRenderer;

        public int CurrencyValue => currencyValue;

        private void Awake()
        {
            EnsureVisuals();
        }

        public void Initialize(int value, Vector3 initialVelocity, Transform target, float lockZ = 0f)
        {
            currencyValue = Mathf.Max(1, value);
            velocity = initialVelocity;
            velocity.z = 0f;
            playerTarget = target;
            zPositionLock = lockZ;

            Vector3 pos = transform.position;
            pos.z = zPositionLock;
            transform.position = pos;

            currentState = State.Scattering;
            stateTimer = scatterDuration;
            currentMagnetSpeed = initialMagnetSpeed;

            EnsureVisuals();
        }

        private void EnsureVisuals()
        {
            // Eğer nesne üzerinde Renderer yoksa otomatik altın kristal/shard görseli oluştur
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer == null)
            {
                var filter = gameObject.GetComponent<MeshFilter>();
                if (filter == null) filter = gameObject.AddComponent<MeshFilter>();

                // Küçük elmas/kristal küp şeklinde primitive
                GameObject tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                filter.sharedMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(tempPrimitive);

                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);

                // URP Lit / Simple Lit / Unlit materyal
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                                Shader.Find("Universal Render Pipeline/Simple Lit") ??
                                Shader.Find("Universal Render Pipeline/Unlit") ??
                                Shader.Find("Standard");

                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.name = "CurrencyShard_Mat";
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", shardColor);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", shardColor);

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", emissionColor);
                    }
                    meshRenderer.material = mat;
                }

                // Çarpışma istemiyoruz, saf oynanış hissi için trigger collider veya collidersız
                var col = GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (currentState == State.Collected) return;

            // Görsel rotasyon (Soulslike parlayan kül/kristal hissi)
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

            switch (currentState)
            {
                case State.Scattering:
                    UpdateScattering();
                    break;

                case State.Magnetizing:
                    UpdateMagnetizing();
                    break;
            }

            // 2.5D Z kilit garantisi
            Vector3 clampedPos = transform.position;
            clampedPos.z = zPositionLock;
            transform.position = clampedPos;
        }

        private void UpdateScattering()
        {
            stateTimer -= Time.deltaTime;

            // Yerçekimi ve saçılma hareketi
            velocity.y -= gravity * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            if (stateTimer <= 0f)
            {
                currentState = State.Magnetizing;
                currentMagnetSpeed = initialMagnetSpeed;
            }
        }

        private void UpdateMagnetizing()
        {
            if (playerTarget == null)
            {
                // Oyuncuyu bul
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTarget = playerObj.transform;
                }
                else
                {
                    // Fallback: PlayerHUD mevcutsa HUD'a ekle ve yok et
                    Collect();
                    return;
                }
            }

            // Oyuncunun göğüs/merkez seviyesine doğru yönel
            Vector3 targetCenter = playerTarget.position + Vector3.up * 1.0f;
            targetCenter.z = zPositionLock;

            Vector3 direction = (targetCenter - transform.position);
            float distance = direction.magnitude;

            if (distance <= collectDistance)
            {
                Collect();
                return;
            }

            // Hızlanarak oyuncuya uçma
            currentMagnetSpeed = Mathf.MoveTowards(currentMagnetSpeed, maxMagnetSpeed, magnetAcceleration * Time.deltaTime);
            Vector3 moveStep = direction.normalized * currentMagnetSpeed * Time.deltaTime;

            // Aşmayı engelle (Overshoot prevention)
            if (moveStep.magnitude >= distance)
            {
                transform.position = targetCenter;
                Collect();
            }
            else
            {
                transform.position += moveStep;
            }
        }

        private void Collect()
        {
            if (currentState == State.Collected) return;
            currentState = State.Collected;

            // HUD Stamina/Currency sayacını artır
            if (PlayerHUD.HasInstance)
            {
                PlayerHUD.Instance.AddCurrency(currencyValue);
            }
            else if (PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.AddCurrency(currencyValue);
            }

            // Görsel / Ses efektleri (küçük toplama hissi)
            CreateCollectionFeedback();

            Destroy(gameObject);
        }

        private void CreateCollectionFeedback()
        {
            // Mini parıltı / kıvılcım efekti
            if (CombatFeedbackController.HasInstance)
            {
                CombatFeedbackController.Instance.SpawnImpactSparks(transform.position, Vector3.up);
            }
        }
    }
}
