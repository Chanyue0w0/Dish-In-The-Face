using PrimeTween;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class BeForceOut : MonoBehaviour
{
    [SerializeField] private float forwardDistance = 20f;
    [SerializeField] private float forwardDuration = 0.5f;

    [Header("----- Events -----")] 
    public UnityEvent FourceOut;
    
    [Header("----- References -----")] 
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rigidbody2D rb;

    private Tween moveOut;

    private Vector2 direction;

    
    void OnDisable() {
        if (moveOut.isAlive) {
            moveOut.Stop(); // 立即停止，避免 OnComplete 再被呼叫
        }
    }
    
    private void MoveOut()
    {
        // 取得原本的移動方向
        Vector2 moveDir = rb != null ? rb.velocity.normalized : Vector2.zero;
        if (moveDir == Vector2.zero && agent != null)
        {
            moveDir = agent.velocity.normalized;
        }
        if (moveDir == Vector2.zero)
        {
            moveDir = direction; // 預設一個方向
        }

        if (moveDir == Vector2.zero)
        {
            Debug.Log("force out not move");
            FourceOut?.Invoke();
            return;
        }
        
        Vector3 targetPos = transform.position + (Vector3)(moveDir * forwardDistance);

        if (agent != null && agent.enabled)
            agent.isStopped = true;
        
        moveOut = Tween.Position(transform, targetPos, forwardDuration)
            .OnComplete(() =>
            {
                if (agent != null)
                {
                    if (!agent.enabled)
                    {
                        agent.enabled = true;
                        TryEnsureOnNavMesh();
                    }
                    agent.isStopped = false;
                }
                FourceOut?.Invoke();
            });
    }

    private bool TryEnsureOnNavMesh(float searchRadius = 2f)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            return agent.Warp(hit.position);
        }
        return false;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ExitDoor"))
        {
            MoveOut();
        }
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }
}
