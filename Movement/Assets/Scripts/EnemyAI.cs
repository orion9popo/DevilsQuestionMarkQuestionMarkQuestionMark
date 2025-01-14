using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

public class EnemyAI : MonoBehaviour
{
    private bool cooldown = false, isDamaged = false, isDying = false;
    private Vector3 goTo;
    private Enum state;
    private Animator animator;
    private Rigidbody rigidbody;
    private BoxCollider boxCollider;
    public float speed;
    public bool thePack = false;
    public float attackSpeed;
    public float grabRange;
    public Transform player;
    public float attackRange;
    public float attackRange2;
    public int health = 3;
    public Collider hitbox;

    enum States
    {
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

    void Update()
    {
        if (thePack) { 
            animator.SetBool("IsWalking", true);  
            return; 
        }
        if (isDamaged || isDying) { transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z)); return; }
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        switch (state)
        {
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

    void Chase()
    {
        animator.SetBool("IsWalking", true);
        goTo = player.position;
        this.transform.position = Vector3.MoveTowards(this.transform.position, goTo, speed * Time.deltaTime);
        if ((transform.position - player.position).magnitude < attackRange)
        {
            state = States.Look;
            return;
        }
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
    }

    void Look()
    {
        if ((transform.position - player.position).magnitude > attackRange)
        {
            state = States.Chase;
            return;
        }
        animator.SetBool("IsWalking", true);
        goTo = -Vector3.Cross(transform.position - player.position, transform.up) + transform.position;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        if ((transform.position - player.position).magnitude < grabRange && !cooldown)
        {
            animator.SetTrigger("Attack");
            goTo = goTo + transform.forward * 1000;
            state = States.Attack;
            StartCoroutine(grabSwitch());
            speed = 3;
            return;
        }
        if ((transform.position - player.position).magnitude > attackRange2)
        {
            goTo = player.position;
        }
        this.transform.position = Vector3.MoveTowards(this.transform.position, goTo, attackSpeed * Time.deltaTime);

        /*GameObject projectile = Instantiate(bullet, transform.position, Quaternion.identity);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = (player.position - transform.position).normalized * projectileSpeed -  0.5f * Physics.gravity * ((player.position - transform.position).magnitude)/projectileSpeed;
        Destroy(projectile, 2f);
        StartCoroutine(waitCooldown());*/

    }
    private bool launchAttack(Collider other, Vector3 pos)
    {
        Collider[] cols = Physics.OverlapBox(pos, other.bounds.extents, transform.rotation);
        GameObject visual = Instantiate(other.transform.gameObject, pos, transform.rotation);
        StartCoroutine(DestoryHitbox(visual));
        bool didHit = false;
        foreach (Collider col in cols)
        {
            if (col.tag == tag)
                continue;

            HurtBox hurtBox = col.transform.GetComponent<HurtBox>();
            if (hurtBox != null)
            {
                hurtBox.TakeDamage(5);
                didHit = true;
                Debug.Log("did damage");
            }
        }
        return didHit;
    }

    void SpawnHitbox()
    {
        launchAttack(hitbox, transform.position + transform.forward);
    }
    void Attack()
    {
        transform.position = Vector3.MoveTowards(this.transform.position, goTo, speed * Time.deltaTime);
        speed = speed * (1 - Time.deltaTime);
        transform.LookAt(goTo);
    }
    public void Kill()
    {
        Destroy(gameObject);
    }
    public void startKill()
    {
        isDying = true;
        gameObject.tag = "Dying";
    }
    public void DamageWait()
    {
        StartCoroutine(damW());
    }
    private IEnumerator DestoryHitbox(GameObject hitbox)
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(hitbox);
    }
    IEnumerator damW()
    {
        isDamaged = true;
        yield return new WaitForSeconds(2.3f);
        isDamaged = false;
    }
    IEnumerator grabSwitch()
    {
        float oldSpeed = speed;
        cooldown = true;
        yield return new WaitForSeconds(2f);
        cooldown = false;
        state = States.Chase;
        speed = oldSpeed;
    }
}
