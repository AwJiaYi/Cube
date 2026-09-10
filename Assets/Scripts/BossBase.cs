using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public abstract class BossBase : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Chasing,
        Attacking,
        Cooldown,
        Staggered,
        Dead
    }

    [Header("Boss 基础数值")]
    public string bossName = "Boss";
    public float maxHealth = 1000f;
    public float currentHealth;

    [Header("韧性条 (Stagger/Armor) 机制")]
    public float maxStagger = 200f;
    public float currentStagger;
    public float staggerRecoverRate = 10f;

    [Header("二阶段机制")]
    public bool isPhaseTwo = false;
    [Range(0.1f, 0.9f)]
    public float phaseTwoHealthThreshold = 0.5f;

    [Header("通用战斗参数")]
    public float moveSpeed = 3.5f;
    public float attackRange = 2.5f;
    public float attackCooldown = 1.2f;

    [Header("基础引用")]
    public Transform player;
    protected CharacterController controller;
    protected Animator animator;
    protected Renderer bossRenderer;
    protected Color originalColor;

    [Header("受击配置")]
    public float hitCooldown = 0.15f;

    [Header("状态监控 (Debug - Inspector 可见)")]
    public BossState currentState = BossState.Idle;
    [SerializeField] protected bool isInvulnerable = false;

    protected float stateTimer = 0f;

    protected static readonly int AnimSpeed = Animator.StringToHash("Speed");
    protected static readonly int AnimIsPhaseTwo = Animator.StringToHash("IsPhaseTwo");

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        currentStagger = maxStagger;
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        bossRenderer = GetComponentInChildren<Renderer>();
        if (bossRenderer != null && bossRenderer.material.HasProperty("_Color"))
            originalColor = bossRenderer.material.color;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        UpdateBossUI();
    }

    protected virtual void Update()
    {
        if (currentState == BossState.Dead || player == null) return;

        // 💡 防卡死保护：确保 Animator 速度不会被卡肉机制永久锁定在 0
        if (animator != null && animator.speed < 0.1f && currentState != BossState.Dead)
        {
            // 如果不在卡肉瞬间，自动恢复播放速度
            animator.speed = 1f;
        }

        RecoverStagger();
        UpdateStateMachine();
    }

    protected virtual void UpdateStateMachine()
    {
        switch (currentState)
        {
            case BossState.Idle:
            case BossState.Chasing:
                HandleMovementAndDecision();
                break;

            case BossState.Attacking:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    OnAttackFinished();
                }
                break;

            case BossState.Cooldown:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    currentState = BossState.Idle;
                }
                else
                {
                    RotateTowardsPlayer();
                }
                break;

            case BossState.Staggered:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    RecoverFromStagger();
                }
                break;
        }
    }

    protected abstract void HandleMovementAndDecision();

    protected virtual void OnAttackFinished()
    {
        float cd = isPhaseTwo ? attackCooldown * 0.5f : attackCooldown;
        currentState = BossState.Cooldown;
        stateTimer = cd;

        if (animator != null)
        {
            animator.CrossFade("Boss_Idle", 0.1f);
        }
    }

    protected virtual void RecoverStagger()
    {
        if (currentState != BossState.Staggered && currentStagger < maxStagger)
        {
            currentStagger += staggerRecoverRate * Time.deltaTime;
            currentStagger = Mathf.Min(currentStagger, maxStagger);
            UpdateBossUI();
        }
    }

    public virtual void TakeBossDamage(float damage, float staggerDamage, Vector3 attackDirection = default)
    {
        if (currentState == BossState.Dead || isInvulnerable) return;

        // 1. 扣除韧性
        if (currentState != BossState.Staggered)
        {
            currentStagger -= staggerDamage;
            if (currentStagger <= 0)
            {
                TriggerStaggerState();
                return;
            }
            else
            {
                StartCoroutine(HitCooldownCoroutine());
            }
        }

        // 2. 结算伤害
        float damageMultiplier = (currentState == BossState.Staggered) ? 2.0f : 0.8f;
        float finalDamage = damage * damageMultiplier;
        currentHealth -= finalDamage;

        Debug.Log($"<color=cyan><b>[Boss受击]</b> 受到伤害: {finalDamage} | 剩余血量: {currentHealth} | 当前状态: {currentState}</color>");

        CheckPhaseTwoTrigger();

        // 3. 受击微幅位移
        if (controller != null && attackDirection != Vector3.zero)
        {
            Vector3 pushDir = attackDirection.normalized;
            pushDir.y = 0;
            float pushForce = (currentState == BossState.Staggered) ? 0.15f : 0.03f;
            controller.Move(pushDir * pushForce);
        }

        TriggerHitImpact();
        TriggerHitFlash();
        TriggerDamageText(finalDamage);
        UpdateBossUI();

        if (currentHealth <= 0)
        {
            OnBossDeath();
        }
    }

    protected virtual void TriggerStaggerState()
    {
        currentState = BossState.Staggered;
        stateTimer = 3.5f; // 强行打断动作，瘫痪 3.5 秒
        isInvulnerable = false;

        Debug.Log("<color=yellow><b>⚠️ Boss 韧性爆条！进入瘫痪状态 (Staggered)</b></color>");

        if (animator != null)
        {
            animator.CrossFade("Boss_Stagger_Start", 0.1f);
        }

        if (bossRenderer != null) bossRenderer.material.color = Color.blue;
    }

    protected virtual void RecoverFromStagger()
    {
        currentStagger = maxStagger;
        currentState = BossState.Idle;

        Debug.Log("<color=green><b>✨ Boss 从瘫痪中恢复！</b></color>");

        if (animator != null)
        {
            animator.CrossFade("Boss_Idle", 0.1f);
        }

        if (bossRenderer != null) bossRenderer.material.color = originalColor;
        UpdateBossUI();
    }

    protected virtual IEnumerator HitCooldownCoroutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(hitCooldown);
        isInvulnerable = false;
    }

    protected virtual void CheckPhaseTwoTrigger()
    {
        if (!isPhaseTwo && (currentHealth / maxHealth) <= phaseTwoHealthThreshold)
        {
            isPhaseTwo = true;

            if (animator != null && HasParameter(animator, "IsPhaseTwo"))
            {
                animator.SetBool(AnimIsPhaseTwo, true);
            }

            OnEnterPhaseTwo();
        }
    }

    protected virtual void OnEnterPhaseTwo()
    {
        Debug.Log($"<color=red><b>【{bossName} 触发二阶段狂暴！】</b></color>");
    }

    protected virtual void TriggerHitImpact()
    {
        Animator playerAnim = player != null ? player.GetComponentInChildren<Animator>() : null;

        if (ImpactManager.Instance != null)
        {
            if (currentState == BossState.Staggered)
                ImpactManager.Instance.TriggerImpact(0.08f, 0.15f, 0.25f, animator, playerAnim);
            else
                ImpactManager.Instance.TriggerImpact(0.03f, 0.05f, 0.08f, animator, playerAnim);
        }
    }

    protected virtual void TriggerHitFlash()
    {
        HitFlash flashScript = GetComponent<HitFlash>();
        if (flashScript != null)
            flashScript.CallHitFlash();
        else
            StartCoroutine(HitFlashCoroutine());
    }

    private IEnumerator HitFlashCoroutine()
    {
        if (bossRenderer != null && bossRenderer.material.HasProperty("_Color"))
        {
            bossRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.06f);
            bossRenderer.material.color = (currentState == BossState.Staggered) ? Color.blue : originalColor;
        }
    }

    protected virtual void TriggerDamageText(float finalDamage)
    {
        if (DamageTextManager.Instance != null)
        {
            DamageType type = (currentState == BossState.Staggered) ? DamageType.Critical : (finalDamage > 20f ? DamageType.Skill : DamageType.Normal);
            DamageTextManager.Instance.SpawnDamageText(transform.position, finalDamage, type);
        }
    }

    protected virtual void UpdateBossUI()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateBossUI(currentHealth, maxHealth, currentStagger, maxStagger);
    }

    protected virtual void OnBossDeath()
    {
        currentState = BossState.Dead;
        StopAllCoroutines();
        Debug.Log($"<b><color=yellow>{bossName} 被击败！</color></b>");

        if (animator != null)
        {
            animator.CrossFade("Boss_Die", 0.1f);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowVictoryScreen();

        Destroy(gameObject, 3.5f);
    }

    protected virtual void ChasePlayer(float speed)
    {
        currentState = BossState.Chasing;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        controller.Move(direction * speed * Time.deltaTime);

        if (animator != null && HasParameter(animator, "Speed"))
        {
            float currentSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            animator.SetFloat(AnimSpeed, currentSpeed);
        }

        RotateTowardsPlayer();
    }

    protected virtual void RotateTowardsPlayer()
    {
        if (player == null) return;
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }
    }

    protected bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    #region 动画关键帧事件接口
    public virtual void OnAttackHitFrame() { }
    public virtual void OnAttackEndFrame()
    {
        if (currentState == BossState.Attacking)
        {
            OnAttackFinished();
        }
    }
    #endregion
}