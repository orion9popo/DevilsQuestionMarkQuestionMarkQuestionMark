// TODO
/* 

- finsh the FUCKING maze
- finish enemy
- HELP!!!

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
enum states
{
    idle,
    running,
    attacking,
    dying
}
enum attackStates
{
    forward,
    back,
    still
}

public class PlayerController : MonoBehaviour
{
    public float speed = 6.0f;
    public float turnSmoothTime = 0.1f;
    public float jumpHeight = 1f;
    public float sensitivity = -1f;
    public Camera cam;
    public PlayerInputActions playerInput;
    public Collider[] hitboxes;
    public GameObject handleBone;
    public ParticleSystem[] VFX;
    new Rigidbody rigidbody;
    HurtBox hurtBox;
    private float turnSmoothVelocity;
    private float x, y;
    private Vector2 input = new Vector2();
    private Vector3 move = new Vector3(), rotate;
    private Vector3 targetDirection = new Vector3();
    private InputAction inputMove;
    private bool lockedIn, isItHighTime = false, IsNotLooking = false, isAirborn, isHelmBringer = false;
    private Transform lockTarget;
    private int attackProgression = 0, equippedWeapon = 0;
    private Animator animator;
    private states state = states.idle;
    private attackStates dirState = attackStates.still;
    private float timeSinceLastSwing = 0;
    private float highTime = 0;
    private float dirValueY = 0, dirValueX = 0, oldDirM = 0, WishVertical = 0;
    private Vector2 oldDir = Vector2.zero;
    
    Dictionary<Tuple<attackStates, string, bool>, Delegate>[] attackDictionary = new Dictionary<Tuple<attackStates, string, bool>, Delegate>[2];

    // attack delagate pointers (used for attack dictionary)

    private delegate void SwordBasicAttackDelegate();
    private delegate void SwordRaveDelegate();
    private delegate void StingerDelegate();
    private delegate void RollingActionDelegate();
    private delegate void HelmBringerDelegate();
    private delegate void SawDelegate();
    private delegate void RisingStrikeDelegate();
    private delegate void RollingThunderDelegate();
    private delegate void TauntDelegate();
    private delegate void AirRollingThunderDelegate();
    private delegate void GauntletBasicAttackDelegate();
    private delegate void GauntletRaveDelegate();
    private delegate void SpitDelegate();
    private delegate void TumultuousEarthDelegate();
    private delegate void FreeReignDelegate();
    private delegate void UpperCutDelegate();
    private delegate void BlastDelegate();

    // Attacks
    private void SwordBasicAttack()
    {
        if ((lockTarget.position - transform.position).magnitude > 2)
            move = transform.forward;
        if (timeSinceLastSwing > 1) attackProgression = 0;
        timeSinceLastSwing = 0;
        launchAttack(hitboxes[0], transform.position + transform.forward * 2, 10, transform.forward * 150);
        animator.SetInteger("AttackProgression", attackProgression);
        if (attackProgression < 2) attackProgression += 1;
        else attackProgression = 0;
        StartCoroutine(CoolDownCoroutine(0.33f));
        Debug.Log(IsNotLooking);
    }
    private void SwordRave()
    {
        move *= 0.5f;
        rigidbody.velocity += Vector3.up * 3;
        if (timeSinceLastSwing > 1) attackProgression = 0;
        timeSinceLastSwing = 0;
        animator.SetInteger("AttackProgression", attackProgression);
        if (attackProgression < 2) attackProgression += 1;
        else
        {
            attackProgression = 0;
            rigidbody.velocity += Vector3.up * 4;
            StartCoroutine(CoolDownCoroutine(0.7f));
            StartCoroutine(SwordRave3Supplement());
            return;
        }
        launchAttack(hitboxes[0], transform.position + transform.forward * 2, 10, Vector3.zero);
        StartCoroutine(CoolDownCoroutine(0.33f));
    }
    private void Stinger()
    {
        move = 5 * targetDirection;
        launchAttack(hitboxes[3], transform.position + transform.forward * 5, 10, transform.forward * 400);
        StartCoroutine(CoolDownCoroutine(0.5f));
        StartCoroutine(StingerSupplement());
    }
    private void RisingStrike()
    {
        move = Vector3.zero;
        attackProgression = 0;
        launchAttack(hitboxes[2], transform.position + transform.forward * 2 + Vector3.up * 1, 10, Vector3.up * 800);
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
        rigidbody.velocity = new Vector3(rigidbody.velocity.x, -10, rigidbody.velocity.z);
        StartCoroutine(HelmBringerSupplement());
    }
    private void Saw()
    {
        StartCoroutine(SawSupplement());
    }
    private void RollingThunder()
    {
        
    }
    private void AirRollingThunder()
    {
        rigidbody.velocity = new Vector3(rigidbody.velocity.x, 10, rigidbody.velocity.z);
        StartCoroutine(AirRollingThunderSupplement());
    }
    private void GauntletBasicAttack()
    {

    }
    private void GauntletRave()
    {

    }
    private void Spit()
    {

    }
    private void TumultuousEarth()
    {

    }
    private void FreeReign()
    {

    }
    private void UpperCut()
    {

    }
    private void Blast(){

    }
     private void Taunt(InputAction.CallbackContext context)
    {
        state = states.attacking;
        StartCoroutine(CoolDownCoroutine(2));
        StartCoroutine(flicker("Taunt"));
    }
    private void switchWeapon(InputAction.CallbackContext context){
        if(context.ReadValue<float>() == attackDictionary.Length-1)
            equippedWeapon = equippedWeapon == 0 ? attackDictionary.Length-1 : equippedWeapon - 1;
        else
            equippedWeapon = equippedWeapon == attackDictionary.Length-1 ? 0 : equippedWeapon + 1;
        Debug.Log(equippedWeapon);
    }
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        hurtBox = GetComponent<HurtBox>();
        lockTarget = transform;

        attackDictionary[0] = new();
        attackDictionary[1] = new();

        // Sword attacks

        SwordBasicAttackDelegate BAD = new SwordBasicAttackDelegate(SwordBasicAttack);
        SwordRaveDelegate AAD = new SwordRaveDelegate(SwordRave);
        StingerDelegate SD = new StingerDelegate(Stinger);
        RollingActionDelegate RAD = new RollingActionDelegate(RollingAction);
        SawDelegate SaD = new SawDelegate(Saw);
        RisingStrikeDelegate RSD = new RisingStrikeDelegate(RisingStrike);
        HelmBringerDelegate HBD = new HelmBringerDelegate(HelmBringer);
        RollingThunderDelegate RTD = new RollingThunderDelegate(RollingThunder);
        AirRollingThunderDelegate ARTD = new AirRollingThunderDelegate(AirRollingThunder);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack1", true), BAD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack1", false), AAD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack1", false), AAD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack1", false), AAD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack1", true), SD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack2", true), RTD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack2", true), RAD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack2", true), SaD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack1", true), RSD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack2", false), ARTD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack2", false), HBD);
        attackDictionary[0].Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack2", false), HBD);

        //Gauntlet attacks

        GauntletBasicAttackDelegate GBAD = new(GauntletBasicAttack);
        GauntletRaveDelegate GRD = new(GauntletRave);
        SpitDelegate SpD = new(Spit);
        TumultuousEarthDelegate TED= new(TumultuousEarth);
        FreeReignDelegate FRD = new(FreeReign);
        BlastDelegate BD = new(Blast);
        UpperCutDelegate UCD = new(UpperCut);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack1", true), GBAD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack1", false), GRD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack2", true), TED);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.still, "Attack2", false), SpD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack1", true), GBAD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack1", false), FRD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack2", true), TED);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.forward, "Attack2", false), SpD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack1", true), UCD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack1", false), GRD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack2", true), BD);
        attackDictionary[1].Add(new Tuple<attackStates, string, bool>(attackStates.back, "Attack2", false), SpD);
    }



    // Movement
    void Update()
    {
        if (state == states.dying) return;
        input = inputMove.ReadValue<Vector2>();
        timeSinceLastSwing += Time.deltaTime;
        if (isItHighTime && playerInput.Player.Attack1.IsPressed()) highTime += Time.deltaTime;
        if (lockTarget == null) lockTarget = transform;
        targetDirection = (lockTarget.position - transform.position).normalized;
        switch (state)
        {
            case states.idle:
                WishVertical = 0;
                animator.SetFloat("WishVertical", WishVertical);
                VFX[2].gameObject.SetActive(false);
                //VFX[2].Play();
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
                if (!isAirborn) VFX[2].gameObject.SetActive(true);
                float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                move = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                if (input.magnitude < 0.1)
                {
                    move = new Vector3(0, 0, 0);
                    state = states.idle;
                    return;
                }

                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 Mmove = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
                move = (Mmove * input.y - Vector3.Cross(Mmove, cam.transform.up) * input.x).normalized;
                WishVertical = Vector3.Dot(new Vector3(move.x, 0, move.z).normalized, new Vector3(targetDirection.x, 0, targetDirection.z).normalized);
                if (lockedIn && dirState == attackStates.back) move *= speed * 0.5f;
                else move *= speed;

                break;

        }
        if (lockedIn)
        {
            cam.transform.LookAt(lockTarget);
            if (IsNotLooking == false)
                transform.LookAt(new Vector3(lockTarget.position.x, transform.position.y, lockTarget.position.z));
            cam.transform.position += (transform.position + (10 - (lockTarget.position - transform.position).magnitude * 0.3f) * Vector3.Cross(transform.forward, transform.up) + Vector3.up * 2 - cam.transform.position) * 0.01f;
            dirValueY = Vector3.Dot(new Vector3(move.x, 0, move.z).normalized, targetDirection);
            dirValueX = Vector3.Dot(new Vector3(move.x, 0, move.z).normalized, Vector3.Cross(targetDirection, transform.up));
            oldDir.y += (dirValueY - oldDir.y) * 0.1f;
            oldDir.x += (dirValueX - oldDir.x) * 0.1f;
            if (state != states.attacking) animator.SetFloat("Vertical", oldDir.y);
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
        animator.SetBool("Grounded", IsGrounded());
        if (rigidbody.velocity.y < 0 && !IsGrounded())
        {
            animator.SetBool("Fall", true);
        }
        else animator.SetBool("Fall", false);
        if (isAirborn && IsGrounded())
        {
            attackProgression = 0;
            StartCoroutine(flicker("Land"));
        }
        isAirborn = !IsGrounded();
        compensateForWalls(transform.position, cam.transform.position);
    }
    public void FixedUpdate()
    {
        rigidbody.MovePosition(transform.position + move * speed * Time.fixedDeltaTime);
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
        playerInput.Player.Taunt.performed += Taunt;
        playerInput.Player.WeaponSwitch.performed += switchWeapon;
        //playerInput.Player.MouseWheel.performed += mouseScroll;
        playerInput.Enable();
    }

    void OnDisable()
    {
        playerInput.Disable();
        playerInput.Player.Attack1.performed -= checkAttack;
        playerInput.Player.Attack2.performed -= checkAttack;
        playerInput.Player.LockOn.performed -= Lock;
        playerInput.Player.Jump.performed -= Jump;
        playerInput.Player.Taunt.performed -= Taunt;
    }
    /*private void mouseScroll(InputAction.CallbackContext value)
    {
        float scroll = value.ReadValue<Vector2>().y * 0.01f;
        cam.transform.position += cam.transform.forward * scroll;
    }*/

   
    private void compensateForWalls(Vector3 start, Vector3 to)
    {
        RaycastHit hit;
        if (Physics.Raycast(start, (to - start).normalized, out hit, 11.5f, 1 << 7))
        {
            cam.transform.position = hit.point;
        }
    }

    private void checkAttack(InputAction.CallbackContext context)
    {
        if (state == states.attacking || state == states.dying) return;
        if (context.action.name == "Attack2" && !lockedIn) return;
        state = states.attacking;
        StartCoroutine(flicker(context.action.name));
        attackDictionary[equippedWeapon][new Tuple<attackStates, String, bool>(dirState, context.action.name, IsGrounded())]?.DynamicInvoke();
        Debug.Log(attackDictionary[equippedWeapon][new Tuple<attackStates, String, bool>(dirState, context.action.name, IsGrounded())]);
    }

    private void Lock(InputAction.CallbackContext context)
    {
        if (state == states.dying) return;
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
        if (IsGrounded() && state != states.dying)
        {
            StartCoroutine(flicker("Jump"));
            attackProgression = 0;
            state = states.attacking;
            StartCoroutine(CoolDownCoroutine(0.2f));
            if (WishVertical > -0.706) rigidbody.velocity = new Vector3(rigidbody.velocity.x,math.sqrt( Physics.gravity.magnitude * 3 * jumpHeight), rigidbody.velocity.z);
            else move =  targetDirection * -4;
            StartCoroutine(jumpVFXm());
        }
    }
    public void death()
    {
        SceneManager.LoadScene("Death");
    }
    public void startDeath()
    {
        gameObject.tag = "Dying";
        state = states.dying;
    }

    // helper functions

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.38f);
    }

    public void SwordVFX()
    {
        StartCoroutine(SwordVFXm());
    }
    public void sawSwordVFX()
    {
        StartCoroutine(sawSwordVFXm());
    }
    public void HelmBringerLandAnimEvent()
    {
        isHelmBringer = false;
        animator.SetBool("IsHelmBringer", false);
        state = states.attacking;
        StartCoroutine(IHATEMAKINGIENUMBERATORS());
    }
    private IEnumerator IHATEMAKINGIENUMBERATORS(){
        yield return new WaitForSeconds(0.2f);
        state = states.idle;
    }
    private IEnumerator sawSwordVFXm()
    {
        VFX[0].gameObject.SetActive(true);
        Quaternion rot = handleBone.transform.rotation;
        VFX[0].transform.rotation = rot;
        VFX[0].transform.SetParent(handleBone.transform);
        VFX[0].transform.localPosition = Vector3.zero;
        yield return new WaitForSeconds(0.3f);
        VFX[0].transform.SetParent(null);
        VFX[0].gameObject.SetActive(false);
    }
    private IEnumerator jumpVFXm()
    {
        VFX[1].gameObject.SetActive(true);
        VFX[1].transform.position = transform.position - Vector3.up * 1.2f;
        yield return new WaitForSeconds(0.5f);
        VFX[1].gameObject.SetActive(false);
    }

    private IEnumerator SwordVFXm()
    {
        VFX[0].gameObject.SetActive(true);
        Vector3 pos = transform.position + Vector3.up + transform.forward;
        Quaternion rot = handleBone.transform.rotation;
        VFX[0].transform.position = pos;
        VFX[0].transform.rotation = rot;
        yield return new WaitForSeconds(0.3f);
        VFX[0].gameObject.SetActive(false);
    }

    private bool launchAttack(Collider other, Vector3 pos, float damage, Vector3 knockback)
    {
        Collider[] cols = Physics.OverlapBox(pos, other.bounds.extents, transform.rotation);
        bool didHit = false;
        foreach (Collider col in cols)
        {
            if (col.tag == tag)
                continue;

            HurtBox hurtBox = col.transform.GetComponent<HurtBox>();
            if (hurtBox != null)
            {
                hurtBox.TakeKnockback(knockback);
                if (hurtBox.TakeDamage(damage) && col.transform == lockTarget)
                {
                    lockedIn = false;
                    lockTarget.tag = "Dying";
                    StartCoroutine(delaytag());
                }
                didHit = true;
            }
        }
        return didHit;
    }
    private IEnumerator delaytag()
    {
        yield return new WaitForSeconds(0.1f);
        Lock(new InputAction.CallbackContext());
    }
    private IEnumerator delayAirborne()
    {
        yield return new WaitForEndOfFrame();
        isAirborn = true;
    }

    private IEnumerator flicker(string trigger)
    {
        animator.SetTrigger(trigger);
        yield return new WaitForSeconds(0.1f);
        animator.ResetTrigger(trigger);
    }
    private IEnumerator HelmBringerSupplement()
    {
        yield return new WaitForSeconds(0.16666f);
        launchAttack(hitboxes[2], transform.position + transform.forward * 2 + Vector3.down * 3, 15, -Vector3.up * 350 + transform.forward * 150);
        rigidbody.velocity = new Vector3(rigidbody.velocity.x, -20, rigidbody.velocity.z);
    }
    private IEnumerator StingerSupplement()
    {
        IsNotLooking = true;
        yield return new WaitForSeconds(0.5f);
        IsNotLooking = false;
    }
    private IEnumerator RollingActionSupplement(Vector3 pos)
    {
        move = Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.1f);
            launchAttack(hitboxes[1], pos, 5, Vector3.up * 400);
        }
        state = states.idle;
    }
    private IEnumerator SawSupplement()
    {
        move = Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.125f);
            launchAttack(hitboxes[0], transform.position + transform.forward, 5, Vector3.up * 400);
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
            rigidbody.velocity = new Vector3(rigidbody.velocity.x, math.sqrt(Physics.gravity.magnitude * 7), rigidbody.velocity.z);
            highTime = 0;
            StartCoroutine(CoolDownCoroutine(0.133f));
            yield return new WaitForSeconds(0.3f);
            StartCoroutine(delayAirborne());
            yield break;
        }
        StartCoroutine(CoolDownCoroutine(0.5f));
        highTime = 0;
    }
    private IEnumerator SwordRave3Supplement()
    {
        yield return new WaitForSeconds(0.3333f);
        launchAttack(hitboxes[0], transform.position + transform.forward * 2, 10, Vector3.zero);
    }
    private IEnumerator AirRollingThunderSupplement(){
        IsNotLooking = true;
        while(!IsGrounded()){
            yield return new WaitForSeconds(0.1f);
            Debug.Log(IsNotLooking);
            launchAttack(hitboxes[1], transform.position,5, transform.forward * 3 + Vector3.up * 400);
        }
        Debug.Log("done");
        rigidbody.velocity = Vector3.zero;
        move = Vector3.zero;
        state = states.idle;
        IsNotLooking = false;
    }
    private IEnumerator DestoryHitbox(GameObject hitbox)
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(hitbox);
    }
    private IEnumerator CoolDownCoroutine(float time)
    {
        yield return new WaitForSeconds(time);
        state = states.idle;
        move = Vector3.zero;
    }
    
}


