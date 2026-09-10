using UnityEngine;

public class TacticalBomb : MonoBehaviour
{
    [Header("爆弹基础参数")]
    public float baseDamage = 25f;       // 基础伤害
    public float staggerDamage = 40f;    // 基础削韧
    public float explosionRadius = 3.5f; // 爆炸物理半径
    public float knockbackForce = 6f;    // 击退力

    [Header("目标图层")]
    public LayerMask enemyLayer;

    private bool hasExploded = false;

    private void OnCollisionEnter(Collision collision)
    {
        // 避免重复触发
        if (hasExploded) return;

        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        Debug.Log("<color=orange>💥 战术爆弹引爆！</color>");

        // 1. 范围球体检测
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            // 判定 A：普通小怪靶子 (EnemyDummy)
            EnemyDummy dummy = enemy.GetComponent<EnemyDummy>();
            if (dummy != null)
            {
                Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized;
                knockbackDir.y = 0.2f; // 带一点微微向上弹起的效果

                dummy.TakeDamage(baseDamage, knockbackDir, knockbackForce);
            }

            // 判定 B：Boss
            BossBase boss = enemy.GetComponent<BossBase>();
            if (boss != null)
            {
                float finalStagger = staggerDamage;
                float finalDamage = baseDamage;

                // 💡 联动机制：使用状态机检查 Boss 是否处于瘫痪/失衡状态
                if (boss.currentState == BossBase.BossState.Staggered)
                {
                    finalStagger *= 1.8f; // 爆破强力破防
                    finalDamage *= 1.5f;
                    Debug.Log("<color=yellow>✨ 触发联动！对瘫痪/易伤状态下的 Boss 造成额外爆破伤害！</color>");
                }

                Vector3 attackDir = (enemy.transform.position - transform.position).normalized;
                boss.TakeBossDamage(finalDamage, finalStagger, attackDir);
            }
        }

        // 2. 销毁爆弹
        Destroy(gameObject);
    }

    // 在 Scene 视口画出爆炸范围，方便调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}