using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
    [SerializeField] private float health = 100;
    Animator animator;
    void Start(){
        animator = transform.GetComponent<Animator>();
    }

    public Boolean TakeDamage(float damage){
        health -= damage;
        if(health <= 0){
            Debug.Log("dead!");
            StartCoroutine(flicker("Die"));
            return true;
        }
        StartCoroutine(flicker("Damage"));
        return false;
    }

    private IEnumerator flicker(string name){
        if(animator != null){
        animator.SetTrigger(name);
        yield return new WaitForEndOfFrame();
        animator.ResetTrigger(name);
        }
    }

}
