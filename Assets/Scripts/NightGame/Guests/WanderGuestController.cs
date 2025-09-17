using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WanderGuestController : MonoBehaviour
{
    [Header("------------ Settings ------------")]
    [Tooltip("每次抵達目的地後停留的秒數")]
    public float waitTime = 3f;
    [Tooltip("NavMeshAgent 的移動速度")]
    public float moveSpeed = 2f;

    [Header("Obstacle")]
    [Tooltip("隔多久才允許再生一次障礙(秒)")]
    public float obstacleCooldown = 10f;

    [Header("Stuck Retry Settings")]
    [SerializeField, Tooltip("卡住檢查的週期(秒)")]
    private float stuckCheckInterval = 1.5f;
    [SerializeField, Tooltip("認定為幾乎沒移動的距離閾值")]
    private float stuckThreshold = 0.05f;
    [SerializeField, Tooltip("卡住後，延遲多久重試走位(秒)")]
    private float retryDelay = 2f;

    [Header("Leave Settings")]
    [SerializeField, Tooltip("最短停留時間(秒)")]
    private float minStayDuration = 15f;
    [SerializeField, Tooltip("最長停留時間(秒)")]
    private float maxStayDuration = 30f;

    [Header("------------ Reference ------------")]
    [SerializeField, Tooltip("可隨機生成的障礙物 Prefab 清單(可為空)")]
    private GameObject[] obstacles;

    private NavMeshAgent agent;
    private bool isWaiting = false;
    private bool isInteracting = false;
    private float obstacleTimer = 0f;

    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private bool isRetrying = false;

    // 物件池釋放
    private GuestPoolHandler poolHandler;

    // ====== NavMesh 安全輔助 ======
    private bool AgentReady() =>
        agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    private float SafeRemainingDistance() =>
        AgentReady() ? agent.remainingDistance : float.PositiveInfinity;

    private Vector3 SafeVelocity() =>
        AgentReady() ? agent.velocity : Vector3.zero;

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) {
            agent.speed = moveSpeed;
            agent.updateRotation = false; // 2D 專案：關閉 3D 旋轉
            agent.updateUpAxis = false;   // 2D 專案：讓代理在 XY 平面
        }
        poolHandler = GetComponent<GuestPoolHandler>();
    }

    private void OnEnable() {
        // 狀態重置
        obstacleTimer = 0f;
        isWaiting = false;
        isInteracting = false;
        stuckTimer = 0f;
        isRetrying = false;

        // 確保「啟用時」就在 NavMesh 上；否則嘗試貼回
        TryEnsureOnNavMesh();

        lastPosition = transform.position;

        // 起始即選一個目的地（若不在 NavMesh，MoveToNewDestination 會先 return）
        MoveToNewDestination();

        // 啟動隨機離場計時
        float leaveTime = Random.Range(minStayDuration, maxStayDuration);
        // StartCoroutine(LeaveAfterDelay(leaveTime));
    }

    private void OnDisable() {
        // 停止所有協程，避免停用後仍有 callback 觸發
        StopAllCoroutines();
    }

    private void Update() {
        if (isInteracting) return;

        // 尚未可用 → 嘗試貼回 NavMesh，並跳過本幀 Update，避免調用 NavMesh API 拋錯
        if (!AgentReady()) {
            TryEnsureOnNavMesh();
            return;
        }

        obstacleTimer += Time.deltaTime;

        // 抵達判定（只在 path 準備完成後檢查）
        if (!agent.pathPending && SafeRemainingDistance() <= agent.stoppingDistance && !isWaiting) {
            StartCoroutine(WaitThenMove());
        }

        // 路徑無效時，換新目標點
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) {
            MoveToNewDestination();
        }

        CheckStuckAndRetry();
        FlipSpriteByVelocity();
    }

    private IEnumerator WaitThenMove() {
        isWaiting = true;

        // 到點後偶爾製造障礙物（可選）
        if (obstacles != null && obstacles.Length > 0 && obstacleTimer >= obstacleCooldown) {
            GameObject prefab = obstacles[Random.Range(0, obstacles.Length)];
            Instantiate(prefab, transform.position, Quaternion.identity);
            obstacleTimer = 0f;
        }

        yield return new WaitForSeconds(waitTime);
        isWaiting = false;
        MoveToNewDestination();
    }

    /// <summary>
    /// 指派新的可達目的地（只有在已在 NavMesh 上時才會動作）
    /// </summary>
    private void MoveToNewDestination() {
        if (!AgentReady()) {
            // 嘗試貼回；若仍失敗則先跳過，待下幀再試
            if (!TryEnsureOnNavMesh()) return;
        }

        Vector3 p;
        if (TryGetRandomReachablePointOnNavMesh(out p, 25)) {
            agent.SetDestination(p);
        }
        // 若沒找到，就保持原地，過一段時間重試（由卡住檢查接手）
    }

    /// <summary>
    /// 互動期間停住（如：被玩家攔下對話等）
    /// </summary>
    public void StopMovementForInteraction() {
        isInteracting = true;
        if (AgentReady()) {
            agent.ResetPath();
        }
        StopAllCoroutines();
    }

    /// <summary>
    /// 互動結束後恢復漫遊
    /// </summary>
    public void ResumeMovementAfterInteraction() {
        isInteracting = false;
        MoveToNewDestination();
    }

    // ====== 卡住檢查與重試 ======
    private void CheckStuckAndRetry() {
        float moved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (moved < stuckThreshold) {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckCheckInterval && !isRetrying) {
                isRetrying = true;
                StartCoroutine(RetryPathAfterDelay(retryDelay));
            }
        } else {
            stuckTimer = 0f;
            isRetrying = false;
        }
    }

    private IEnumerator RetryPathAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);

        float moved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (!isInteracting && moved < stuckThreshold) {
            MoveToNewDestination();
        }

        stuckTimer = 0f;
        isRetrying = false;
    }

    // ====== 視覺：依速度翻面 ======
    private void FlipSpriteByVelocity() {
        Vector3 v = SafeVelocity();
        if (v.x > 0.01f) {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        } else if (v.x < -0.01f) {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    // ====== 離場 ======
    // private IEnumerator LeaveAfterDelay(float delay) {
    //     yield return new WaitForSeconds(delay);
    //     Leave();
    // }

    public void Leave() {
        if (poolHandler != null) poolHandler.Release();
        else gameObject.SetActive(false);
    }

    // ====== NavMesh 工具 ======

    /// <summary>
    /// 嘗試把代理貼回最近的 NavMesh 上。成功回傳 true。
    /// </summary>
    private bool TryEnsureOnNavMesh(float searchRadius = 2f) {
        if (agent == null || !agent.isActiveAndEnabled) return false;
        if (agent.isOnNavMesh) return true;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, searchRadius, NavMesh.AllAreas)) {
            // Warp 只會在成功放置到 NavMesh 上時回傳 true
            return agent.Warp(hit.position);
        }
        return false;
    }

    /// <summary>
    /// 從 NavMesh 三角網格上隨機抽點，並確認「可達」。
    /// </summary>
    private bool TryGetRandomReachablePointOnNavMesh(out Vector3 point, int maxAttempts = 20) {
        point = transform.position;

        var tri = NavMesh.CalculateTriangulation();
        if (tri.vertices == null || tri.vertices.Length < 3 || tri.indices == null || tri.indices.Length < 3)
            return false;

        for (int i = 0; i < maxAttempts; i++) {
            // 隨機取一個三角形
            int triIndex = Random.Range(0, tri.indices.Length / 3);
            Vector3 a = tri.vertices[tri.indices[triIndex * 3 + 0]];
            Vector3 b = tri.vertices[tri.indices[triIndex * 3 + 1]];
            Vector3 c = tri.vertices[tri.indices[triIndex * 3 + 2]];

            // Barycentric 亂數取樣
            float r1 = Random.value;
            float r2 = Random.value;
            if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
            Vector3 p = a + (b - a) * r1 + (c - a) * r2;

            // 2D 專案：維持 Z 不動（XY 平面）
            p.z = transform.position.z;

            // 貼回 NavMesh 並驗證可達性
            NavMeshHit hit;
            if (NavMesh.SamplePosition(p, out hit, 1.0f, NavMesh.AllAreas)) {
                var path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete) {
                    point = hit.position;
                    return true;
                }
            }
        }
        return false;
    }
    
    public void OnGrabbedByPlayer()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
    }

    public void OnReleasedByPlayer()
    {
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            TryEnsureOnNavMesh();
        }
    }
    
    public bool IsMoving() {
        return AgentReady() && agent.velocity.sqrMagnitude > 0.01f && !isWaiting && !isInteracting;
    }
}
