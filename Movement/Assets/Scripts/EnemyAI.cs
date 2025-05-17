using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class EnemyAI : MonoBehaviour
{
    private bool cooldown = false, isDamaged = false, isDying = false;
    private Vector3 goTo;
    private Enum state;
    private Animator animator;
    private Rigidbody rigidbody;
    private BoxCollider boxCollider;
    private GameObject enemyCount;
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
        enemyCount = GameObject.Find("EnemyCount");
        if(player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
        if(hitbox == null)
            hitbox = GameObject.Find("M1Hitbox").GetComponent<Collider>();
    }

    void Update()
    {
        if (thePack) { 
            animator.SetBool("IsWalking", true);  
            return; 
        }
        if (isDamaged || isDying) { transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z)); return; }
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
        rigidbody.MovePosition(transform.position + (player.position - transform.position) * speed  * Time.deltaTime);
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
        rigidbody.MovePosition(transform.position + (Vector3.Cross(transform.up, (player.position - transform.position).normalized) * 3 + (player.position - transform.position).normalized) * attackSpeed  * Time.deltaTime);

        /*GameObject projectile = Instantiate(bullet, transform.position, Quaternion.identity);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = (player.position - transform.position).normalized * projectileSpeed -  0.5f * Physics.gravity * ((player.position - transform.position).magnitude)/projectileSpeed;
        Destroy(projectile, 2f);
        StartCoroutine(waitCooldown());*/

    }
    public void SpawnHitbox(){
        launchAttack(hitbox,transform.forward * 2 + transform.position);
    }
    private bool launchAttack(Collider other, Vector3 pos)
    {
        Collider[] cols = Physics.OverlapBox(pos, other.bounds.extents, transform.rotation);
        //GameObject visual = Instantiate(other.gameObject, pos, transform.rotation);
        bool didHit = false;
        foreach (Collider col in cols)
        {
            if (col.tag == "Player")
            {
                HurtBox hurtBox = col.transform.GetComponent<HurtBox>();
                if (hurtBox != null)
                {
                    hurtBox.TakeDamage(5);
                    didHit = true;
                }
            }
        }
        return didHit;
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
        if(enemyCount != null){
            TextMeshProUGUI text =enemyCount.GetComponent<TextMeshProUGUI>();
            int currentCount = Int32.Parse(text.text.Substring(13));
            if(currentCount ==1) SceneManager.LoadScene("Win");
            text.text = "Enemies Left:" + (currentCount-1);
        }
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
