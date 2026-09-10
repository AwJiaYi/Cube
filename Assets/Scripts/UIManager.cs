using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine; // 如果使用的是 Cinemachine v3，需要加这行；v2 则用 Cinemachine

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("玩家 UI 引用")]
    public RectTransform playerHealthMask;
    private float playerMaxBarWidth = 250f;

    [Header("Boss UI 引用")]
    public GameObject bossHUDPanel;
    public RectTransform bossHealthMask;
    public RectTransform bossStaggerMask;
    private float bossMaxBarWidth = 500f;

    [Header("结算 UI 面板")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    [Header("击杀特写镜头设置")]
    [Tooltip("绑定场景中的 Cinemachine Virtual Camera")]
    public CinemachineCamera vcam; // 如果是 Cinemachine v2，类型写 CinemachineVirtualCamera
    public float killCamFOV = 30f;   // 特写拉近后的 FOV（越小镜头越放大）
    public float zoomSpeed = 5f;     // 镜头拉近的速度

    public bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 记录面板初始宽度
        if (playerHealthMask != null) playerMaxBarWidth = playerHealthMask.sizeDelta.x;
        if (bossHealthMask != null) bossMaxBarWidth = bossHealthMask.sizeDelta.x;
        if (bossStaggerMask != null) bossMaxBarWidth = bossStaggerMask.sizeDelta.x;
    }

    private void Update()
    {
        // 游戏结束后，按下 R 键快速重启场景
        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1.0f; // 恢复正常时间流速
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void UpdatePlayerHealth(float current, float max)
    {
        if (playerHealthMask != null)
        {
            float targetWidth = Mathf.Clamp01(current / max) * playerMaxBarWidth;
            playerHealthMask.sizeDelta = new Vector2(targetWidth, playerHealthMask.sizeDelta.y);
        }
    }

    public void UpdateBossUI(float currentHP, float maxHP, float currentStagger, float maxStagger)
    {
        if (bossHUDPanel != null && !bossHUDPanel.activeSelf)
        {
            bossHUDPanel.SetActive(true);
        }

        if (bossHealthMask != null)
        {
            float targetWidth = Mathf.Clamp01(currentHP / maxHP) * bossMaxBarWidth;
            bossHealthMask.sizeDelta = new Vector2(targetWidth, bossHealthMask.sizeDelta.y);
        }

        if (bossStaggerMask != null)
        {
            float targetWidth = Mathf.Clamp01(currentStagger / maxStagger) * bossMaxBarWidth;
            bossStaggerMask.sizeDelta = new Vector2(targetWidth, bossStaggerMask.sizeDelta.y);
        }
    }

    public void HideBossUI()
    {
        if (bossHUDPanel != null) bossHUDPanel.SetActive(false);
    }

    #region 结算与重置触发

    public void ShowVictoryScreen()
    {
        if (isGameOver) return;
        isGameOver = true;
        StartCoroutine(VictorySequence());
    }

    public void ShowDefeatScreen()
    {
        if (isGameOver) return;
        isGameOver = true;
        StartCoroutine(DefeatSequence());
    }

    private IEnumerator VictorySequence()
    {
        isGameOver = true;
        HideBossUI();

        Debug.Log("<color=cyan><b>【触发慢动作特写！】开始慢镜头与镜头拉近</b></color>");

        // 保存原有的 FOV
        float originalFOV = 60f;
        if (vcam != null)
        {
            originalFOV = vcam.Lens.FieldOfView; // Cinemachine v2 请写 vcam.m_Lens.FieldOfView
        }

        float timer = 0f;
        float slowDuration = 1.5f; // 慢镜头特写持续 1.5 秒（真实时间）

        while (timer < slowDuration)
        {
            // 1. 强行锁定慢动作
            Time.timeScale = 0.1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // 2. 动态平滑拉近镜头 (FOV 变小)
            if (vcam != null)
            {
                vcam.Lens.FieldOfView = Mathf.Lerp(vcam.Lens.FieldOfView, killCamFOV, Time.unscaledDeltaTime * zoomSpeed);
                // 注意：如果是 Cinemachine v2，上一行请写：
                // vcam.m_Lens.FieldOfView = Mathf.Lerp(vcam.m_Lens.FieldOfView, killCamFOV, Time.unscaledDeltaTime * zoomSpeed);
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 慢动作结束，恢复正常时间
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // 恢复原有 FOV（如果需要保持放大直到界面出来，也可以不恢复）
        if (vcam != null)
        {
            vcam.Lens.FieldOfView = originalFOV; // v2 为 vcam.m_Lens.FieldOfView
        }

        Debug.Log("<color=green><b>【慢动作结束】恢复正常</b></color>");

        if (victoryPanel != null) victoryPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator DefeatSequence()
    {
        // 稍微等待，给玩家死亡动画或受击反馈留出渲染时间
        yield return new WaitForSeconds(1.0f);

        if (defeatPanel != null) defeatPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion
}