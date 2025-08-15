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

		lastPosition = transform.position;
		MoveToNewDestination(); // 這時 agent 已經初始化了

		// 安排離開計時器
		float leaveTime = Random.Range(minStayDuration, maxStayDuration);
		StartCoroutine(LeaveAfterDelay(leaveTime));
	}

	void Start()
	{
		lastPosition = transform.position;
		MoveToNewDestination();

		float leaveTime = Random.Range(minStayDuration, maxStayDuration);
		StartCoroutine(LeaveAfterDelay(leaveTime));
	}

	void Update()
	{
		if (isInteracting) return;

		obstacleTimer += Time.deltaTime;

		if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isWaiting)
		{
			StartCoroutine(WaitThenMove());
		}

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

	void MoveToNewDestination()
	{
		Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
		randomDirection += transform.position;

		NavMeshHit hit;
		if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
		{
			agent.SetDestination(hit.position);
		}
	}

	public void StopMovementForInteraction()
	{
		isInteracting = true;
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
}
