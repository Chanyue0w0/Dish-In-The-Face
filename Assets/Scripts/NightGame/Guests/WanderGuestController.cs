using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WanderGuestController : MonoBehaviour
{
	[Header("------------ Settings ------------")]
	public float wanderRadius = 5f;         // �r�޲��ʪ��̤j�b�|
	public float waitTime = 3f;             // ��F�ؼЫᵥ�ݪ��ɶ�
	public float moveSpeed = 2f;            // �ȤH���ʳt��

	[Header("Obstacle")]
	public float obstacleCooldown = 10f;      // ��ê���ͦ��N�o�ɶ��]��^

	[Header("Stuck Retry Settings")]
	[SerializeField] private float stuckCheckInterval = 1.5f;  // �X���S�ʧ@�����d��
	[SerializeField] private float stuckThreshold = 0.05f;     // �h�񪺶Z�������S����
	[SerializeField] private float retryDelay = 2f;            // �d��ᵥ�ݦh�[�A���s�M��

	[Header("Leave Settings")]
	[SerializeField] private float minStayDuration = 15f;  // �̵u���d���
	[SerializeField] private float maxStayDuration = 30f;  // �̪����d���

	[Header("------------ Reference ------------")]
	[SerializeField] GameObject[] obstacles;  // �i�ͦ�����ê�� Prefab

	private NavMeshAgent agent;
	private bool isWaiting = false;
	private bool isInteracting = false;
	private float obstacleTimer = 0f;

	private Vector3 lastPosition;
	private float stuckTimer = 0f;
	private bool isRetrying = false;

	// ������B�z��
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
		// ���m���A
		obstacleTimer = 0f;
		isWaiting = false;
		isInteracting = false;
		stuckTimer = 0f;
		isRetrying = false;

		lastPosition = transform.position;
		MoveToNewDestination(); // �o�� agent �w�g��l�ƤF

		// �w�����}�p�ɾ�
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

		// �Ϊ�����^��
		if (poolHandler != null)
			poolHandler.Release();
		else
			Destroy(gameObject);
	}
}
