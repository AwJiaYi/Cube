using System.Collections;
using UnityEngine;

public class EnemyDummy : MonoBehaviour
{
    public float health = 100f;
    private Renderer myRenderer;
    private Color originalColor;
    private Rigidbody rb;

    // 新增：易伤状态
    public bool isVulnerable = false;
    private float vulnerableMultiplier = 1.5f; // 易伤状态下受到 1.5 倍伤害

    void Start()
    {
        myRenderer = GetComponent<Renderer>();
        if (myRenderer != null) originalColor = myRenderer.material.color;

        // 如果没有指定 Rigidbody，则自动添加
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void TakeDamage(float damage, Vector3 knockbackDir, float knockbackForce)
    {
        // 如果处于易伤状态，触发 1.5 倍伤害
        float finalDamage = isVulnerable ? damage * vulnerableMultiplier : damage;

        health -= finalDamage;
        Debug.Log($"小怪受到 {finalDamage} 点伤害！{(isVulnerable ? "(触发易伤!)" : "")} 剩余血量：{health}");

        if (rb != null)
        {
            rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
        }

        StartCoroutine(FlashEffect());

        if (health <= 0)
        {
            Destroy(gameObject, 0.1f);
        }
        // 💡 新增：根据是否易伤触发不同强度的卡肉 + 震屏
        if (ImpactManager.Instance != null)
        {
            if (isVulnerable)
            {
                // 暴击/易伤重击：卡肉 0.06秒，震屏 0.15秒，强度 0.25
                ImpactManager.Instance.TriggerImpact(stopDuration: 0.06f, shakeDuration: 0.15f, shakeIntensity: 0.25f);
            }
            else
            {
                // 普通打击：微卡肉 0.03秒，微震屏 0.08秒，强度 0.08
                ImpactManager.Instance.TriggerImpact(stopDuration: 0.03f, shakeDuration: 0.08f, shakeIntensity: 0.08f);
            }
        }
        // 💡 修复位置：将原来的 bool 值 (false) 替换为对应的 DamageType 枚举
        if (DamageTextManager.Instance != null)
        {
            // 如果处在易伤状态，飘字类型选 Critical (暴击/大字)；否则选 Normal (普通)
            DamageType damageType = isVulnerable ? DamageType.Critical : DamageType.Normal;

            // 记得传入实际扣血的 finalDamage，而不是基础 damage
            DamageTextManager.Instance.SpawnDamageText(transform.position, finalDamage, damageType);
        }
    }

    // 挂载易伤状态（持续一定时间）
    public void ApplyVulnerable(float duration)
    {
        StartCoroutine(VulnerableRoutine(duration));
    }

    private IEnumerator VulnerableRoutine(float duration)
    {
        isVulnerable = true;
        // 易伤状态下变黄色作为提示
        if (myRenderer != null) myRenderer.material.color = Color.yellow;

        yield return new WaitForSeconds(duration);

        isVulnerable = false;
        if (myRenderer != null) myRenderer.material.color = originalColor;
    }

    private IEnumerator FlashEffect()
    {
        if (myRenderer != null)
        {
            // 如果处于易伤状态受击变橙色，普通变白色
            myRenderer.material.color = isVulnerable ? new Color(1f, 0.5f, 0f) : Color.white;
            yield return new WaitForSeconds(0.08f);
            myRenderer.material.color = isVulnerable ? Color.yellow : originalColor;
        }
    }
}