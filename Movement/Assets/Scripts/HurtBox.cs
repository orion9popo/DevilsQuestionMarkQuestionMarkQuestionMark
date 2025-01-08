using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
    [SerializeField] private float health = 100;

    public Boolean TakeDamage(float damage){
        health -= damage;
        if(health <= 0){
            Debug.Log("dead!");
            Destroy(this.gameObject);
            return true;
        }
        return false;
    }

}
