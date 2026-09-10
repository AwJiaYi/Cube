using UnityEngine;

public class BossGarlan : BossBase
{
    [Header("加兰专属参数")]
    public float lightAttackDamage = 18f;
    public float heavyAttackDamage = 35f;
    public float phaseTwoSpeedBonus = 1.25f;

    private int currentAttackType = 0; // 0: 普通平砍, 1: 蓄力重斩

    protected override void Start()
    {
        bossName = "败誓之骑士·加兰";
        base.Start();
    }

    protected override void HandleMovementAndDecision()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            float currentSpeed = isPhaseTwo ? moveSpeed * phaseTwoSpeedBonus : moveSpeed;
            ChasePlayer(currentSpeed);
        }
        else
        {
            StartAttackPattern();
        }
    }

    private void StartAttackPattern()
    {
        currentState = BossState.Attacking;

        if (animator != null && HasParameter(animator, "Speed"))
        {
            animator.SetFloat(AnimSpeed, 0f);
        }

        // 转向玩家
        Vector3 targetDir = (player.position - transform.position).normalized;
        targetDir.y = 0;
        if (targetDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(targetDir);

        // 1. 决定攻击招式
        currentAttackType = (isPhaseTwo && Random.value > 0.4f) ? 1 : 0;
        string attackStateName = currentAttackType == 1 ? "Boss_Attack_Combo1" : "Boss_Attack_Combo0";

        // 2. 播放动画与安全检查
        if (animator != null)
        {
            // 💡 使用 CrossFade 替代 Play，过渡更平滑且不容易被过渡条件打断
            animator.CrossFade(attackStateName, 0.1f);
        }

        // 3. 设置攻击动作持续时长（如无帧事件，以此倒计时为准）
        stateTimer = (currentAttackType == 1) ? 1.2f : 0.85f;
    }

    protected override void OnEnterPhaseTwo()
    {
        base.OnEnterPhaseTwo();

        if (bossRenderer != null && bossRenderer.material.HasProperty("_Color"))
        {
            originalColor = new Color(0.8f, 0.1f, 0.2f);
            bossRenderer.material.color = originalColor;
        }
    }

    public override void OnAttackHitFrame()
    {
        if (player == null || currentState == BossState.Staggered) return;

        float damage = currentAttackType == 1 ? heavyAttackDamage : lightAttackDamage;
        float hitRadius = currentAttackType == 1 ? attackRange + 1.2f : attackRange;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= hitRadius)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null && !playerController.isDead)
            {
                if (playerController.isPerfectDodgeActive)
                {
                    playerController.TriggerPerfectDodgeReward();
                }
                else
                {
                    playerController.TakePlayerDamage(damage);
                }
            }
        }
    }
}