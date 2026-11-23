using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
public class PlayerGun : MonoBehaviour
{
    [Header("Essentials")]
    PlayerInputHandler inputHandler;
    PlayerEnergy playerEnergy;
    public LayerMask enemyLayer;

    [Header("Gun Stuff")]
    [SerializeField] Transform gunTip;
    public Guns guns;
    float currentfireRate;
    float fireTimer;
    bool triggerReleased;

    [Header("Demo")]
    public Guns[] gunsList;


    [Header("Effects")]
    AudioSource audioSource;
    public TrailRenderer bulletTrailPrefab;
    public float trailSpeed = 150f;
    public TextMeshPro damageText;
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        inputHandler = GetComponentInParent<PlayerInputHandler>();
        playerEnergy = GetComponentInParent<PlayerEnergy>();
        currentfireRate = guns.gunFireRate;
        fireTimer= 1/currentfireRate;
    }

    private void Update()
    {
        Aim();
        GunFireRate();
        GunShoot();

        //demo
        GunSwitch();
    }





    private void Aim()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        mousePosition.z = Camera.main.WorldToScreenPoint(transform.position).z;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector3 direction = mouseWorldPosition - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Vector3 eulerAngles = transform.eulerAngles;
        if (transform.parent.rotation.y > 0)
        {
            transform.localRotation = Quaternion.Euler(-angle, 0, 0);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(180+angle, 0, 0);
            
        }

    }   



    private void GunFireRate()
    {
        if (fireTimer > 0)
        {
            fireTimer -= Time.deltaTime;
        }

    }
    private void GunShoot()
    {
        if (!inputHandler.gunShooting)
        {
            triggerReleased = true;
        }

        if (guns.gunType == GunType.Automatic)
        {

            if (inputHandler.gunShooting)
            {
                GunCheck();
            }
        }
        else
        {

            if (inputHandler.gunShooting && triggerReleased)
            {
                GunCheck();
                triggerReleased = false; 
            }
        }
    }
    private void GunCheck()
    {
        


        if (inputHandler.gunShooting&& Time.time >= fireTimer)
        {
            playerEnergy.GunEnergyUsage(guns.gunEnergyDrain);
            audioSource.PlayOneShot(guns.gunSound);

            

            switch (guns.gunType)
            {


                case GunType.Automatic:
                    
                    float randomRangeY = UnityEngine.Random.Range(-guns.gunSpread, guns.gunSpread);
                    Vector3 direction = gunTip.forward + new Vector3(0, randomRangeY, 0);


                    RaycastHit hit;
                    if (Physics.Raycast(gunTip.position, direction, out hit, guns.gunRange)) {


                        
                        float hitDistance = Vector3.Distance(gunTip.position, hit.point);
                        TextMeshPro damageTextEffect = Instantiate(damageText, hit.point + Vector3.up * 0.3f, Quaternion.identity);
                        float gunDamageFinal = Mathf.RoundToInt(guns.gunDamage - (guns.gunDamage * ((0f + hitDistance) / (guns.gunRange*2f))));
                        float fontSize = Mathf.Clamp(gunDamageFinal*3f, 4f, 20f);
                        damageTextEffect.fontSize= fontSize ;
                        damageTextEffect.text = gunDamageFinal.ToString();



                        TrailRenderer trail = Instantiate(bulletTrailPrefab, gunTip.position, Quaternion.identity);
                        StartCoroutine(SpawnTrail(trail, hit.point));
                        Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
                        ParticleSystem spawnedParticle=Instantiate(guns.gunParticle, hit.point, Quaternion.LookRotation(-direction));
                        ParticleSystemRenderer psRenderer = spawnedParticle.GetComponent<ParticleSystemRenderer>();
                        if (psRenderer != null)
                        {
                            psRenderer.sharedMaterial = hitRenderer.sharedMaterial;
                        }
                    }
                    else
                    {
                        Vector3 endPoint = gunTip.position + direction.normalized * guns.gunRange;
                        TrailRenderer trail = Instantiate(bulletTrailPrefab, gunTip.position, Quaternion.identity);
                        StartCoroutine(SpawnTrail(trail, endPoint));
                    }


                        break;
                case GunType.Shotgun:

                    for (int i = 0; i < guns.gunProjectileNumber; i++)
                    {
                        float randomRangeY2 = UnityEngine.Random.Range(-guns.gunSpread, guns.gunSpread);
                        Vector3 direction2 = gunTip.forward + new Vector3(0, randomRangeY2, 0);

                        RaycastHit hit2;
                        if (Physics.Raycast(gunTip.position, direction2, out hit2, guns.gunRange))
                        {

                            float hitDistance = Vector3.Distance(gunTip.position, hit2.point);
                            TextMeshPro damageTextEffect = Instantiate(damageText, hit2.point + Vector3.up * 0.3f, Quaternion.identity);
                            float gunDamageFinal = Mathf.RoundToInt(guns.gunDamage - (guns.gunDamage * ((0f + hitDistance) / guns.gunRange)));
                            float fontSize = Mathf.Clamp(gunDamageFinal * 3f, 4f, 20f);
                            damageTextEffect.fontSize = fontSize;
                            damageTextEffect.text = gunDamageFinal.ToString();



                            TrailRenderer trail = Instantiate(bulletTrailPrefab, gunTip.position, Quaternion.identity);
                            StartCoroutine(SpawnTrail(trail, hit2.point));
                            Renderer hitRenderer2 = hit2.collider.GetComponent<Renderer>();
                            ParticleSystem spawnedParticle2 = Instantiate(guns.gunParticle, hit2.point, Quaternion.LookRotation(-direction2));
                            ParticleSystemRenderer psRenderer2 = spawnedParticle2.GetComponent<ParticleSystemRenderer>();
                            if (psRenderer2 != null)
                            {
                                psRenderer2.sharedMaterial = hitRenderer2.sharedMaterial;
                            }
                        }
                        else
                        {
                            Vector3 endPoint = gunTip.position + direction2.normalized * guns.gunRange;
                            TrailRenderer trail = Instantiate(bulletTrailPrefab, gunTip.position, Quaternion.identity);
                            StartCoroutine(SpawnTrail(trail, endPoint));
                        }

                    }

                    break;
            }

            fireTimer = Time.time +( 1 / currentfireRate);

        }



           
        
    }
    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 targetPoint)
    {
        float distance = Vector3.Distance(trail.transform.position, targetPoint);
        float startingDistance = distance;

        while (distance > 0.01f)
        {
            trail.transform.position = Vector3.Lerp(trail.transform.position, targetPoint, 1 - (distance / startingDistance) + (Time.deltaTime * trailSpeed / startingDistance));
            distance = Vector3.Distance(trail.transform.position, targetPoint);
            yield return null;
        }

        trail.transform.position = targetPoint;

    }
    private void GunSwitch()
    {
        if (inputHandler.pressed1)
        {
            guns = gunsList[0];
            inputHandler.gunShooting = false;
            currentfireRate = guns.gunFireRate;
            fireTimer = 1 / currentfireRate;
        }
        else if (inputHandler.pressed2)
        {
            guns = gunsList[1];
            inputHandler.gunShooting = false;
            currentfireRate = guns.gunFireRate;
            fireTimer = 1 / currentfireRate;
        }
        else if (inputHandler.pressed3)
        {
            guns = gunsList[2];
            inputHandler.gunShooting = false;
            currentfireRate = guns.gunFireRate;
            fireTimer = 1 / currentfireRate;
        }
    }

}
