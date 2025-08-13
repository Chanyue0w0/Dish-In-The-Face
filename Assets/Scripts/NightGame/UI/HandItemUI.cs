using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandItemUI : MonoBehaviour
{
    [SerializeField] private Transform playerHandItem;
    [SerializeField] private Image dishImage;
    [SerializeField] private TextMeshProUGUI dishCountText;

	public void Start()
	{
		dishImage.gameObject.SetActive(false);
	}
	public void ChangeHandItemUI()
    {
        if (playerHandItem.childCount == 0)
		{
			dishImage.gameObject.SetActive(false);
			dishCountText.text = "";
            return;
		}

        dishCountText.text = playerHandItem.childCount.ToString();
        Sprite sprite = playerHandItem.GetChild(0)?.GetComponent<SpriteRenderer>()?.sprite;
        if (sprite == null)
        {
			dishImage.gameObject.SetActive(false);
			return;
        }

        dishImage.gameObject.SetActive(true);
        dishImage.sprite = sprite;
    }
}
