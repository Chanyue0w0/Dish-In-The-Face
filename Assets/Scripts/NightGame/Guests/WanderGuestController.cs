using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WanderGuestController : MonoBehaviour
{
	[Header("------------ Settings ------------")]
	public float wanderRadius = 5f;         // 徘徊移動的最大半徑
	public float waitTime = 3f;             // 到達目標後等待的時間
	public float moveSpeed = 2f;            // 客人移動速度

	[Header("Obstacle")]
	public float obstacleCooldown = 10f;      // 障礙物生成冷卻時間（秒）

	[Header("Stuck Retry Settings")]
	[SerializeField] private float stuckCheckInterval = 1.5f;  // 幾秒內沒動作視為卡住
	[SerializeField] private float stuckThreshold = 0.05f;     // 多近的距離視為沒移動
	[SerializeField] private float retryDelay = 2f;            // 卡住後等待多久再重新尋路
	
	[Header("Leave Settings")]
	[SerializeField] private float minStayDuration = 15f;  // 最短停留秒數
	[SerializeField] private float maxStayDuration = 30f;  // 最長停留秒數

	[Header("------------ Reference ------------")]
	[SerializeField] GameObject[] obstacles;  // 可生成的障礙物 Prefab

	private NavMeshAgent agent;     // 導航代理元件
	private bool isWaiting = false; // 是否正在原地等待
	private bool isInteracting = false; // 是否正在互動中（會暫停移動）
	private float obstacleTimer = 0f; // 障礙物冷卻計時器

	// 卡住相關
	private Vector3 lastPosition;    // 上一幀位置，用來比較是否有移動
	private float stuckTimer = 0f;   // 累積未移動時間
	private bool isRetrying = false; // 是否正在重試中

	void Start()
	{
		// 取得 NavMeshAgent 並設定速度與控制行為
		agent = GetComponent<NavMeshAgent>();
		agent.speed = moveSpeed;
		agent.updateRotation = false; // 不要自動旋轉角色
		agent.updateUpAxis = false;   // 適用於 2D 模式下不改變 Y 軸方向

		lastPosition = transform.position;
		MoveToNewDestination(); // 初始找一個位置開始徘徊


		// 安排離開計時器
		float leaveTime = Random.Range(minStayDuration, maxStayDuration);
		StartCoroutine(LeaveAfterDelay(leaveTime));
	}

	void Update()
	{
		if (isInteracting) return; // 如果在互動中，暫停所有移動邏輯

		obstacleTimer += Time.deltaTime;

		// 如果已到達目標點、不是在等待中，就開始進入等待協程
		if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isWaiting)
		{
			StartCoroutine(WaitThenMove());
		}

		// 如果目前目標點無法抵達（無效路徑），則重新找路
		if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
		{
			MoveToNewDestination();
		}

		CheckStuckAndRetry();  // 檢查是否卡住
		FlipSpriteByVelocity(); // 根據移動方向翻轉角色
	}

	// 停下來等待一段時間後再移動
	IEnumerator WaitThenMove()
	{
		isWaiting = true;

		// 在等待時可能隨機生成障礙物（根據冷卻時間）
		if (obstacles.Length > 0 && obstacleTimer >= obstacleCooldown)
		{
			GameObject prefab = obstacles[Random.Range(0, obstacles.Length)];
			Instantiate(prefab, transform.position, Quaternion.identity); // 在目前位置生成
			obstacleTimer = 0f; // 重置冷卻
		}

		yield return new WaitForSeconds(waitTime); // 等待一段時間
		isWaiting = false;
		MoveToNewDestination(); // 再次尋找下一個位置
	}

	// 隨機在半徑範圍內找下一個可到達的點
	void MoveToNewDestination()
	{
		Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
		randomDirection += transform.position;

		NavMeshHit hit;
		// 找最近的可走點，並設為目標
		if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
		{
			agent.SetDestination(hit.position);
		}
	}

	// 外部呼叫：當客人正在與其他物件互動時，暫停移動
	public void StopMovementForInteraction()
	{
		isInteracting = true;
		agent.ResetPath();         // 停止當前路徑
		StopAllCoroutines();       // 停止等待中協程
	}

	// 外部呼叫：互動結束後，恢復徘徊行為
	public void ResumeMovementAfterInteraction()
	{
		isInteracting = false;
		MoveToNewDestination();    // 重新找下一個點
	}

	// 檢查是否卡住（持續一段時間都沒移動）
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

	// 延遲一段時間後，再次判斷是否仍卡住，若是則重新找路徑
	private IEnumerator RetryPathAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (!isInteracting && moved < stuckThreshold)
		{
			MoveToNewDestination(); // 重新導航
		}

		stuckTimer = 0f;
		isRetrying = false;
	}

	// 根據 NavMeshAgent 的 velocity 翻轉角色方向（X軸左右）
	private void FlipSpriteByVelocity()
	{
		if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);       // 朝右
		else if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);     // 朝左
	}

	// 一段時間後自動離開（例如被移除）
	private IEnumerator LeaveAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		// 離開處理（可改成播放動畫、淡出等）
		//Debug.Log($"{gameObject.name} 離開");
		Destroy(gameObject); // 預設直接刪除
	}

}
