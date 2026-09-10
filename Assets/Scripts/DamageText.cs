using UnityEngine;
using TMPro;

// 伤害类型枚举
public enum DamageType
{
    Normal,     // 普通伤害：白色、中字号、平滑上浮
    Critical,   // 暴击/破防：金黄色、大字号、带有轻微缩放冲击感
    Skill       // 技能大招：红橙色、特大字号、向上剧烈散开
}

public class DamageText : MonoBehaviour
{
    [Header("组件引用")]
    public TMP_Text textMesh;

    [Header("动画控制")]
    public float lifetime = 0.8f;
    private float timer = 0f;

    private Vector3 moveDirection;
    private float moveSpeed = 1.2f;
    private Color textColor;
    private Vector3 initialScale;

    private void Awake()
    {
        if (textMesh == null) textMesh = GetComponentInChildren<TMP_Text>();
        initialScale = transform.localScale;
    }

    public void Setup(float damageAmount, DamageType damageType)
    {
        if (textMesh == null) textMesh = GetComponentInChildren<TMP_Text>();
        if (textMesh == null) return;

        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        // 基础随机散射位移
        float randomX = Random.Range(-0.4f, 0.4f);
        moveDirection = new Vector3(randomX, 1.2f, 0f).normalized;

        // 根据伤害类型应用不同样式与效果
        switch (damageType)
        {
            case DamageType.Normal:
                textColor = Color.white;
                textMesh.fontSize = 32f;
                moveSpeed = 1.0f;
                break;

            case DamageType.Critical:
                textColor = new Color(1f, 0.85f, 0f); // 亮金色
                textMesh.fontSize = 46f;
                textMesh.text += "!";
                moveSpeed = 1.8f;
                // 暴击初始加成放大，产生Pop出屏效果
                transform.localScale = initialScale * 1.4f;
                break;

            case DamageType.Skill:
                textColor = new Color(1f, 0.3f, 0.1f); // 爆炎橙红
                textMesh.fontSize = 52f;
                textMesh.text = "CRIT " + textMesh.text;
                moveSpeed = 2.2f;
                transform.localScale = initialScale * 1.6f;
                break;
        }

        textMesh.color = textColor;

        // 面向相机
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    void Update()
    {
        if (textMesh == null) return;

        timer += Time.deltaTime;

        // 1. 位置上浮
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // 2. 暴击/大招的 Scale 弹跳收缩恢复 (Pop Effect)
        if (transform.localScale.x > initialScale.x)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, initialScale, Time.deltaTime * 10f);
        }

        // 3. 始终朝向相机
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        // 4. 后半段透明度淡出
        if (timer > lifetime * 0.4f)
        {
            float alpha = Mathf.Lerp(1f, 0f, (timer - lifetime * 0.4f) / (lifetime * 0.6f));
            textMesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
        }

        // 5. 销毁
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}