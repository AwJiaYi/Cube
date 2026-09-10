using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactManager : MonoBehaviour
{
    public static ImpactManager Instance { get; private set; }

    [Header("相机引用")]
    public Transform mainCamera;

    private Coroutine shakeCoroutine;
    private Coroutine hitStopCoroutine;
    private Vector3 originalCamPos;
    private bool isShaking = false;

    // 💡 安全机制：记录当前所有被卡肉冻结的 Animator，防止中断协程时速度无法恢复
    private readonly HashSet<Animator> frozenAnimators = new HashSet<Animator>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    /// <summary>
    /// 打击感触发接口
    /// </summary>
    public void TriggerImpact(float stopDuration = 0.08f, float shakeDuration = 0.15f, float shakeIntensity = 0.25f, Animator targetsAnimator = null, Animator attackerAnimator = null)
    {
        if (stopDuration > 0f)
        {
            DoHitStop(stopDuration, targetsAnimator, attackerAnimator);
        }

        if (shakeDuration > 0f && shakeIntensity > 0f)
        {
            DoCameraShake(shakeDuration, shakeIntensity);
        }
    }

    public void DoHitStop(float duration, Animator targetsAnimator = null, Animator attackerAnimator = null)
    {
        // 1. 如果有上一次没完成的卡肉，强行清算恢复速度，防止 anim.speed 卡死在 0
        ResetAllFrozenAnimators();

        if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);

        // 2. 收集本次需要冻结的 Animator
        if (targetsAnimator != null) frozenAnimators.Add(targetsAnimator);
        if (attackerAnimator != null) frozenAnimators.Add(attackerAnimator);

        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // 冻结所有目标的动画
        foreach (var anim in frozenAnimators)
        {
            if (anim != null) anim.speed = 0f;
        }

        Time.timeScale = 0.001f;

        yield return new WaitForSecondsRealtime(duration);

        // 恢复时间
        if (UIManager.Instance == null || !UIManager.Instance.isGameOver)
        {
            Time.timeScale = 1.0f;
        }

        // 恢复所有目标的动画速度
        ResetAllFrozenAnimators();
    }

    // 还原所有动画速度的安全方法
    private void ResetAllFrozenAnimators()
    {
        foreach (var anim in frozenAnimators)
        {
            if (anim != null) anim.speed = 1f;
        }
        frozenAnimators.Clear();
    }

    public void DoCameraShake(float duration, float intensity)
    {
        if (mainCamera == null) return;

        if (!isShaking)
        {
            originalCamPos = mainCamera.localPosition;
        }

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, intensity));
    }

    private IEnumerator ShakeRoutine(float duration, float intensity)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector3 randomOffset = Random.insideUnitSphere * intensity;
            randomOffset.z = 0f;

            mainCamera.localPosition = originalCamPos + randomOffset;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCamera.localPosition = originalCamPos;
        isShaking = false;
    }

    private void OnDisable()
    {
        Time.timeScale = 1.0f;
        ResetAllFrozenAnimators();
    }

    public void TriggerBulletTime(float timeScale = 0.2f, float duration = 0.5f)
    {
        StartCoroutine(BulletTimeRoutine(timeScale, duration));
    }

    private IEnumerator BulletTimeRoutine(float timeScale, float duration)
    {
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
    }
}