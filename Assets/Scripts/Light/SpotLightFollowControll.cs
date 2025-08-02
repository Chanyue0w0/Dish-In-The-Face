using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class SpotlightFollower : MonoBehaviour
{
	[Header("Follow Setting")]
	[SerializeField] private Transform target;  // ���a
	[SerializeField] private bool followPosition = true;

	[Header("Offset")]
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
			initialLocalPosition = transform.position - target.position;
			targetSprite = target.GetComponent<SpriteRenderer>();
		}
		initialRotation = transform.rotation;

		// �O���Ұʮ� Outer - Inner �b�|�t�Z
		outerOffsetDistance = light2D.pointLightOuterRadius - light2D.pointLightInnerRadius;
		outerOffsetDistance = Mathf.Max(outerOffsetDistance, 0.01f); // �קK�� 0
	}

	private void LateUpdate()
	{
		if (target == null || targetSprite == null) return;

		// ���H��m
		if (followPosition)
		{
			transform.position = target.position + initialLocalPosition + positionOffset;
		}

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
}
