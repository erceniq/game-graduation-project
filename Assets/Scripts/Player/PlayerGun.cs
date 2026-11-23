using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
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
    bool gunShot;
    [Header("Effects")]
    AudioSource audioSource;

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
        if(inputHandler.gunShooting&& Time.time >= fireTimer && !gunShot)
        {
            playerEnergy.GunEnergyUsage(guns.gunEnergyDrain);
            audioSource.PlayOneShot(guns.gunSound);

            

            switch (guns.gunType)
            {


                case GunType.Automatic:
                    
                    float randomRangeY = UnityEngine.Random.Range(-guns.gunSpread, guns.gunSpread);
                    Vector3 direction = gunTip.forward + new Vector3(0, randomRangeY, 0);


                    RaycastHit hit;
                    if (Physics.Raycast(gunTip.position, direction, out hit, guns.gunRange, enemyLayer)) { }

                    
                    break;

                case GunType.Shotgun:

                    for (int i = 0; i < guns.gunProjectileNumber; i++)
                    {
                        float randomRangeY2 = UnityEngine.Random.Range(-guns.gunSpread, guns.gunSpread);
                        Vector3 direction2 = gunTip.forward + new Vector3(0, randomRangeY2, 0);

                        RaycastHit hit2;
                        if (Physics.Raycast(gunTip.position, direction2, out hit2, guns.gunRange, enemyLayer)) { }
                    }

                    break;
            }

            fireTimer = Time.time +( 1 / currentfireRate);





        }
    }
}
