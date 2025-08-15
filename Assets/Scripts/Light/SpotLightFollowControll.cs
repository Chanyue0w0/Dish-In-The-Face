using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class SpotlightFollower : MonoBehaviour
{
	[Header("Follow Setting")]
	[SerializeField] private Transform target;  // ���a
	[SerializeField] private bool followPosition = true;

	[Header("Offset")]
	[Tooltip("�l�׺����b�ؼФW��h�ֶZ���]�@�ɮy�Ъ� +Y ��V�^�C�Y���ΡA�h�ϥ� initialLocalPosition + positionOffset�C")]
	[SerializeField] private bool useAboveDistance = true;
	[SerializeField] private float aboveDistance = 1f;     // �Z���ؼФW��h�ֶZ���]�@�ɮy�С^
	[SerializeField] private Vector3 positionOffset = Vector3.zero;

	private Light2D light2D;
	private Vector3 initialLocalPosition;
	private Quaternion initialRotation;
	private SpriteRenderer targetSprite;

	private float outerOffsetDistance = 1f; // OuterRadius �P InnerRadius ���t�Z

	private void Awake()
	{
		light2D = GetComponent<Light2D>();

		if (target != null)
		{
			// �Y�ϥΤW��Z���A�w�] initialLocalPosition �H aboveDistance ���D�F
			// �_�h�O���P�ؼз�U���۹��m�C
			initialLocalPosition = useAboveDistance
				? Vector3.up * aboveDistance
				: (transform.position - target.position);

			targetSprite = target.GetComponent<SpriteRenderer>();
		}

		initialRotation = transform.rotation;

		// �O���Ұʮ� Outer - Inner �b�|�t�Z
		outerOffsetDistance = light2D.pointLightOuterRadius - light2D.pointLightInnerRadius;
		outerOffsetDistance = Mathf.Max(outerOffsetDistance, 0.01f); // �קK�� 0
	}

	private void OnEnable()
	{
		// �ҥήɥߧY���m��ؼЦ�m�]�]�t�W��Z��/�ۭq offset�^�A�ç�s�@�����׻P�b�|
		ResetToTargetImmediate();
	}

	private void LateUpdate()
	{
		if (target == null || targetSprite == null) return;

		// ���H��m
		if (followPosition)
		{
			Vector3 baseOffset = useAboveDistance ? (Vector3.up * aboveDistance) : initialLocalPosition;
			transform.position = target.position + baseOffset + positionOffset;
		}

		UpdateAimAndRadius();
	}

	private void ResetToTargetImmediate()
	{
		if (target == null) return;

		// �H�ثe�]�w�p���¦ offset
		Vector3 baseOffset = useAboveDistance ? (Vector3.up * aboveDistance) : initialLocalPosition;

		// �ߧY���m�����ؼ�
		transform.position = target.position + baseOffset + positionOffset;

		// �ߧY��s���׻P�b�|�A�קK�ҥη�U�{�{
		if (targetSprite == null) targetSprite = target.GetComponent<SpriteRenderer>();
		UpdateAimAndRadius();
	}

	private void UpdateAimAndRadius()
	{
		if (target == null || light2D == null || targetSprite == null) return;

		// ���V���a
		Vector2 toPlayer = target.position - transform.position;
		float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);

		// �p�⪱�a�}����m�]�ϥ� SpriteRenderer�^
		float playerBottomY = targetSprite.bounds.min.y;
		Vector2 playerBottomPos = new Vector2(target.position.x, playerBottomY);

		// Inner Radius = ������}���Z��
		float innerDistance = Vector2.Distance(transform.position, playerBottomPos);
		light2D.pointLightInnerRadius = Mathf.Max(0.01f, innerDistance);

		// Outer Radius = inner + �T�w�t�Z
		light2D.pointLightOuterRadius = light2D.pointLightInnerRadius + outerOffsetDistance;
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		// �קK�]�w���t��
		aboveDistance = Mathf.Max(0f, aboveDistance);

		// �Y�s�边������ useAboveDistance�A��s initialLocalPosition �����
		if (target != null)
		{
			initialLocalPosition = useAboveDistance ? Vector3.up * aboveDistance : (transform.position - target.position);
		}
	}
#endif
}
