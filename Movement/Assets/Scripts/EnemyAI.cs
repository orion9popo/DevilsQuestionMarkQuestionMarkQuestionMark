using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyAI : MonoBehaviour
{
    private bool cooldown = false, isDamaged = false;
    private Vector3 goTo;
    private Enum state;
    private Animator animator;
    private Rigidbody rigidbody;
    private BoxCollider boxCollider;
    public float speed;
    public float attackSpeed;
    public float grabRange;
    public Transform player;
    public float attackRange;
    public float attackRange2;
    public int health = 3; 

    enum States{
        Look,
        Chase,
        Attack
    }
    
    void Start()
    {
        state = States.Chase;
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
    }

    void Update(){
        if(isDamaged) {transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));return;}
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        switch(state){
            case States.Look:
            Look();
            break;
            case States.Chase:
            Chase();
            break;
            case States.Attack:
            Attack();
            break;
        }
    }

    void Chase(){
        animator.SetBool("IsWalking", true);
        goTo = player.position;
        this.transform.position = Vector3.MoveTowards(this.transform.position, goTo, speed * Time.deltaTime);
        if((transform.position-player.position).magnitude < attackRange){
            state = States.Look;
            return;
        }
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
    }

    void Look(){
        if((transform.position-player.position).magnitude > attackRange){
            state = States.Chase;
            return;
        }
        animator.SetBool("IsWalking", false);
        goTo = -Vector3.Cross(transform.position - player.position, transform.up) + transform.position;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        if((transform.position-player.position).magnitude < grabRange && !cooldown){
            animator.SetTrigger("Attack");
            goTo = goTo + transform.forward * 1000;
            state = States.Attack;
            boxCollider.isTrigger = true;
            rigidbody.useGravity = false;
            StartCoroutine(grabSwitch());
            speed = 45;
            return;
        }
        if((transform.position - player.position).magnitude > attackRange2){
            goTo = player.position;
        }
        this.transform.position = Vector3.MoveTowards(this.transform.position,  goTo, attackSpeed * Time.deltaTime);
       
        /*GameObject projectile = Instantiate(bullet, transform.position, Quaternion.identity);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = (player.position - transform.position).normalized * projectileSpeed -  0.5f * Physics.gravity * ((player.position - transform.position).magnitude)/projectileSpeed;
        Destroy(projectile, 2f);
        StartCoroutine(waitCooldown());*/

    }
    void Attack(){
        transform.position = Vector3.MoveTowards(this.transform.position, goTo, speed * Time.deltaTime);
        speed = speed * (1-Time.deltaTime);
        transform.LookAt(goTo);
    }
    public void Kill(){
        tag = "Untagged";
        Destroy(gameObject);
    }
    public void DamageWait(){
        StartCoroutine(damW());
    }
    IEnumerator damW(){
        isDamaged = true;
        yield return new WaitForSeconds(5.75f);
        isDamaged = false;
    }
    IEnumerator grabSwitch(){
        float oldSpeed = speed;
        cooldown = true;
        yield return new WaitForSeconds(2f);
        cooldown = false;
        state = States.Chase;
        speed = oldSpeed;
        boxCollider.isTrigger = false;
        rigidbody.useGravity = true;
    }
}
