using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class SpotlightFollower : MonoBehaviour
{
	[Header("Follow Setting")]
	[SerializeField] private Transform target;  // 玩家
	[SerializeField] private bool followPosition = true;

	[Header("Offset")]
	[Tooltip("始終維持在目標上方多少距離（世界座標的 +Y 方向）。若停用，則使用 initialLocalPosition + positionOffset。")]
	[SerializeField] private bool useAboveDistance = true;
	[SerializeField] private float aboveDistance = 1f;     // 距離目標上方多少距離（世界座標）
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
			// 若使用上方距離，預設 initialLocalPosition 以 aboveDistance 為主；
			// 否則記錄與目標當下的相對位置。
			initialLocalPosition = useAboveDistance
				? Vector3.up * aboveDistance
				: (transform.position - target.position);

			targetSprite = target.GetComponent<SpriteRenderer>();
		}

		initialRotation = transform.rotation;

		// 記錄啟動時 Outer - Inner 半徑差距
		outerOffsetDistance = light2D.pointLightOuterRadius - light2D.pointLightInnerRadius;
		outerOffsetDistance = Mathf.Max(outerOffsetDistance, 0.01f); // 避免為 0
	}

	private void OnEnable()
	{
		// 啟用時立即重置到目標位置（包含上方距離/自訂 offset），並更新一次角度與半徑
		ResetToTargetImmediate();
	}

	private void LateUpdate()
	{
		if (target == null || targetSprite == null) return;

		// 跟隨位置
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

		// 以目前設定計算基礎 offset
		Vector3 baseOffset = useAboveDistance ? (Vector3.up * aboveDistance) : initialLocalPosition;

		// 立即把位置對齊到目標
		transform.position = target.position + baseOffset + positionOffset;

		// 立即更新角度與半徑，避免啟用當下閃爍
		if (targetSprite == null) targetSprite = target.GetComponent<SpriteRenderer>();
		UpdateAimAndRadius();
	}

	private void UpdateAimAndRadius()
	{
		if (target == null || light2D == null || targetSprite == null) return;

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

#if UNITY_EDITOR
	private void OnValidate()
	{
		// 避免設定為負值
		aboveDistance = Mathf.Max(0f, aboveDistance);

		// 若編輯器內切換 useAboveDistance，更新 initialLocalPosition 的基準
		if (target != null)
		{
			initialLocalPosition = useAboveDistance ? Vector3.up * aboveDistance : (transform.position - target.position);
		}
	}
#endif
}
