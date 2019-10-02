using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour
{
    Rigidbody rb;
    Tank Origin;
    public float speed = 10, Damage = 50;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Fire(GameObject barrel, Tank origin)
    {
        rb = GetComponent<Rigidbody>();
        transform.position = barrel.transform.position + barrel.transform.forward;
        rb.velocity = barrel.transform.forward * speed;
        transform.LookAt(transform.position + rb.velocity);
        Destroy(gameObject, 5);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Destructable temp;
        if ((temp = collision.collider.transform.root.GetComponent<Destructable>()) != null || (temp = collision.collider.transform.GetComponent<Destructable>()) != null)
        {
            temp.Hit(Damage, Origin);
        }
        Destroy(gameObject);
    }
}
