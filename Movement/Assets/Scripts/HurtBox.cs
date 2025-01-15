using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
    [SerializeField] private float health = 100;
    Animator animator;
    public GameObject enemy;
    void Start()
    {
        animator = transform.GetComponent<Animator>();
    }

    public Boolean TakeDamage(float damage)
    {
        if(gameObject.tag == "TheMachine"){
            Instantiate(enemy, transform.position + transform.forward * 2, quaternion.identity);
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            enemyAI.player = GameObject.Find("datedevilhunter").transform;
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
