using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class SpotlightFollower : MonoBehaviour
{
	[Header("Follow Setting")]
	[SerializeField] private Transform target;  // 玩家
	[SerializeField] private bool followPosition = true;

	[Header("Offset")]
	[SerializeField] private Vector3 positionOffset = Vector3.zero;

	private Light2D light2D;
	private Vector3 initialLocalPosition;
	private Quaternion initialRotation;
	private SpriteRenderer targetSprite;

	private float outerOffsetDistance = 1f; // OuterRadius 與 InnerRadius 的差距

	private void Awake()
	{
		light2D = GetComponent<Light2D>();
		if (target != null)
		{
			initialLocalPosition = transform.position - target.position;
			targetSprite = target.GetComponent<SpriteRenderer>();
		}
		initialRotation = transform.rotation;

		// 記錄啟動時 Outer - Inner 半徑差距
		outerOffsetDistance = light2D.pointLightOuterRadius - light2D.pointLightInnerRadius;
		outerOffsetDistance = Mathf.Max(outerOffsetDistance, 0.01f); // 避免為 0
	}

	private void LateUpdate()
	{
		if (target == null || targetSprite == null) return;

		// 跟隨位置
		if (followPosition)
		{
			transform.position = target.position + initialLocalPosition + positionOffset;
		}

		// 面向玩家
		Vector2 toPlayer = target.position - transform.position;
		float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);

		// 計算玩家腳底位置（使用 SpriteRenderer）
		float playerBottomY = targetSprite.bounds.min.y;
		Vector2 playerBottomPos = new Vector2(target.position.x, playerBottomY);

		// Inner Radius = 光源到腳底距離
		float innerDistance = Vector2.Distance(transform.position, playerBottomPos);
		light2D.pointLightInnerRadius = Mathf.Max(0.01f, innerDistance);

		// Outer Radius = inner + 固定差距
		light2D.pointLightOuterRadius = light2D.pointLightInnerRadius + outerOffsetDistance;
	}
}
