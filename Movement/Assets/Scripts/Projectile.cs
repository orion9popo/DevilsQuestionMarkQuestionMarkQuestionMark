using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    Collider collider;
    Rigidbody rigidbody;
    public String enemyTag;
    public float damage;
    public bool isPiercing;
    public Vector3 velocity;
    private float lifeTime = 0;
    public Projectile(String tag, float dmg, bool pierce, Vector3 velo){
        enemyTag = tag;
        damage = dmg;
        isPiercing = pierce;
        velocity = velo;
    }
    void Update()
    {
        lifeTime += Time.deltaTime;
        if(lifeTime > 5){
            Destroy(gameObject);
        }
    }
    void Start()
    {
        collider = transform.GetComponent<Collider>();
        rigidbody = transform.GetComponent<Rigidbody>();
        rigidbody.velocity = velocity;
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.transform.tag == enemyTag){
            other.transform.GetComponent<HurtBox>().TakeDamage(damage);
            if(!isPiercing) Destroy(gameObject);
        }
    }
}
