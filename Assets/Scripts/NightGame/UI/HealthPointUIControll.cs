//using System.Collections;
//using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;

public class HealthPointUIControll : MonoBehaviour
{
    [SerializeField] private List<GameObject> heartImage;
    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject heart in heartImage)
            heart.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DecreaseHP()
    {
        for (int i = heartImage.Count - 1; i >= 0; i--)
            if (heartImage[i].activeSelf)
            {
				heartImage[i].SetActive(false);
                break;
			}
	}
}
