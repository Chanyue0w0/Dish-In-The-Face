using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WanderGuestController : MonoBehaviour
{
	[Header("------------ Settings ------------")]
	public float waitTime = 3f;             // ��F�ؼЫᵥ�ݪ��ɶ�
	public float moveSpeed = 2f;            // �ȤH���ʳt��

	[Header("Obstacle")]
	public float obstacleCooldown = 10f;    // ��ê���ͦ��N�o�ɶ��]��^

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

		// �T�O�@�}�l�N�b NavMesh �W
		TryEnsureOnNavMesh(2f);

		lastPosition = transform.position;
		MoveToNewDestination(); // �|�b��i NavMesh �W���@�ӥi��F�I

		// �w�����}�p�ɾ�
		float leaveTime = Random.Range(minStayDuration, maxStayDuration);
		StartCoroutine(LeaveAfterDelay(leaveTime));
	}

	private void OnDisable()
	{
		// ����^�����קK���۪���{/Invoke�~��]
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

		// �ت��a���ĴN���s��
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
	/// �b��i NavMesh �W�D�@�ӡu�i��F�v���H���I�����s�ت��a
	/// </summary>
	void MoveToNewDestination()
	{
		// ���O�I�G�T�O�ۤv���b NavMesh �W
		if (!TryEnsureOnNavMesh(2f)) return;

		Vector3 p;
		if (TryGetRandomReachablePointOnNavMesh(out p, 25))
		{
			agent.SetDestination(p);
		}
		// �Y���I���ѴN������a�A�ݤU���A��
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

		// �Ϊ�����^��
		if (poolHandler != null)
			poolHandler.Release();
		else
			gameObject.SetActive(false);
	}

	// ---------- NavMesh �u�� ----------

	/// <summary>
	/// ���է�N�z�H��^ NavMesh�]�Y�ثe���b NavMesh �W�^
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
	/// �q NavMesh �T��������@�ӤT���ΡA�H�����I�A�ýT�{�u�i��F�v
	/// maxAttempts�G�̦h���զ���
	/// </summary>
	private bool TryGetRandomReachablePointOnNavMesh(out Vector3 point, int maxAttempts = 20)
	{
		point = transform.position;

		var tri = NavMesh.CalculateTriangulation();
		if (tri.vertices == null || tri.vertices.Length < 3 || tri.indices == null || tri.indices.Length < 3)
			return false;

		for (int i = 0; i < maxAttempts; i++)
		{
			// �H���D�@�ӤT����
			int triIndex = Random.Range(0, tri.indices.Length / 3);
			Vector3 a = tri.vertices[tri.indices[triIndex * 3 + 0]];
			Vector3 b = tri.vertices[tri.indices[triIndex * 3 + 1]];
			Vector3 c = tri.vertices[tri.indices[triIndex * 3 + 2]];

			// �H���öüƨ��T���Τ��I�]Barycentric�^
			float r1 = Random.value;
			float r2 = Random.value;
			if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
			Vector3 p = a + (b - a) * r1 + (c - a) * r2;

			// 2D �M�ױ`�� XY �����A�O���쥻 z�]�קK�����ס^
			p.z = transform.position.z;

			// �l���� NavMesh�A���ˬd�O�_�i����
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
