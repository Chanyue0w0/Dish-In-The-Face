using UnityEngine;

public class GarbageOnGround : MonoBehaviour
{
    [Header("�]�w Player �� Tag")]
    [SerializeField] private string playerTag = "Player"; // �w�] Tag �W��

    private PlayerMovement player;

    void Start()
    {
        // ���������a���S�w tag �����a�A�è��o�� PlayerMovement �}��
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogWarning("Garbage �L�k���a�� tag �����a����I");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // �ˬd�O�_�O���a�I��
        if (player != null && other.CompareTag(playerTag))
        {
            // �p�G���a�ثe���b�Ʀ�A�N�R���ۤv
            if (player.IsPlayerDash())
            {
                Destroy(gameObject);
            }
        }
    }
}
