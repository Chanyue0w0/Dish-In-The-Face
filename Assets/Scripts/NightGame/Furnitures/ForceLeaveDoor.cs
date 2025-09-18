using UnityEngine;
using PrimeTween;

public class ForceLeaveDoor : MonoBehaviour
{
    private static readonly int IsOpen = Animator.StringToHash("isOpen");

    [Header("Setting")]
    [SerializeField] private bool attackToOpen = false;
    [SerializeField] private Vector2 outDirection;
    [SerializeField] private Animator animator;

    [Header("Tags")]
    [SerializeField] private string stunTriggerTag = "AttackStun"; // 造成暈眩的攻擊 Trigger Tag
    [SerializeField] private string playerTag = "Player";

    [Header("Door Timing")]
    [SerializeField] private float openDuration = 3f; // 開啟多久後自動關閉

    private Tween autoCloseTween;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other) return;

        BeForceOut otherOut = other.GetComponent<BeForceOut>();

        // 碰撞判斷
        if (attackToOpen && other.CompareTag(stunTriggerTag))
        {
            if (otherOut != null) otherOut.SetDirection(outDirection);

            animator.SetBool(IsOpen, true);

            // 取消之前的延遲關閉，避免多次觸發
            if (autoCloseTween.isAlive) {
                autoCloseTween.Stop();
            }

            // 3秒後自動關門
            autoCloseTween = Tween.Delay(openDuration, () => {
                animator.SetBool(IsOpen, false);
            });
        }
        else if (!attackToOpen && other.CompareTag(playerTag))
        {
            if (otherOut != null) otherOut.SetDirection(outDirection);
            animator.SetBool(IsOpen, true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // attackToOpen 狀況下不靠 exit 關閉
        if (attackToOpen) return;

        BeForceOut otherOut = other.GetComponent<BeForceOut>();
        if (other.CompareTag("Player") || otherOut != null)
        {
            animator.SetBool(IsOpen, false);
        }
    }

    public bool IsDoorOpen() => animator.GetBool(IsOpen);
}