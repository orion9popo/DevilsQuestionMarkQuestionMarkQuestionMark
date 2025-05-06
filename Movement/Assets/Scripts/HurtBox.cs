using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.Mathematics;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
    [SerializeField] private float health = 100;
    Animator animator;
    public GameObject enemy;
    private new Rigidbody rigidbody;
    void Start()
    {
        animator = transform.GetComponent<Animator>();
        rigidbody = GetComponentInParent<Rigidbody>();
    }
    void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.tag == "Bounds"){
            TakeDamage(1000000);
        }
    }
    public Boolean TakeDamage(float damage)
    {
        if(gameObject.tag == "TheMachine"){
            Instantiate(enemy, transform.position - transform.forward * 3, quaternion.identity);
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            enemyAI.player = GameObject.FindWithTag("Player").transform;
            enemyAI.hitbox = GameObject.Find("M1Hitbox").GetComponent<Collider>();
            enemyAI.thePack = false;
        }
        health -= damage;
        if (health <= 0)
        {
            StartCoroutine(flicker("Die"));
            return true;
        }
        StartCoroutine(flicker("Damage"));
        return false;
    }
    public void TakeKnockback(Vector3 knockback){
        if(rigidbody)
            rigidbody.AddForce(knockback,ForceMode.Acceleration);
        Debug.Log("doing something");
    }
    public void Halt(){
        rigidbody.velocity = Vector3.zero;
        rigidbody.useGravity = false;
    }
    public void Resume(){
        rigidbody.useGravity = true;
    }

    private IEnumerator flicker(string name)
    {
        if (animator != null && gameObject.tag != "Dying")
        {
            animator.SetTrigger(name);
            yield return new WaitForSeconds(0.01f);
            animator.ResetTrigger(name);
        }
    }

}
