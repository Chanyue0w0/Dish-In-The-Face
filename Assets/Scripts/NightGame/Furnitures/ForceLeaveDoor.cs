using UnityEngine;

public class ForceLeaveDoor : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private Transform player;
    // Start is called before the first frame update
    void Start()
    {
        player = RoundManager.Instance.player;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player" || other.tag == "Enemy")
        {
            animator.SetBool("isOpen", true);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Player" || other.tag == "Enemy")
        {
            animator.SetBool("isOpen", false);
        }
    }
}
