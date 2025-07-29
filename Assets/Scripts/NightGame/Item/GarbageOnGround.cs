using UnityEngine;

public class GarbageOnGround : MonoBehaviour
{
    [Header("設定 Player 的 Tag")]
    [SerializeField] private string playerTag = "Player"; // 預設 Tag 名稱

    private PlayerMovement player;

    void Start()
    {
        // 找到場景中帶有特定 tag 的玩家，並取得其 PlayerMovement 腳本
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogWarning("Garbage 無法找到帶有 tag 的玩家物件！");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 檢查是否是玩家碰到
        if (player != null && other.CompareTag(playerTag))
        {
            // 如果玩家目前正在滑行，就刪除自己
            if (player.IsPlayerDash())
            {
                Destroy(gameObject);
            }
        }
    }
}
