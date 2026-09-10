using UnityEngine;

public class AnimationRelay : MonoBehaviour
{
    private PlayerController playerController;

    void Start()
    {
        // 自动向父节点查找 PlayerController 组件
        playerController = GetComponentInParent<PlayerController>();
    }

    // 这个方法会暴露给 Animation Event 的下拉菜单
    public void OnAttackHitFrame()
    {
        if (playerController != null)
        {
            playerController.OnAttackHitFrame();
        }
    }
}