using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

public class PlayerController : MonoBehaviour
{

    public float gravity = -90.81f;
    public float speed = 6.0f;
    public float turnSmoothTime = 0.1f;
    public float jumpHeight = 1f;
    public float sensitivity = -1f;
    public Camera cam;
    public float cap = 10;
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
    private InputAction inputMove, attack1, attack2, lockOn, jump;
    private bool lockedIn;
    private Transform lockTarget;
    private int swordProgression = 0;
    private Animator animator;
    private states state = states.idle;
    private float timeSinceLastSwing= 0;
    enum states
    {
        idle,
        running,
        attacking
    }


    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        lockTarget = transform;
    }
    // Movement
    void Update()
    {
        input = inputMove.ReadValue<Vector2>();
        timeSinceLastSwing += Time.deltaTime;
        switch (state)
        {

            case states.idle:
                if (input.magnitude > 0.1)
                {
                    state = states.running;
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
            cam.transform.position = transform.position + 7 * Vector3.Cross(transform.forward, transform.up) + Vector3.up * 2 ;
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
    public void damage()
    {

    }



    void Awake()
    {
        playerInput = new PlayerInputActions();
    }
    private void OnEnable()
    {
        inputMove = playerInput.Player.Move;
        playerInput.Player.Attack1.performed += M1;
        playerInput.Player.Attack2.performed += M2;
        playerInput.Player.LockOn.performed += Lock;
        playerInput.Player.Jump.performed += Jump;
        playerInput.Enable();
    }
    void OnDisable()
    {
        playerInput.Disable();
        playerInput.Player.Attack1.performed -= M1;
        playerInput.Player.Attack2.performed -= M2;
        playerInput.Player.LockOn.performed -= Lock;
        playerInput.Player.Jump.performed -= Jump;
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
    private void Jump(InputAction.CallbackContext context){
        Debug.Log("jump");
        if (characterController.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(gravity * -3 * jumpHeight);
            }
    }

    private void M2(InputAction.CallbackContext context)
    {
        if(!characterController.isGrounded) return;
        Vector3 targetdir = (lockTarget.position - transform.position).normalized;
        if(lockedIn && Vector3.Dot(new Vector3(move.x, 0 , move.z).normalized, targetdir) < -0.766f){
            StartCoroutine(M2BackCoroutine(transform.position + transform.forward * 4));
        }

    }

    private void M1(InputAction.CallbackContext context)
    {
        if(!characterController.isGrounded)return;
        if(state == states.attacking) return;
        
        state = states.attacking;
        Vector3 targetdir = (lockTarget.position - transform.position).normalized;
        if(lockedIn && Vector3.Dot(new Vector3(move.x, 0 , move.z).normalized, targetdir)< -0.766f){
            move = Vector3.zero;
            verticalVelocity = 8;
            StartCoroutine(M1coroutine(0.33f));
            return;
        }
        if(lockedIn && Vector3.Dot(new Vector3(move.x, 0 , move.z).normalized, targetdir)> 0.766f){
            move =  15 * targetdir;
            StartCoroutine(M1coroutine(0.5f));
            return;
        }
        move = transform.forward*3;
        if(timeSinceLastSwing > 0.5){
            timeSinceLastSwing = 0;
            swordProgression = 0;
        }
        launchAttack(hitboxes[0], transform.position + transform.forward * 2);
        switch(swordProgression){
            case 0:
            break;
            case 1:
            break;
            case 2:
            break;
        }
        StartCoroutine(M1coroutine(0.33f));
    }
    private void launchAttack(Collider other, Vector3 pos){
        Collider[] cols = Physics.OverlapBox(pos, other.bounds.extents, transform.rotation, 1 << 6);
        GameObject visual = Instantiate(other.transform.gameObject, pos, transform.rotation);
        StartCoroutine(DestoryHitbox(visual));
        foreach (Collider col in cols)
        {
            if(col.tag == tag)
                continue;

            HurtBox hurtBox = col.transform.GetComponent<HurtBox>();
            if(hurtBox != null){
                Debug.Log(col.name + " hit");
                if(hurtBox.TakeDamage(10) && col.transform == lockTarget) {lockedIn = false; lockTarget = transform;}
            }
        }
    }

    private IEnumerator M2BackCoroutine(Vector3 pos){
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.1f);
            launchAttack(hitboxes[1], pos);
        }
    }
    private IEnumerator DestoryHitbox(GameObject hitbox){
        yield return new WaitForSeconds(0.2f);
        Destroy(hitbox);
    }
    private IEnumerator M1coroutine(float time){
        yield return new WaitForSeconds(time);
        state = states.idle;
        move = Vector3.zero;
    }
}
