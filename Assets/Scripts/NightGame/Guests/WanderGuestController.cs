using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WanderGuestController : MonoBehaviour
{
	[Header("------------ Settings ------------")]
	public float waitTime = 3f;             // 到達目標後等待的時間
	public float moveSpeed = 2f;            // 客人移動速度

	[Header("Obstacle")]
	public float obstacleCooldown = 10f;    // 障礙物生成冷卻時間（秒）

	[Header("Stuck Retry Settings")]
	[SerializeField] private float stuckCheckInterval = 1.5f;  // 幾秒內沒動作視為卡住
	[SerializeField] private float stuckThreshold = 0.05f;     // 多近的距離視為沒移動
	[SerializeField] private float retryDelay = 2f;            // 卡住後等待多久再重新尋路

	[Header("Leave Settings")]
	[SerializeField] private float minStayDuration = 15f;  // 最短停留秒數
	[SerializeField] private float maxStayDuration = 30f;  // 最長停留秒數

	[Header("------------ Reference ------------")]
	[SerializeField] GameObject[] obstacles;  // 可生成的障礙物 Prefab

	private NavMeshAgent agent;
	private bool isWaiting = false;
	private bool isInteracting = false;
	private float obstacleTimer = 0f;

	private Vector3 lastPosition;
	private float stuckTimer = 0f;
	private bool isRetrying = false;

	// 物件池處理器
	private GuestPoolHandler poolHandler;

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		agent.speed = moveSpeed;
		agent.updateRotation = false;
		agent.updateUpAxis = false;

		poolHandler = GetComponent<GuestPoolHandler>();
	}

	private void OnEnable()
	{
		// 重置狀態
		obstacleTimer = 0f;
		isWaiting = false;
		isInteracting = false;
		stuckTimer = 0f;
		isRetrying = false;

		// 確保一開始就在 NavMesh 上
		TryEnsureOnNavMesh(2f);

		lastPosition = transform.position;
		MoveToNewDestination(); // 會在整張 NavMesh 上取一個可到達點

		// 安排離開計時器
		float leaveTime = Random.Range(minStayDuration, maxStayDuration);
		StartCoroutine(LeaveAfterDelay(leaveTime));
	}

	private void OnDisable()
	{
		// 物件回收時避免掛著的協程/Invoke繼續跑
		StopAllCoroutines();
	}

	void Update()
	{
		if (isInteracting) return;

		obstacleTimer += Time.deltaTime;

		if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isWaiting)
		{
			StartCoroutine(WaitThenMove());
		}

		// 目的地失效就重新找
		if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
		{
			MoveToNewDestination();
		}

		CheckStuckAndRetry();
		FlipSpriteByVelocity();
	}

	IEnumerator WaitThenMove()
	{
		isWaiting = true;

		if (obstacles.Length > 0 && obstacleTimer >= obstacleCooldown)
		{
			GameObject prefab = obstacles[Random.Range(0, obstacles.Length)];
			Instantiate(prefab, transform.position, Quaternion.identity);
			obstacleTimer = 0f;
		}

		yield return new WaitForSeconds(waitTime);
		isWaiting = false;
		MoveToNewDestination();
	}

	/// <summary>
	/// 在整張 NavMesh 上挑一個「可到達」的隨機點做為新目的地
	/// </summary>
	void MoveToNewDestination()
	{
		// 先保險：確保自己站在 NavMesh 上
		if (!TryEnsureOnNavMesh(2f)) return;

		Vector3 p;
		if (TryGetRandomReachablePointOnNavMesh(out p, 25))
		{
			agent.SetDestination(p);
		}
		// 若取點失敗就維持原地，待下次再試
	}

	public void StopMovementForInteraction()
	{
		isInteracting = true;
		if (agent != null && agent.isOnNavMesh)
			agent.ResetPath();
		StopAllCoroutines();
	}

	public void ResumeMovementAfterInteraction()
	{
		isInteracting = false;
		MoveToNewDestination();
	}

	private void CheckStuckAndRetry()
	{
		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (moved < stuckThreshold)
		{
			stuckTimer += Time.deltaTime;
			if (stuckTimer >= stuckCheckInterval && !isRetrying)
			{
				isRetrying = true;
				StartCoroutine(RetryPathAfterDelay(retryDelay));
			}
		}
		else
		{
			stuckTimer = 0f;
			isRetrying = false;
		}
	}

	private IEnumerator RetryPathAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (!isInteracting && moved < stuckThreshold)
		{
			MoveToNewDestination();
		}

		stuckTimer = 0f;
		isRetrying = false;
	}

	private void FlipSpriteByVelocity()
	{
		if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);
	}

	private IEnumerator LeaveAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		// 用物件池回收
		if (poolHandler != null)
			poolHandler.Release();
		else
			Destroy(gameObject);
	}

	// ---------- NavMesh 工具 ----------

	/// <summary>
	/// 嘗試把代理人放回 NavMesh（若目前不在 NavMesh 上）
	/// </summary>
	private bool TryEnsureOnNavMesh(float searchRadius)
	{
		if (agent == null || !agent.isActiveAndEnabled) return false;
		if (agent.isOnNavMesh) return true;

		NavMeshHit hit;
		if (NavMesh.SamplePosition(transform.position, out hit, searchRadius, NavMesh.AllAreas))
		{
			return agent.Warp(hit.position);
		}
		return false;
	}

	/// <summary>
	/// 從 NavMesh 三角網任選一個三角形，隨機取點，並確認「可到達」
	/// maxAttempts：最多嘗試次數
	/// </summary>
	private bool TryGetRandomReachablePointOnNavMesh(out Vector3 point, int maxAttempts = 20)
	{
		point = transform.position;

		var tri = NavMesh.CalculateTriangulation();
		if (tri.vertices == null || tri.vertices.Length < 3 || tri.indices == null || tri.indices.Length < 3)
			return false;

		for (int i = 0; i < maxAttempts; i++)
		{
			// 隨機挑一個三角形
			int triIndex = Random.Range(0, tri.indices.Length / 3);
			Vector3 a = tri.vertices[tri.indices[triIndex * 3 + 0]];
			Vector3 b = tri.vertices[tri.indices[triIndex * 3 + 1]];
			Vector3 c = tri.vertices[tri.indices[triIndex * 3 + 2]];

			// 以均勻亂數取三角形內點（Barycentric）
			float r1 = Random.value;
			float r2 = Random.value;
			if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
			Vector3 p = a + (b - a) * r1 + (c - a) * r2;

			// 2D 專案常用 XY 平面，保持原本 z（避免跳高度）
			p.z = transform.position.z;

			// 吸附到 NavMesh，並檢查是否可走到
			NavMeshHit hit;
			if (NavMesh.SamplePosition(p, out hit, 1.0f, NavMesh.AllAreas))
			{
				var path = new NavMeshPath();
				if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
				{
					point = hit.position;
					return true;
				}
			}
		}
		return false;
	}
}
