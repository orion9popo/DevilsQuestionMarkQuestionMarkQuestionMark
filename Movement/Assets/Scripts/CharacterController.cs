// TODO
/* 

- finsh the FUCKING maze
- finish enemy
- HELP!!!

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TMPro;
using Unity.Mathematics;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    public GameObject handleBone;
    public GameObject[] VFX;
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
    private bool lockedIn, isItHighTime = false, isStinger = false, isAirborn, isHelmBringer = false;
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
            move = transform.forward * 2;
        if (timeSinceLastSwing > 1) swordProgression = 0;
        timeSinceLastSwing = 0;
        launchAttack(hitboxes[0], transform.position + transform.forward * 2);
        animator.SetInteger("SwordProgression", swordProgression);
        if (swordProgression < 2) swordProgression += 1;
        else {
            swordProgression = 0;
            StartCoroutine(M1coroutine(1));
            return;
        }
        StartCoroutine(M1coroutine(0.33f));
    }
    private void AirAttack()
    {
        move *= 0.5f;
        verticalVelocity += 8;
        if (timeSinceLastSwing > 1) swordProgression = 0;
        timeSinceLastSwing = 0;
        animator.SetInteger("SwordProgression", swordProgression);
        if (swordProgression < 2) swordProgression += 1;
        else{ 
            swordProgression = 0;
            verticalVelocity += 16;
            StartCoroutine(M1coroutine(0.7f));
            StartCoroutine(airAttack3Supplement());
            return;
        }
        launchAttack(hitboxes[0], transform.position + transform.forward * 2);
        StartCoroutine(M1coroutine(0.33f));
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
    }
    private void RollingAction()
    {
        StartCoroutine(RollingActionSupplement(transform.position + transform.forward * 4));
    }
    private void HelmBringer()
    {
        isHelmBringer = true;
        animator.SetBool("IsHelmBringer", true);
        state = states.attacking;
        move = Vector3.zero;
        verticalVelocity = 8;
        StartCoroutine(HelmBringerSupplement());
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
        if(lockTarget == null) lockTarget = transform;
        targetDir = (lockTarget.position - transform.position).normalized;
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
            if(state != states.attacking) animator.SetFloat("Vertical", oldDir.y);
            else animator.SetFloat("Vertical", 0);
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
        animator.SetBool("Grounded", characterController.isGrounded);
        if(verticalVelocity < -1 && !characterController.isGrounded) {

            animator.SetBool("Fall", true);
            if(characterController.isGrounded) verticalVelocity = 0;
        }
        else animator.SetBool("Fall", false);
        if(characterController.isGrounded){
            if(isAirborn){
                isAirborn = false;
                swordProgression = 0;
                StartCoroutine(flicker("Land"));
            }
        }
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
        Debug.Log(enemies);
        List<GameObject> markedEnemies = new List<GameObject>();
        float dist = Mathf.Infinity;
        Transform closetEnemy = transform;

        for (int i = 0; i < enemies.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, enemies[i].transform.position - transform.position, out hit, 20f) && hit.transform.tag == "Enemy" && hit.distance > (lockTarget.position - transform.position).magnitude)
            {
                markedEnemies.Add(enemies[i]);
                Debug.DrawLine(transform.position, hit.point, Color.red, 1f);
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
    }
    private void Jump(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded)
        {
            if(dirValueY >-0.706) StartCoroutine(delayAirborne());
            StartCoroutine(flicker("Jump"));
            swordProgression = 0;
            state = states.attacking;
            StartCoroutine(M1coroutine(0.2f));
            if(WishVertical > -0.706) verticalVelocity = Mathf.Sqrt(gravity * -3 * jumpHeight);
            else move = (lockTarget.position - transform.position).normalized * -10;
            StartCoroutine(jumpVFXm());
        }
    }

    // helper functions

    public void SwordVFX(){
        StartCoroutine(SwordVFXm());
    }
    public void sawSwordVFX(){
        StartCoroutine(sawSwordVFXm());
    }
    public void HelmBringerLandAnimEvent(){
        state = states.idle;
        isHelmBringer = false;
        StartCoroutine(IHATEMAKINGIENUMERATORS());
    }
    private IEnumerator IHATEMAKINGIENUMERATORS(){
        yield return new WaitForSeconds(0.3f);
        animator.SetBool("IsHelmBringer", false);
    }

    private IEnumerator sawSwordVFXm()
    {
        VFX[0].SetActive(true);
        Quaternion rot = handleBone.transform.rotation;
        VFX[0].transform.rotation = rot;
        VFX[0].transform.SetParent(handleBone.transform);
        VFX[0].transform.localPosition = Vector3.zero;
        yield return new WaitForSeconds(0.3f);
        VFX[0].transform.SetParent(null);
        VFX[0].SetActive(false);
    }
    private IEnumerator jumpVFXm(){
        VFX[1].SetActive(true);
        VFX[1].transform.position = transform.position - Vector3.up * 1.2f;
        yield return new WaitForSeconds(0.5f);
        VFX[1].SetActive(false);
    }

    private IEnumerator SwordVFXm(){
        VFX[0].SetActive(true);
        Vector3 pos = transform.position+ Vector3.up + transform.forward;
        Quaternion rot = handleBone.transform.rotation ;
        VFX[0].transform.position = pos;
        VFX[0].transform.rotation = rot;
        yield return new WaitForSeconds(0.3f);
        VFX[0].SetActive(false);
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
                if (hurtBox.TakeDamage(10) && col.transform == lockTarget) { 
                    lockedIn = false;
                    Lock(new InputAction.CallbackContext());
                 }
                didHit = true;
            }
        }
        return didHit;
    }
    private IEnumerator delayAirborne(){
        yield return new WaitForEndOfFrame();
        isAirborn = true;
    }

    private IEnumerator flicker(string trigger)
    {
        animator.SetTrigger(trigger);
        yield return new WaitForSeconds(0.1f);
        animator.ResetTrigger(trigger);
    }
    private IEnumerator HelmBringerSupplement(){
        yield return new WaitForSeconds(0.16666f);
        launchAttack(hitboxes[2], transform.position + transform.forward * 2 + Vector3.down * 3);
        verticalVelocity = -20;
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
            verticalVelocity = 16;
            highTime = 0;
            StartCoroutine(M1coroutine(0.133f));
            yield return new WaitForSeconds(0.3f);
            StartCoroutine(delayAirborne());
            yield break;
        }
        StartCoroutine(M1coroutine(0.5f));
        highTime = 0;
    }
    private IEnumerator airAttack3Supplement(){
        yield return new WaitForSeconds(0.3333f);
        launchAttack(hitboxes[0], transform.position + transform.forward * 2);
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
