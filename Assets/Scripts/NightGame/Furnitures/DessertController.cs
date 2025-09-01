using UnityEngine;

public class DessertController : MonoBehaviour
{
    
    [Header("-------- Dessert Cooldown ---------")]
    [SerializeField, Min(0f)] private float dessertColdownSeconds = 5f;
    [SerializeField] private bool hideBarWhenReady = true;
    
    
    [Header("-------- Reference ---------")]
    [SerializeField] private Transform barFill;
    [SerializeField] private Animator dessertAnimator;
    // Start is called before the first frame update

    private bool isPlayerBeside;
	private float dessertCdRemain;
    private bool IsDessertOnCd => dessertCdRemain > 0f;
    
    void Start()
    {
        dessertCdRemain = 0f;
        barFill.gameObject.SetActive(false);
        barFill.localScale = new Vector3(0f, 1f, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDesertColdDown();
    }
    
    private void UpdateDesertColdDown()
    {
        if (!IsDessertOnCd) return;

        dessertCdRemain = Mathf.Max(0f, dessertCdRemain - Time.deltaTime);
        float p = (dessertColdownSeconds <= 0f) ? 0f : (dessertCdRemain / dessertColdownSeconds);

        barFill.localScale = new Vector3(Mathf.Clamp01(p), 1f, 1f);

        if (!IsDessertOnCd)
        {
            barFill.localScale = new Vector3(0f, 1f, 1f);
            barFill.gameObject.SetActive(false);
        }
    }
    
    
    private void StartDessertColdown()
    {
        if (dessertColdownSeconds <= 0f)
        {
            dessertCdRemain = 0f;
            barFill.localScale = new Vector3(0f, 1f, 1f);
            barFill.gameObject.SetActive(false);
            return;
        }

        dessertCdRemain = dessertColdownSeconds;
        barFill.gameObject.SetActive(true);
        barFill.localScale = new Vector3(1f, 1f, 1f);
    }
    
    public bool UseDessert()
    {
        if (IsDessertOnCd || !isPlayerBeside) return false;

        if (dessertAnimator)
            dessertAnimator.Play("DessertEffect", -1, 0f);

        RoundManager.Instance.chairGroupManager.ResetAllSetGuestsPatience();
        StartDessertColdown();
        return true;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            isPlayerBeside = true;
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            isPlayerBeside = false;
        }
    }
}
