using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    private CinemachineImpulseSource impulseSource;

    [Header("移动与视角设置")]
    public float moveSpeed = 6.0f;
    [Range(0.01f, 1.0f)]
    public float turnSmoothTime = 0.15f;
    private float turnSmoothVelocity;
    public float gravity = 20.0f;
    private float verticalVelocity = 0f;

    [Header("相机目标设置")]
    public Transform cameraTarget;

    [Header("冲刺/闪避设置")]
    public float dashSpeed = 18.0f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 0.8f;
    public float perfectDodgeWindow = 0.15f;
    public bool isPerfectDodgeActive = false;

    [Header("攻击与连招设置")]
    public Transform attackPoint;
    public float attackRange = 1.2f;
    public float knockbackForce = 8f;
    public LayerMask enemyLayer;
    public GameObject hitVFXPrefab;

    private int comboIndex = 0;
    private bool inputBuffer = false;
    private bool canComboAdvance = false;
    private float comboResetTimer = 0f;
    public float comboResetTime = 1.0f;

    [Header("Q技能：针刺突刺 (Thrust)")]
    public float qSkillDamage = 10f;
    public float qSkillDashDistance = 3f;
    public float qSkillCooldown = 4.0f;
    private bool canQSkill = true;

    [Header("玩家基础数值")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("E 技能：战术爆弹")]
    public GameObject bombPrefab;
    public Transform throwPoint;
    public float throwForce = 12f;
    public float throwUpwardForce = 4f;
    public float eSkillCooldown = 5f;
    private bool canESkill = true;

    // 状态控制变量
    private bool isDashing = false;
    private bool canDash = true;
    public bool isInvulnerable = false;
    public bool isDead = false; // 💡 死亡标志位

    private Vector3 dashDirection = Vector3.zero;
    private bool isAttacking = false;

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerHealth(currentHealth, maxHealth);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (isDead) return; // 💡 阵亡后停止一切逻辑

        HandleInput();

        if (comboIndex > 0 && !isAttacking)
        {
            comboResetTimer += Time.deltaTime;
            if (comboResetTimer >= comboResetTime)
            {
                ResetCombo();
            }
        }

        if (isDashing)
        {
            ExecuteDash();
        }
        else
        {
            HandleMove();
        }
    }

    void LateUpdate()
    {
        if (cameraTarget != null)
        {
            cameraTarget.position = transform.position + Vector3.up * 1.2f;
            cameraTarget.rotation = Quaternion.identity;
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            StopAllCoroutines();
            StartCoroutine(PerformDash());
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!isAttacking)
            {
                StartCoroutine(PerformComboAttack());
            }
            else if (canComboAdvance)
            {
                inputBuffer = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) && canQSkill && !isAttacking)
        {
            StartCoroutine(PerformQSkill());
        }

        if (Input.GetKeyDown(KeyCode.E) && canESkill)
        {
            StartCoroutine(PerformESkill());
        }
    }

    private void HandleMove()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 moveVelocity = Vector3.zero;

        if (inputDir.magnitude >= 0.1f && !isAttacking)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            moveVelocity = moveDir.normalized * moveSpeed;
        }

        if (animator != null)
        {
            float currentSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            animator.SetFloat("Speed", currentSpeed);
        }

        moveVelocity.y = verticalVelocity;
        controller.Move(moveVelocity * Time.deltaTime);
    }

    #region 三段连招系统 (Combo System)

    public void OnAttackHitFrame()
    {
        float damage = 12f;
        float staggerDamage = 10f;
        float knockback = knockbackForce * 0.5f;

        if (comboIndex == 2)
        {
            damage = 28f;
            staggerDamage = 30f;
            knockback = knockbackForce * 1.5f;
        }

        ExecuteHitDetection(damage, staggerDamage, knockback, comboIndex);
    }

    private IEnumerator PerformComboAttack()
    {
        isAttacking = true;
        inputBuffer = false;
        canComboAdvance = false;
        comboResetTimer = 0f;

        if (animator != null)
        {
            if (HasAnimatorParameter(animator, "ComboIndex"))
            {
                animator.SetInteger("ComboIndex", comboIndex);
            }
            animator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(0.15f);
        canComboAdvance = true;

        yield return new WaitForSeconds(0.2f);

        if (inputBuffer && comboIndex < 2)
        {
            comboIndex++;
            StartCoroutine(PerformComboAttack());
        }
        else
        {
            canComboAdvance = false;
            yield return new WaitForSeconds(0.1f);
            isAttacking = false;

            if (comboIndex >= 2)
            {
                ResetCombo();
            }
        }
    }

    private void ResetCombo()
    {
        comboIndex = 0;
        isAttacking = false;
        canComboAdvance = false;
        inputBuffer = false;
        comboResetTimer = 0f;
        if (animator != null) animator.SetInteger("ComboIndex", 0);
    }

    private void ExecuteHitDetection(float damage, float staggerDamage, float knockback, int currentCombo)
    {
        if (attackPoint == null) return;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        bool hitAnything = false;

        foreach (Collider enemy in hitEnemies)
        {
            Vector3 attackDir = (enemy.transform.position - transform.position).normalized;

            BossBase boss = enemy.GetComponent<BossBase>();
            if (boss != null)
            {
                boss.TakeBossDamage(damage, staggerDamage, attackDir);
                hitAnything = true;
            }

            EnemyDummy dummy = enemy.GetComponent<EnemyDummy>();
            if (dummy != null)
            {
                attackDir.y = 0;
                dummy.TakeDamage(damage, attackDir, knockback);
                hitAnything = true;
            }

            if (hitVFXPrefab != null)
            {
                Instantiate(hitVFXPrefab, enemy.bounds.center, Quaternion.identity);
            }
        }

        if (hitAnything && ImpactManager.Instance != null)
        {
            if (currentCombo == 2)
            {
                ImpactManager.Instance.TriggerImpact(0.12f, 0.25f, 0.35f, null, animator);
            }
            else
            {
                ImpactManager.Instance.TriggerImpact(0.05f, 0.1f, 0.15f, null, animator);
            }

            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(currentCombo == 2 ? 1.0f : 0.4f);
            }
        }
    }

    #endregion

    #region 闪避与极限闪避 (Perfect Dodge)

    private IEnumerator PerformDash()
    {
        canDash = false;
        isDashing = true;
        isInvulnerable = true;
        isPerfectDodgeActive = true;

        if (animator != null) animator.SetBool("IsDashing", true);

        Renderer playerRend = GetComponentInChildren<Renderer>();
        Color playerOldColor = Color.white;
        if (playerRend != null && playerRend.material.HasProperty("_Color"))
        {
            playerOldColor = playerRend.material.color;
            playerRend.material.color = Color.white;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputVector = new Vector3(horizontal, 0, vertical).normalized;

        if (inputVector != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(inputVector.x, inputVector.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            dashDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            transform.rotation = Quaternion.LookRotation(dashDirection);
        }
        else
        {
            dashDirection = transform.forward;
        }

        yield return new WaitForSeconds(perfectDodgeWindow);
        isPerfectDodgeActive = false;

        yield return new WaitForSeconds(dashDuration - perfectDodgeWindow);

        if (playerRend != null && playerRend.material.HasProperty("_Color"))
        {
            playerRend.material.color = playerOldColor;
        }

        isDashing = false;
        isInvulnerable = false;
        if (animator != null) animator.SetBool("IsDashing", false);

        yield return new WaitForSeconds(dashCooldown - dashDuration);
        canDash = true;
    }

    private void ExecuteDash()
    {
        controller.Move(dashDirection * dashSpeed * Time.deltaTime);
    }

    public void TriggerPerfectDodgeReward()
    {
        Debug.Log("<color=lime><b>【极限闪避 Perfect Dodge！】触发子弹时间！刷新技能！</b></color>");

        if (ImpactManager.Instance != null)
        {
            ImpactManager.Instance.TriggerBulletTime(0.2f, 0.5f);
        }

        canQSkill = true;

        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.SpawnDamageText(transform.position + Vector3.up * 1.5f, 0, DamageType.Critical);
        }
    }

    #endregion

    #region 技能与受击 / 死亡

    private IEnumerator PerformQSkill()
    {
        canQSkill = false;
        isAttacking = true;

        if (animator != null) animator.SetTrigger("QSkill");

        Vector3 thrustDir = transform.forward;
        float elapsed = 0f;
        float dashTime = 0.1f;

        while (elapsed < dashTime)
        {
            controller.Move(thrustDir * (qSkillDashDistance / dashTime) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ExecuteHitDetection(qSkillDamage, 20f, knockbackForce * 0.5f, 1);

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;

        yield return new WaitForSeconds(qSkillCooldown - 0.2f);
        canQSkill = true;
    }

    private IEnumerator PerformESkill()
    {
        canESkill = false;

        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position + transform.forward * 0.8f + Vector3.up * 1.2f;

        if (bombPrefab != null)
        {
            GameObject bomb = Instantiate(bombPrefab, spawnPos, transform.rotation);
            Rigidbody bombRb = bomb.GetComponent<Rigidbody>();

            if (bombRb != null)
            {
                Vector3 forceDirection = Camera.main.transform.forward * throwForce + Vector3.up * throwUpwardForce;
                bombRb.AddForce(forceDirection, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(eSkillCooldown);
        canESkill = true;
    }

    public void TakePlayerDamage(float damage)
    {
        if (isInvulnerable || isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerHealth(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        if (animator != null)
        {
            if (HasAnimatorParameter(animator, "Die")) animator.SetTrigger("Die");
        }

        Debug.Log("<color=red><b>玩家阵亡！</b></color>");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDefeatScreen();
        }
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}