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

	private NavMeshAgent agent;     // �ɯ�N�z����
	private bool isWaiting = false; // �O�_���b��a����
	private bool isInteracting = false; // �O�_���b���ʤ��]�|�Ȱ����ʡ^
	private float obstacleTimer = 0f; // ��ê���N�o�p�ɾ�

	// �d�����
	private Vector3 lastPosition;    // �W�@�V��m�A�ΨӤ���O�_������
	private float stuckTimer = 0f;   // �ֿn�����ʮɶ�
	private bool isRetrying = false; // �O�_���b���դ�

	void Start()
	{
		// ���o NavMeshAgent �ó]�w�t�׻P����欰
		agent = GetComponent<NavMeshAgent>();
		agent.speed = moveSpeed;
		agent.updateRotation = false; // ���n�۰ʱ��ਤ��
		agent.updateUpAxis = false;   // �A�Ω� 2D �Ҧ��U������ Y �b��V

		lastPosition = transform.position;
		MoveToNewDestination(); // ��l��@�Ӧ�m�}�l�r��


		// �w�����}�p�ɾ�
		float leaveTime = Random.Range(minStayDuration, maxStayDuration);
		StartCoroutine(LeaveAfterDelay(leaveTime));
	}

	void Update()
	{
		if (isInteracting) return; // �p�G�b���ʤ��A�Ȱ��Ҧ������޿�

		obstacleTimer += Time.deltaTime;

		// �p�G�w��F�ؼ��I�B���O�b���ݤ��A�N�}�l�i�J���ݨ�{
		if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isWaiting)
		{
			StartCoroutine(WaitThenMove());
		}

		// �p�G�ثe�ؼ��I�L�k��F�]�L�ĸ��|�^�A�h���s���
		if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
		{
			MoveToNewDestination();
		}

		CheckStuckAndRetry();  // �ˬd�O�_�d��
		FlipSpriteByVelocity(); // �ھڲ��ʤ�V½�ਤ��
	}

	// ���U�ӵ��ݤ@�q�ɶ���A����
	IEnumerator WaitThenMove()
	{
		isWaiting = true;

		// �b���ݮɥi���H���ͦ���ê���]�ھڧN�o�ɶ��^
		if (obstacles.Length > 0 && obstacleTimer >= obstacleCooldown)
		{
			GameObject prefab = obstacles[Random.Range(0, obstacles.Length)];
			Instantiate(prefab, transform.position, Quaternion.identity); // �b�ثe��m�ͦ�
			obstacleTimer = 0f; // ���m�N�o
		}

		yield return new WaitForSeconds(waitTime); // ���ݤ@�q�ɶ�
		isWaiting = false;
		MoveToNewDestination(); // �A���M��U�@�Ӧ�m
	}

	// �H���b�b�|�d�򤺧�U�@�ӥi��F���I
	void MoveToNewDestination()
	{
		Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
		randomDirection += transform.position;

		NavMeshHit hit;
		// ��̪񪺥i���I�A�ó]���ؼ�
		if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
		{
			agent.SetDestination(hit.position);
		}
	}

	// �~���I�s�G��ȤH���b�P��L���󤬰ʮɡA�Ȱ�����
	public void StopMovementForInteraction()
	{
		isInteracting = true;
		agent.ResetPath();         // �����e���|
		StopAllCoroutines();       // ����ݤ���{
	}

	// �~���I�s�G���ʵ�����A��_�r�ަ欰
	public void ResumeMovementAfterInteraction()
	{
		isInteracting = false;
		MoveToNewDestination();    // ���s��U�@���I
	}

	// �ˬd�O�_�d��]����@�q�ɶ����S���ʡ^
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

	// ����@�q�ɶ���A�A���P�_�O�_���d��A�Y�O�h���s����|
	private IEnumerator RetryPathAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (!isInteracting && moved < stuckThreshold)
		{
			MoveToNewDestination(); // ���s�ɯ�
		}

		stuckTimer = 0f;
		isRetrying = false;
	}

	// �ھ� NavMeshAgent �� velocity ½�ਤ���V�]X�b���k�^
	private void FlipSpriteByVelocity()
	{
		if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);       // �¥k
		else if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);     // �¥�
	}

	// �@�q�ɶ���۰����}�]�Ҧp�Q�����^
	private IEnumerator LeaveAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		// ���}�B�z�]�i�令����ʵe�B�H�X���^
		//Debug.Log($"{gameObject.name} ���}");
		Destroy(gameObject); // �w�]�����R��
	}

}
