using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

public class PlayerController : MonoBehaviour
{
    private const bool V = false;
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
    private InputAction inputMove, attack1, attack2, lockOn, jump;
    private bool lockedIn;
    private Transform lockTarget;
    private int swordProgression = 0;
    private Animator animator;
    private states state = states.idle;
    private attackStates dirState = attackStates.still;
    private float timeSinceLastSwing = 0;
    private float highTime = 0;
    private bool isItHighTime = false;
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
        if((lockTarget.position - transform.position).magnitude > 2)
        move = transform.forward * 3;
        if (timeSinceLastSwing > 0.5)
        {
            swordProgression = 0;
        }
        timeSinceLastSwing = 0;
        launchAttack(hitboxes[0], transform.position + transform.forward * 2);
        switch (swordProgression)
        {
            case 0:
                break;
            case 1:
                break;
            case 2:
                break;
        }
        StartCoroutine(M1coroutine(0.33f));
    }
    private void AirAttack()
    {
        launchAttack(hitboxes[0], transform.position + transform.forward * 2);
        StartCoroutine(M1coroutine(0.33f));
        move /= 2;
        verticalVelocity += 8;
    }
    private void Stinger()
    {
        move = 15 * targetDir;
        launchAttack(hitboxes[3], transform.position + transform.forward * 5);
        StartCoroutine(M1coroutine(0.5f));
    }
    private void RisingStrike()
    {
        move = Vector3.zero;
        launchAttack(hitboxes[2], transform.position + transform.forward * 2 + Vector3.up * 1);
        StartCoroutine(RisingStrikeSupplement());
        StartCoroutine(M1coroutine(0.33f));
    }
    private void RollingAction()
    {
        StartCoroutine(RollingActionSupplement(transform.position + transform.forward*4));
    }
    private void HelmBringer()
    {
        StartCoroutine(M1coroutine(0.5f));
        move = Vector3.zero;
        launchAttack(hitboxes[2], transform.position + transform.forward * 2 + Vector3.down * 3);
        verticalVelocity = -5;
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
        if(isItHighTime && playerInput.Player.Attack1.IsPressed()) highTime += Time.deltaTime;
        targetDir = (lockTarget.position - transform.position).normalized;
        switch (state)
        {
            case states.idle:
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
                move *= speed;

                break;

        }
        if (lockedIn)
        {
            cam.transform.LookAt(lockTarget);
            transform.LookAt(new Vector3(lockTarget.position.x, transform.position.y, lockTarget.position.z));
            cam.transform.position += (transform.position + (10 - (lockTarget.position - transform.position).magnitude * 0.3f) * Vector3.Cross(transform.forward, transform.up) + Vector3.up * 2 - cam.transform.position) * 0.01f;
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
        }
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;
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

    private void checkAttack(InputAction.CallbackContext context){
        if(state == states.attacking) return;
        if(context.action.name == "Attack2" && !lockedIn)return;
        float dirValue = Vector3.Dot(new Vector3(move.x, 0, move.z).normalized, targetDir);
        if(dirValue > 0.707f) dirState = attackStates.forward;
        else if(dirValue < -0.707f) dirState = attackStates.back;
        else dirState = attackStates.still;
        Debug.Log(dirState + " | " + context.action.name + " | " + characterController.isGrounded + " | " + dirValue + " | " + attackDictionary[new Tuple<attackStates, String, bool>(dirState, context.action.name, characterController.isGrounded)].Method);
        state = states.attacking;
        attackDictionary[new Tuple<attackStates, String, bool>(dirState, context.action.name, characterController.isGrounded)]?.DynamicInvoke();
    }
    private void Lock(InputAction.CallbackContext context)
    {
        if (lockedIn)
        {
            lockTarget = transform;
            lockedIn = V;
            return;
        }
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, enemies[i].transform.position - transform.position, out hit, 20f) && hit.transform.tag == "Enemy" && hit.distance > (lockTarget.position - transform.position).magnitude)
            {
                lockTarget = enemies[i].transform;
                lockedIn = true;
            }
        }
    }
    private void Jump(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(gravity * -3 * jumpHeight);
        }
    }

    // helper functions

    private void launchAttack(Collider other, Vector3 pos)
    {
        Collider[] cols = Physics.OverlapBox(pos, other.bounds.extents, transform.rotation, 1 << 6);
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

    private IEnumerator RollingActionSupplement(Vector3 pos)
    {
        move= Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.1f);
            launchAttack(hitboxes[1], pos);
        }
        state = states.idle;
    }
    private IEnumerator SawSupplement()
    {
         move= Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.125f);
            launchAttack(hitboxes[0], transform.position +transform.forward);
        }
        state = states.idle;
    }
    private IEnumerator RisingStrikeSupplement(){
        isItHighTime = true;
        yield return new WaitForSeconds(0.2f);
        isItHighTime = false;
        if(highTime > 0.2f){
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
