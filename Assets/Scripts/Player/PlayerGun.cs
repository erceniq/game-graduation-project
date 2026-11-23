using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerGun : MonoBehaviour
{
    [Header("Essentials")]
    PlayerInputHandler inputHandler;
    public LayerMask enemyLayer;

    [Header("Gun Stuff")]
    [SerializeField] Transform gunTip;
    public Guns guns;
    float currentfireRate;
    float fireTimer;
    bool gunShot;

    private void Start()
    {
        inputHandler = GetComponentInParent<PlayerInputHandler>();
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
        if(inputHandler.gunShooting&& fireTimer <= 0&&!gunShot)
        {
            float randomRangeY = UnityEngine.Random.Range(-guns.gunSpread,guns.gunSpread);
            Vector3 direction = gunTip.forward + new Vector3(0,randomRangeY,0);

            RaycastHit hit;
            if (Physics.Raycast(gunTip.position, direction, out hit,guns.gunRange, enemyLayer))

            Debug.Log("pow");
            fireTimer = 1 / currentfireRate;

        }
    }
}
