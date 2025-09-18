using UnityEngine;
using UnityEngine.Events;

public class PathMover : MonoBehaviour
{
    public enum PathMode { OneWay, Loop }            // 單向 or 循環
    public enum TravelDirection { Forward, Backward } // 順走 or 倒走

    [Header("----- References -----")]
    [Tooltip("要被移動的目標物件")]
    [SerializeField] private Transform target;

    [Tooltip("路徑節點（依序）。在 Scene 中擺放空物件，拖進來即可")]
    [SerializeField] private Transform[] waypoints;

    [Header("----- Path Options -----")]
    [Tooltip("路徑模式：單向或循環")]
    [SerializeField] private PathMode pathMode = PathMode.OneWay;

    [Tooltip("起始方向：Forward=從0到N-1，Backward=從N-1到0")]
    [SerializeField] private TravelDirection startDirection = TravelDirection.Forward;

    [Tooltip("移動速度（單位/秒）")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;

    [Tooltip("到達節點的判定距離（防止浮點誤差卡點）")]
    [SerializeField, Min(0.001f)] private float arriveThreshold = 0.05f;

    [Tooltip("Play On Start：進入場景自動開始移動")]
    [SerializeField] private bool playOnStart = true;

    [Header("----- Events -----")]
    public UnityEvent onPathFinished; // 單向模式抵達最後一個節點時觸發
    public UnityEvent<int> onWaypointReached; // 每次踩到節點時（傳出索引）

    // --- Runtime state ---
    private int currentIndex;
    private int step;       // 方向步進：Forward=+1, Backward=-1
    private bool isPlaying; // 是否正在移動

    private void Reset()
    {
        // 預設把 target 設為自己，避免忘了指定
        if (target == null) target = this.transform;
    }

    private void Start()
    {
        Initialize();
        if (playOnStart) Play();
    }

    private void Update()
    {
        if (!isPlaying || target == null || waypoints == null || waypoints.Length == 0) return;

        Transform goal = waypoints[currentIndex];
        if (goal == null) { AdvanceIndex(); return; } // 避免有人把節點刪掉

        Vector3 goalPos = goal.position;
        Vector3 pos = target.position;

        // 移動
        if (moveSpeed <= 0f)
        {
            // 速度為0則直接傳送到目標點（可依需求改成不動）
            target.position = goalPos;
        }
        else
        {
            float stepDist = moveSpeed * Time.deltaTime;
            target.position = Vector3.MoveTowards(pos, goalPos, stepDist);
        }

        // 抵達判定
        if ((target.position - goalPos).sqrMagnitude <= arriveThreshold * arriveThreshold)
        {
            // 確保貼齊
            target.position = goalPos;

            // 事件：踩到節點
            onWaypointReached?.Invoke(currentIndex);

            // 前進到下一節點（或結束/循環）
            if (!AdvanceIndex())
            {
                // 不再前進（OneWay 結束）
                isPlaying = false;
                onPathFinished?.Invoke();
            }
        }
    }

    /// <summary>
    /// 初始化索引與方向
    /// </summary>
    private void Initialize()
    {
        // 防護
        if (waypoints == null || waypoints.Length == 0) return;

        step = (startDirection == TravelDirection.Forward) ? +1 : -1;
        currentIndex = (startDirection == TravelDirection.Forward) ? 0 : (waypoints.Length - 1);

        // 將 target 放到起點上（可依需求關閉）
        if (target != null && waypoints[currentIndex] != null)
        {
            target.position = waypoints[currentIndex].position;
        }
    }

    /// <summary>
    /// 將索引推進到下一個節點。
    /// 回傳：true=成功推進，false=到頭而停止（OneWay）
    /// </summary>
    private bool AdvanceIndex()
    {
        if (waypoints == null || waypoints.Length == 0) return false;

        int next = currentIndex + step;

        // 循環模式
        if (pathMode == PathMode.Loop)
        {
            // 讓 next 落在 [0, len-1]
            int len = waypoints.Length;
            if (next < 0) next = len - 1;
            else if (next >= len) next = 0;

            currentIndex = next;
            return true;
        }

        // 單向模式
        if (next < 0 || next >= waypoints.Length)
        {
            // 不能再前進，停止
            return false;
        }

        currentIndex = next;
        return true;
    }

    // ---------- Public Controls ----------
    public void Play()
    {
        // 若尚未初始化（例如手動在 Inspector 改參數），保險重新檢查
        if (waypoints == null || waypoints.Length == 0 || target == null)
        {
            Debug.LogWarning("[PathMover] Missing target or waypoints.");
            return;
        }
        if (!isPlaying) isPlaying = true;
    }

    public void Pause() => isPlaying = false;

    public void TogglePlayPause() => isPlaying = !isPlaying;

    /// <summary>
    /// 重設回起點（依照目前 startDirection）
    /// </summary>
    public void ResetToStart()
    {
        Initialize();
    }

    /// <summary>
    /// 立即切換行走方向（不改變 pathMode）
    /// </summary>
    public void FlipDirection()
    {
        step *= -1;
    }

    /// <summary>
    /// 設定新的路徑與可選的目標物件
    /// </summary>
    public void SetPath(Transform[] newWaypoints, Transform newTarget = null, bool keepWorldPos = false)
    {
        if (newWaypoints != null && newWaypoints.Length > 0) waypoints = newWaypoints;
        if (newTarget != null) target = newTarget;

        // 如需保留當前位置，不要重設到起點
        if (!keepWorldPos) Initialize();
    }

    /// <summary>
    /// 設定移動方向
    /// </summary>
    public void SetDirection(TravelDirection dir)
    {
        startDirection = dir;
        step = (dir == TravelDirection.Forward) ? +1 : -1;
    }

    // ---------- Gizmos for Scene View ----------
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // 路徑線
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            var a = waypoints[i];
            if (a == null) continue;

            // 節點球
            Gizmos.DrawWireSphere(a.position, 0.15f);

            // 畫到下一點（最後一點連回第一點若是循環）
            int j = i + 1;
            if (j < waypoints.Length)
            {
                var b = waypoints[j];
                if (b != null) Gizmos.DrawLine(a.position, b.position);
            }
            else if (pathMode == PathMode.Loop && waypoints[0] != null)
            {
                Gizmos.DrawLine(a.position, waypoints[0].position);
            }
        }

        // 目標位置
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(target.position, new Vector3(0.2f, 0.2f, 0.2f));
        }
    }
}
