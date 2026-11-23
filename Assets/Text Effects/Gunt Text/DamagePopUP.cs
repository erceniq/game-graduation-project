using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float destroyTime = 0.8f;

    void Start()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;

        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}