using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerGun : MonoBehaviour
{




    private void Update()
    {
        Aim();
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
}
