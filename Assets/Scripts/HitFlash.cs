using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [Header("闪白设置")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f; // 闪白持续时间（秒）

    private Renderer[] renderers;
    private Color[][] originalColors;
    private Coroutine flashCoroutine;

    void Awake()
    {
        // 获取当前物体及其所有子物体的 Renderer（兼容复杂模型结构）
        renderers = GetComponentsInChildren<Renderer>();

        // 记录所有材质的原始颜色
        originalColors = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            originalColors[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    originalColors[i][j] = mats[j].color;
                }
            }
        }
    }

    public void CallHitFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        // 1. 将所有材质设为闪白颜色
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    mats[j].color = flashColor;
                }
            }
        }

        // 2. 挂起极短的时间
        yield return new WaitForSeconds(flashDuration);

        // 3. 还原原始颜色
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    mats[j].color = originalColors[i][j];
                }
            }
        }
    }
}