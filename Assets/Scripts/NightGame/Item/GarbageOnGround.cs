using UnityEngine;

public class GarbageOnGround : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player"; // �w�] Tag �W��

    private PlayerMovement player;

    void Start()
    {
        transform.SetParent(RoundManager.Instance.ObstaclesGroup);

        // ���������a���S�w tag �����a�A�è��o�� PlayerMovement �}��
        player = RoundManager.Instance.Player.GetComponent<PlayerMovement>();
        if (player == null)
        {
            Debug.LogWarning("Garbage �L�k��쪱�a����I");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // �ˬd�O�_�O���a�I��
        if (other.CompareTag(playerTag))
        {
            // �p�G���a�ثe���b�Ʀ�A�N�R���ۤv
            if (player.IsPlayerDash())
            {
                Destroy(gameObject);
            }
        }
    }
}
