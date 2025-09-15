using UnityEngine;

public class ForceLeaveDoor : MonoBehaviour
{
    private static readonly int IsOpen = Animator.StringToHash("isOpen");
    [SerializeField] private Vector2 outDirection;
    [SerializeField] private Animator animator;

    private void OnTriggerEnter2D(Collider2D other)
    {
        BeForceOut otherOut = other.GetComponent<BeForceOut>();
        if (other.CompareTag("Player") || otherOut != null)
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
}
