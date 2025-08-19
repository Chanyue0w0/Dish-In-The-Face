using UnityEngine;

public class GarbageOnGround : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player"; // 預設 Tag 名稱

    private PlayerMovement player;

    void Start()
    {
        transform.SetParent(RoundManager.Instance.ObstaclesGroup);

        // 找到場景中帶有特定 tag 的玩家，並取得其 PlayerMovement 腳本
        player = RoundManager.Instance.Player.GetComponent<PlayerMovement>();
        if (player == null)
        {
            Debug.LogWarning("Garbage 無法找到玩家物件！");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 檢查是否是玩家碰到
        if (other.CompareTag(playerTag))
        {
            // 如果玩家目前正在滑行，就刪除自己
            if (player.IsPlayerDash())
            {
                Destroy(gameObject);
            }
        }
    }
}
