/*using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Hitbox : MonoBehaviour
{
    public float damage = 10f;
    public void Start(){
        Debug.Log("hell world DIE DIE DIE DIE");
        StartCoroutine(waitAndDieJustLikeInRealLife());
    }
    private void OnTriggerEnter(Collider other){
        Debug.Log("Hit");
        HurtBox hurtBox = other.GetComponent<HurtBox>();
        if(hurtBox != null && other.tag != this.tag){
        hurtBox.health -= damage;
        }
    }
    IEnumerator waitAndDieJustLikeInRealLife(){
        yield return new WaitForSeconds(0.2f);
        Destroy(this);
    }

}
*/