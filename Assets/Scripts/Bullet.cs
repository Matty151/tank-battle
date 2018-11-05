using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    public float Speed = 600.0f;
    public float LifeTime = 3.0f;
    public int damage = 50;

    void Start()
    {
        Destroy(gameObject, LifeTime);
    }

    void Update()
    {
        transform.position += 
			transform.forward * Speed * Time.deltaTime;       
    }

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}