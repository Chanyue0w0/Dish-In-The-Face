using UnityEngine;

public class ForceLeaveDoor : MonoBehaviour
{
    private static readonly int IsOpen = Animator.StringToHash("isOpen");
    [SerializeField] private bool attackToOpen = false;
    [SerializeField] private Vector2 outDirection;
    [SerializeField] private Animator animator;

    [SerializeField] private string stunTriggerTag = "AttackStun"; // 造成暈眩的攻擊 Trigger Tag
    [SerializeField] private string playerTag = "Player";
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other) return;
        
        BeForceOut otherOut = other.GetComponent<BeForceOut>();
        
        // 碰撞判斷
        if (attackToOpen && other.CompareTag(stunTriggerTag))
        {
            if (otherOut != null) otherOut.SetDirection(outDirection);
            animator.SetBool(IsOpen, true);
        }
        else if (!attackToOpen && other.CompareTag(playerTag))
        {
            if (otherOut != null) otherOut.SetDirection(outDirection);
            animator.SetBool(IsOpen, true);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        BeForceOut otherOut = other.GetComponent<BeForceOut>();
        if (other.CompareTag("Player") || otherOut != null)
        {
            animator.SetBool(IsOpen, false);
        }
    }
    
    public bool IsDoorOpen() => animator.GetBool(IsOpen);
}
