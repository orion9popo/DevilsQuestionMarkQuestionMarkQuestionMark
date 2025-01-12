// TODO
/* 

- implent animations
- finish rest of moves
- finsh the FUCKING maze
- finish enemy
- HELP!!!

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.XR.WSA;
using Debug = UnityEngine.Debug;

public class PlayerController : MonoBehaviour
{
    public float gravity = -90.81f;
    public float speed = 6.0f;
    public float turnSmoothTime = 0.1f;
    public float jumpHeight = 1f;
    public float sensitivity = -1f;
    public Camera cam;
    public PlayerInputActions playerInput;
    public Collider[] hitboxes;
    CharacterController characterController;
    private float turnSmoothVelocity;
    private float verticalVelocity = -1;
    private Vector3 rotate;
    private float x;
    private float y;
    private Vector2 input = new Vector2();
    private Vector3 move = new Vector3();
    private Vector3 targetDir = new Vector3();
    private InputAction inputMove;
    private bool lockedIn, isItHighTime = false, isStinger = false, isAirborn;
    private Transform lockTarget;
    private int swordProgression = 0;
    private Animator animator;
    private states state = states.idle;
    private attackStates dirState = attackStates.still;
    private float timeSinceLastSwing = 0;
    private float highTime = 0;
    private float dirValueY = 0, dirValueX = 0, oldDirM = 0, WishVertical = 0;
    private Vector2 oldDir = Vector2.zero;

    enum states
    {
        idle,
        running,
        attacking
    }
    enum attackStates
    {
        forward,
        back,
        still
    }
    Dictionary<Tuple<attackStates, string, bool>, Delegate> attackDictionary = new Dictionary<Tuple<attackStates, string, bool>, Delegate>();

    // attack delagate pointers (used for attack dictionary)

    private delegate void BasicAttackDelegate();
    private delegate void AirAttackDelegate();
    private delegate void StingerDelegate();
    private delegate void RollingActionDelegate();
    private delegate void HelmBringerDelegate();
    private delegate void SawDelegate();
    private delegate void RisingStrikeDelegate();

    // Attacks
    private void BasicAttack()
    {
        if ((lockTarget.position - transform.position).magnitude > 2)
            move = transform.forward * 3;
        if (timeSinceLastSwing > 1) swordProgression = 0;
        timeSinceLastSwing = 0;
        launchAttack(hitboxes[0], transform.position + transform.forward * 2);
        StartCoroutine(M1coroutine(0.33f));
        animator.SetInteger("SwordProgression", swordProgression);
        Debug.Log(swordProgression);
        if (swordProgression < 2) swordProgression += 1;
        else swordProgression = 0;
    }
    private void AirAttack()
    {
        launchAttack(hitboxes[0], transform.position + transform.forward * 2);
        StartCoroutine(M1coroutine(0.33f));
        move *= 0.5f;
        verticalVelocity += 8;
        if (timeSinceLastSwing > 1) swordProgression = 0;
        timeSinceLastSwing = 0;
        animator.SetInteger("SwordProgression", swordProgression);
        if (swordProgression < 2) swordProgression += 1;
        else swordProgression = 0;
    }
    private void Stinger()
    {
        move = 15 * targetDir;
        launchAttack(hitboxes[3], transform.position + transform.forward * 5);
        StartCoroutine(M1coroutine(0.5f));
        StartCoroutine(StingerSupplement());
    }
    private void RisingStrike()
    {
        move = Vector3.zero;
        swordProgression = 0;

        launchAttack(hitboxes[2], transform.position + transform.forward * 2 + Vector3.up * 1);
        StartCoroutine(RisingStrikeSupplement());
        StartCoroutine(M1coroutine(0.33f));
    }
    private void RollingAction()
    {
        StartCoroutine(RollingActionSupplement(transform.position + transform.forward * 4));
    }
    private void HelmBringer()
    {
        StartCoroutine(M1coroutine(0.5f));
        move = Vector3.zero;
        launchAttack(hitboxes[2], transform.position + transform.forward * 2 + Vector3.down * 3);
        verticalVelocity = -20;
    }
    private void Saw()
    {
        StartCoroutine(SawSupplement());
    }
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        lockTarget = transform;
        BasicAttackDelegate BAD = new BasicAttackDelegate(BasicAttack);
        AirAttackDelegate AAD = new AirAttackDelegate(AirAttack);
        StingerDelegate SD = new StingerDelegate(Stinger);
        RollingActionDelegate RAD = new RollingActionDelegate(RollingAction);
        SawDelegate SaD = new SawDelegate(Saw);
        RisingStrikeDelegate RSD = new RisingStrikeDelegate(RisingStrike);
        HelmBringerDelegate HBD = new HelmBringerDelegate(HelmBringer);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack1", true), BAD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack1", false), AAD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack1", false), AAD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack1", false), AAD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack1", true), SD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack2", true), RAD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack2", true), RAD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack2", true), SaD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack1", true), RSD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack2", false), HBD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack2", false), HBD);
        attackDictionary.Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack2", false), HBD);
    }

    // Movement
    void Update()
    {
        input = inputMove.ReadValue<Vector2>();
        timeSinceLastSwing += Time.deltaTime;
        if (isItHighTime && playerInput.Player.Attack1.IsPressed()) highTime += Time.deltaTime;
        animator.SetBool("Grounded", characterController.isGrounded);
        targetDir = (lockTarget.position - transform.position).normalized;
        if(characterController.isGrounded && isAirborn){
            isAirborn = false;
            StartCoroutine(flicker("Land"));
        }
        switch (state)
        {
            case states.idle:
                WishVertical = 0;
                animator.SetFloat("WishVertical", WishVertical);
                if (input.magnitude > 0.1)
                {
                    state = states.running;
                    dirState = attackStates.still;
                    return;
                }
                break;

            case states.attacking:

                break;

            case states.running:

                float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                move = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                if (input.magnitude < 0.1)
                {
                    move = new Vector3(0, verticalVelocity, 0);
                    state = states.idle;
                    return;
                }

                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                move = (cam.transform.forward * input.y - Vector3.Cross(cam.transform.forward, cam.transform.up) * input.x).normalized;
                WishVertical = Vector3.Dot(new Vector3(move.x, 0, move.z).normalized, targetDir);
                if (lockedIn && dirState == attackStates.back) move *= speed * 0.5f;
                else move *= speed;

                break;

        }
        if (lockedIn)
        {
            cam.transform.LookAt(lockTarget);
            if (isStinger == false)
                transform.LookAt(new Vector3(lockTarget.position.x, transform.position.y, lockTarget.position.z));
            cam.transform.position += (transform.position + (10 - (lockTarget.position - transform.position).magnitude * 0.3f) * Vector3.Cross(transform.forward, transform.up) + Vector3.up * 2 - cam.transform.position) * 0.01f;
            dirValueY = Vector3.Dot(new Vector3(move.x, 0, move.z).normalized, targetDir);
            dirValueX = Vector3.Dot(new Vector3(move.x, 0, move.z).normalized, Vector3.Cross(targetDir, transform.up));
            oldDir.y += (dirValueY - oldDir.y) * 0.1f;
            oldDir.x += (dirValueX - oldDir.x) * 0.1f;
            animator.SetFloat("Vertical", oldDir.y);
            animator.SetFloat("Horizontal", oldDir.x);
            animator.SetFloat("WishVertical", WishVertical);
            if (WishVertical > 0.707f) dirState = attackStates.forward;
            else if (WishVertical < -0.707f) dirState = attackStates.back;
            else dirState = attackStates.still;
        }
        else
        {
            if (Input.GetMouseButton(1))
            {
                y = Input.GetAxis("Mouse X");
                x = Input.GetAxis("Mouse Y");
                rotate = new Vector3(x, y * sensitivity, 0);
                cam.transform.eulerAngles = cam.transform.eulerAngles - rotate * 4;
            }
            cam.transform.position = transform.position - 10 * cam.transform.forward + Vector3.up * 2;
            oldDirM += (input.magnitude - oldDirM) * 0.1f;
            animator.SetFloat("Horizontal", 0);
            animator.SetFloat("Vertical", oldDirM);
            animator.SetFloat("WishVertical", 0);
        }
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;
        if(verticalVelocity < -1 && !characterController.isGrounded) animator.SetBool("Fall", true);
        else animator.SetBool("Fall", false);

        characterController.Move(move * Time.deltaTime);
    }

    void Awake()
    {
        playerInput = new PlayerInputActions();
    }
    private void OnEnable()
    {
        inputMove = playerInput.Player.Move;
        playerInput.Player.Attack1.performed += checkAttack;
        playerInput.Player.Attack2.performed += checkAttack;
        playerInput.Player.LockOn.performed += Lock;
        playerInput.Player.Jump.performed += Jump;
        playerInput.Enable();
    }
    void OnDisable()
    {
        playerInput.Disable();
        playerInput.Player.Attack1.performed -= checkAttack;
        playerInput.Player.Attack2.performed -= checkAttack;
        playerInput.Player.LockOn.performed -= Lock;
        playerInput.Player.Jump.performed -= Jump;
    }

    private void checkAttack(InputAction.CallbackContext context)
    {
        if (state == states.attacking) return;
        if (context.action.name == "Attack2" && !lockedIn) return;
        state = states.attacking;
        StartCoroutine(flicker(context.action.name));
        attackDictionary[new Tuple<attackStates, String, bool>(dirState, context.action.name, characterController.isGrounded)]?.DynamicInvoke();
    }

    private void Lock(InputAction.CallbackContext context)
    {
        if (lockedIn)
        {
            lockTarget = transform;
            lockedIn = false;
            return;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<GameObject> markedEnemies = new List<GameObject>();
        float dist = Mathf.Infinity;
        Transform closetEnemy = transform;

        for (int i = 0; i < enemies.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, enemies[i].transform.position - transform.position, out hit, 20f) && hit.transform.tag == "Enemy" && hit.distance > (lockTarget.position - transform.position).magnitude)
            {
                markedEnemies.Add(enemies[i]);
                lockedIn = true;
            }
        }
        if (markedEnemies.Count == 0) return;
        for (int i = 0; i < markedEnemies.Count; i++)
        {
            if (dist > (markedEnemies[i].transform.position - transform.position).magnitude)
            {
                dist = (markedEnemies[i].transform.position - transform.position).magnitude;
                closetEnemy = markedEnemies[i].transform;
            }
        }
        lockTarget = closetEnemy;
        Debug.Log(closetEnemy.name);
    }
    private void Jump(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded)
        {
            isAirborn = true;
            StartCoroutine(flicker("Jump"));
            swordProgression = 0;
            state = states.attacking;
            StartCoroutine(M1coroutine(0.2f));
            if(WishVertical > -0.706) verticalVelocity = Mathf.Sqrt(gravity * -3 * jumpHeight);
            else move = (lockTarget.position - transform.position).normalized * -10;
        }
    }

    // helper functions


    private void launchAttack(Collider other, Vector3 pos)
    {
        Collider[] cols = Physics.OverlapBox(pos, other.bounds.extents, transform.rotation);
        GameObject visual = Instantiate(other.transform.gameObject, pos, transform.rotation);
        StartCoroutine(DestoryHitbox(visual));
        foreach (Collider col in cols)
        {
            if (col.tag == tag)
                continue;

            HurtBox hurtBox = col.transform.GetComponent<HurtBox>();
            if (hurtBox != null)
            {
                Debug.Log(col.name + " hit");
                if (hurtBox.TakeDamage(10) && col.transform == lockTarget) { lockedIn = false; lockTarget = transform; }
            }
        }
    }


    private IEnumerator flicker(string trigger)
    {
        animator.SetTrigger(trigger);
        Debug.Log(trigger);
        yield return new WaitForSeconds(0.1f);
        animator.ResetTrigger(trigger);
    }
    private IEnumerator StingerSupplement()
    {
        isStinger = true;
        yield return new WaitForSeconds(0.5f);
        isStinger = false;
    }
    private IEnumerator RollingActionSupplement(Vector3 pos)
    {
        move = Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.1f);
            launchAttack(hitboxes[1], pos);
        }
        state = states.idle;
    }
    private IEnumerator SawSupplement()
    {
        move = Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.125f);
            launchAttack(hitboxes[0], transform.position + transform.forward);
        }
        state = states.idle;
    }
    private IEnumerator RisingStrikeSupplement()
    {
        isItHighTime = true;
       
        yield return new WaitForSeconds(0.2f);
        isItHighTime = false;
        if (highTime > 0.2f)
        {
            isAirborn = true;
            verticalVelocity = 16;
        }
        Debug.Log(highTime);
        highTime = 0;
    }
    private IEnumerator DestoryHitbox(GameObject hitbox)
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(hitbox);
    }
    private IEnumerator M1coroutine(float time)
    {
        yield return new WaitForSeconds(time);
        state = states.idle;
        move = Vector3.zero;
    }
}
