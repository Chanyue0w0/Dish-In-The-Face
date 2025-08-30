using UnityEngine;

public class BeGrabByPlayer : MonoBehaviour
{
    [SerializeField] private bool isCanBeGrabByPlayer = false;
    
    [SerializeField] private bool isOnBeGrabing = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public bool GetIsCanBeGrabByPlayer() => isCanBeGrabByPlayer;
    public void SetIsCanBeGrabByPlayer(bool value) => isCanBeGrabByPlayer = value;
    
    public void SetIsOnBeGrabing(bool value) => isOnBeGrabing = value;
    public bool GetIsCanBeGrabing() => isCanBeGrabByPlayer;
}
