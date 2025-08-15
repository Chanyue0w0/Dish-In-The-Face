using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class TableConveyorBelt : MonoBehaviour
{
	[Header("Path (Waypoints)")]
	[SerializeField] private List<Transform> waypoints = new List<Transform>(); // 路點（至少 2 個）
	[SerializeField] private bool loop = false;

	[Header("Spline Settings")]
	[SerializeField, Range(4, 200)] private int samplesPerSegment = 20; // 每段曲線烘焙點數（越高越平滑、成本越大）
	[SerializeField] private bool autoRebuildOnChange = true;

	[Header("Ride Settings")]
	[SerializeField, Min(0.01f)] private float speed = 20f;  // m/s
	[SerializeField] private bool snapOnStart = true;
	[SerializeField] private float exitSideOffset = 0.2f;

	// === Boarding Surface ===
	private Collider2D boardCollider; // 指定用來判斷「可以搭乘」的碰撞器（通常是桌面/輸送帶面）
	public Collider2D BoardCollider
	{
		get
		{
			if (!boardCollider) boardCollider = GetComponent<Collider2D>();
			return boardCollider;
		}
	}

	// ---- 烘焙後的資料（曲線點、長度表） ----
	private List<Vector3> bakedPoints = new List<Vector3>();
	private List<float> cumulativeLengths = new List<float>(); // 與 bakedPoints 等長
	private float totalLength = 0f;

	// ====== 對外屬性 ======
	public bool Loop => loop;
	public bool SnapOnStart => snapOnStart;
	public float ExitSideOffset => exitSideOffset;
	public float Speed => speed;
	public int WaypointCount => waypoints?.Count ?? 0;
	public float TotalLength => totalLength;

	private void Awake()
	{
		// 不再強制設為 Trigger；允許實體碰撞
		RebuildIfNeeded();
	}

	private void OnValidate()
	{
		if (autoRebuildOnChange)
			RebuildIfNeeded();
	}

	private void Update()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying && autoRebuildOnChange)
			RebuildIfNeeded();
#endif
	}

	// =========================
	// ========== API ==========
	// =========================

	/// 取得第 i 個路點世界座標（保留舊 API 方便過渡）
	public Vector3 GetPoint(int index)
	{
		index = Mathf.Clamp(index, 0, WaypointCount - 1);
		return waypoints[index] ? waypoints[index].position : transform.position;
	}

	public int GetNextIndex(int index)
	{
		int last = WaypointCount - 1;
		if (index < last) return index + 1;
		return loop ? 0 : last;
	}

	public bool IsLastSegment(int index) => !loop && index >= WaypointCount - 2;

	/// 傳回：在曲線上「距離起點 s（公尺）」對應的位置
	public Vector3 EvaluatePositionByDistance(float s)
	{
		if (!HasCurve()) return transform.position;
		s = WrapDistance(s);
		int i = FindSampleIndexByDistance(s, out float segStartLen);
		float segLen = cumulativeLengths[i + 1] - segStartLen;
		float t = segLen > 1e-6f ? (s - segStartLen) / segLen : 0f;
		return Vector3.LerpUnclamped(bakedPoints[i], bakedPoints[i + 1], t);
	}

	/// 傳回：在曲線上距離 s 的「單位切線向量」
	public Vector3 EvaluateTangentByDistance(float s)
	{
		if (!HasCurve()) return Vector3.right;
		s = WrapDistance(s);
		int i = FindSampleIndexByDistance(s, out _);
		Vector3 a = bakedPoints[i];
		Vector3 b = bakedPoints[i + 1];
		Vector3 dir = (b - a);
		return dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.right;
	}

	/// 2D 左法線（可用於下車時側移避免卡牆）
	public Vector3 EvaluateLeftNormalByDistance(float s)
	{
		Vector3 t = EvaluateTangentByDistance(s);
		return new Vector3(-t.y, t.x, 0f);
	}

	/// 把世界座標投影到曲線上，回傳「距離 s」
	public float ProjectPointToDistance(Vector3 worldPos)
	{
		if (!HasCurve()) return 0f;

		// 先粗略：找烘焙點中最近者
		int closest = 0;
		float best = float.MaxValue;
		for (int i = 0; i < bakedPoints.Count; i++)
		{
			float d2 = (worldPos - bakedPoints[i]).sqrMagnitude;
			if (d2 < best)
			{
				best = d2;
				closest = i;
			}
		}

		// 再在相鄰線段上做一次投影細化
		int i0 = Mathf.Max(0, closest - 1);
		int i1 = Mathf.Min(bakedPoints.Count - 2, closest);
		float bestS = cumulativeLengths[closest]; // 預設抓點本身

		for (int i = i0; i <= i1; i++)
		{
			Vector3 a = bakedPoints[i];
			Vector3 b = bakedPoints[i + 1];
			Vector3 ab = b - a;
			float len2 = ab.sqrMagnitude;
			if (len2 < 1e-8f) continue;

			float t = Mathf.Clamp01(Vector3.Dot(worldPos - a, ab) / len2);
			Vector3 p = a + ab * t;
			float d2 = (worldPos - p).sqrMagnitude;
			if (d2 < best)
			{
				best = d2;
				bestS = cumulativeLengths[i] + t * (cumulativeLengths[i + 1] - cumulativeLengths[i]);
			}
		}
		return bestS;
	}

	/// <summary>
	/// 嘗試取消正在滑行的玩家，並沿「玩家輸入方向」把玩家瞬移到此桌（BoardCollider）外側
	/// 距離為 exitSideOffset。若輸入方向與滑行方向相同，則不取消、繼續滑行。
	/// </summary>
	public bool TryCancelSlideAndEject(Rigidbody2D rb, float s, int slideDir, Vector2 inputDir, float sameDirDotThreshold = 0.75f)
	{
		if (rb == null) return false;

		// 目前行進方向（依 slideDir 修正）
		Vector2 tan = ((Vector2)EvaluateTangentByDistance(s));
		if (tan.sqrMagnitude < 1e-6f) tan = Vector2.right;
		tan.Normalize();
		Vector2 travel = (slideDir >= 0) ? tan : -tan;

		// 若玩家輸入與滑行方向同向 → 不取消
		if (inputDir.sqrMagnitude > 1e-6f)
		{
			Vector2 inNorm = inputDir.normalized;
			if (Vector2.Dot(inNorm, travel) >= Mathf.Clamp01(sameDirDotThreshold))
				return false;
		}

		// 判斷帶子更偏上下或左右（決定退出軸）
		bool beltIsVertical = Mathf.Abs(travel.y) >= Mathf.Abs(travel.x);

		// 左法線（以「行進方向」為準），供無輸入時的預設退場方向
		Vector2 leftOfTravel = ((Vector2)EvaluateLeftNormalByDistance(s)).normalized;
		if (slideDir < 0) leftOfTravel = -leftOfTravel; // 反向行進時，左/右相反

		// 計算退出方向（僅在垂直軸上取 +/-1）
		Vector2 exitDir;
		if (beltIsVertical)
		{
			// 往左右退出：優先用玩家 input.x，否則用左側
			float signX = Mathf.Abs(inputDir.x) >= 0.2f ? Mathf.Sign(inputDir.x)
						: (Mathf.Abs(leftOfTravel.x) > 1e-6f ? Mathf.Sign(leftOfTravel.x) : 1f);
			exitDir = new Vector2(signX, 0f);
		}
		else
		{
			// 往上下退出：優先用玩家 input.y，否則用左側
			float signY = Mathf.Abs(inputDir.y) >= 0.2f ? Mathf.Sign(inputDir.y)
						: (Mathf.Abs(leftOfTravel.y) > 1e-6f ? Mathf.Sign(leftOfTravel.y) : 1f);
			exitDir = new Vector2(0f, signY);
		}

		// 目標點：取 BoardCollider 的 AABB 外緣上，沿 exitDir 的最靠外點，再外推 ExitSideOffset
		float margin = Mathf.Max(0.01f, ExitSideOffset);
		Vector2 target;

		if (BoardCollider)
		{
			Bounds b = BoardCollider.bounds;

			if (beltIsVertical)
			{
				// 水平退出：x 取 min/max，y 夾在桌面高度範圍內，避免多餘斜跳
				float x = exitDir.x >= 0f ? b.max.x : b.min.x;
				float y = Mathf.Clamp(rb.position.y, b.min.y, b.max.y);
				Vector2 support = new Vector2(x, y);
				target = support + exitDir * margin;
			}
			else
			{
				// 垂直退出：y 取 min/max，x 夾在桌面寬度範圍內
				float y = exitDir.y >= 0f ? b.max.y : b.min.y;
				float x = Mathf.Clamp(rb.position.x, b.min.x, b.max.x);
				Vector2 support = new Vector2(x, y);
				target = support + exitDir * margin;
			}
		}
		else
		{
			// 沒有 BoardCollider：從當前曲線位置外推
			Vector2 p = EvaluatePositionByDistance(s);
			target = p + exitDir * margin;
		}

		rb.MovePosition(target);
		return true;
	}


	/// 根據「從 s 出發的距離增量 ds」（可正可負）得到新的 s（自動處理 loop）
	public float AdvanceByDistance(float s, float ds)
	{
		if (!HasCurve()) return 0f;
		return WrapDistance(s + ds);
	}

	/// 便利：用目前速度與 dt 計算「距離位移 ds」
	public float StepDistance(float deltaTime) => speed * Mathf.Max(0f, deltaTime);

	/// 便利：從當前世界座標，計算「起始 s」。
	public float ComputeStartDistanceFromWorld(Vector3 worldPos) => ProjectPointToDistance(worldPos);

	// === 端點/方向判定（雙向通行需求） ===
	public Vector3 GetHeadPosition() => EvaluatePositionByDistance(0f);
	public Vector3 GetTailPosition() => EvaluatePositionByDistance(Mathf.Max(0f, totalLength));
	public bool HasEnds() => totalLength > 0f && !loop;

	/// 決定：按鍵後應從「頭(0)」或「尾(totalLength)」開始，並給出行進方向(+1 前進，-1 反向)
	public void DecideStartAndDirection(Vector3 worldPos, out float startS, out int dirSign)
	{
		if (!HasEnds())
		{
			// loop：沒有端點概念；取就近投影 s，方向依切線與指向曲線向量的點積估
			float s0 = ProjectPointToDistance(worldPos);
			Vector3 tangent = EvaluateTangentByDistance(s0);
			Vector3 toCurve = EvaluatePositionByDistance(s0) - worldPos;
			dirSign = Vector3.Dot(tangent, toCurve) < 0f ? +1 : -1;
			startS = s0;
			return;
		}

		Vector3 head = GetHeadPosition();
		Vector3 tail = GetTailPosition();
		float dHead2 = (worldPos - head).sqrMagnitude;
		float dTail2 = (worldPos - tail).sqrMagnitude;

		if (dHead2 <= dTail2)
		{
			startS = 0f;
			dirSign = +1; // 頭 → 尾
		}
		else
		{
			startS = totalLength;
			dirSign = -1; // 尾 → 頭
		}
	}

	/// 搭乘時每幀推進（方向 +1/-1）
	public float StepAlong(float s, int dirSign, float deltaTime)
	{
		float ds = StepDistance(deltaTime) * Mathf.Sign(dirSign);
		float next = AdvanceByDistance(s, ds);
		if (!loop)
			next = Mathf.Clamp(next, 0f, totalLength);
		return next;
	}

	// =========================
	// ===== 內部建構流程 =====
	// =========================
	private bool HasCurve()
	{
		return bakedPoints != null && bakedPoints.Count >= 2 && totalLength > 0f;
	}

	private float WrapDistance(float s)
	{
		if (!loop) return Mathf.Clamp(s, 0f, Mathf.Max(totalLength, 1e-5f));
		if (totalLength <= 0f) return 0f;
		s %= totalLength;
		if (s < 0) s += totalLength;
		return s;
	}

	private int FindSampleIndexByDistance(float s, out float segStartLen)
	{
		int lo = 0, hi = cumulativeLengths.Count - 2; // 至少兩點
		while (lo <= hi)
		{
			int mid = (lo + hi) >> 1;
			if (cumulativeLengths[mid + 1] <= s) lo = mid + 1;
			else if (cumulativeLengths[mid] > s) hi = mid - 1;
			else
			{
				segStartLen = cumulativeLengths[mid];
				return mid;
			}
		}
		int idx = Mathf.Clamp(lo, 0, cumulativeLengths.Count - 2);
		segStartLen = cumulativeLengths[idx];
		return idx;
	}

	private void RebuildIfNeeded()
	{
		if (WaypointCount < 2)
		{
			bakedPoints.Clear();
			cumulativeLengths.Clear();
			totalLength = 0f;
			return;
		}
		BakeSpline();
	}

	/// 將路點以 Catmull-Rom 補間，烘焙成折線 + 長度表
	private void BakeSpline()
	{
		bakedPoints.Clear();
		cumulativeLengths.Clear();
		totalLength = 0f;

		// 控制點陣列
		List<Vector3> ctrl = new List<Vector3>();
		for (int i = 0; i < WaypointCount; i++)
			ctrl.Add(GetPoint(i));

		if (loop)
		{
			ctrl.Insert(0, ctrl[WaypointCount - 1]);
			ctrl.Add(ctrl[1]); // 原始第一個
			ctrl.Add(ctrl[2]); // 原始第二個
		}
		else
		{
			Vector3 p0 = ctrl[0];
			Vector3 p1 = ctrl[1];
			Vector3 pn_1 = ctrl[^2];
			Vector3 pn = ctrl[^1];

			Vector3 pre = p0 + (p0 - p1);       // p(-1)
			Vector3 post = pn + (pn - pn_1);    // p(n+1)
			ctrl.Insert(0, pre);
			ctrl.Add(post);
		}

		int originalSegCount = WaypointCount - 1;
		int effectiveSeg = loop ? WaypointCount : originalSegCount;

		for (int seg = 0; seg < effectiveSeg; seg++)
		{
			int i0 = seg + 0;
			Vector3 P0 = ctrl[i0 + 0];
			Vector3 P1 = ctrl[i0 + 1];
			Vector3 P2 = ctrl[i0 + 2];
			Vector3 P3 = ctrl[i0 + 3];

			int steps = Mathf.Max(2, samplesPerSegment);
			for (int s = 0; s < steps; s++)
			{
				float t = (float)s / (steps - 1);
				Vector3 p = CatmullRom(P0, P1, P2, P3, t);
				AppendBakedPoint(p);
			}
			// 避免段與段重複最後一點（除非最後一段）
			if (seg < effectiveSeg - 1 && bakedPoints.Count > 0)
			{
				bakedPoints.RemoveAt(bakedPoints.Count - 1);
				cumulativeLengths.RemoveAt(cumulativeLengths.Count - 1);
			}
		}

		totalLength = cumulativeLengths.Count > 0 ? cumulativeLengths[^1] : 0f;

		// 防護：若 totalLength 幾乎為 0，至少保留兩點
		if (totalLength <= 1e-6f && bakedPoints.Count >= 1)
		{
			if (bakedPoints.Count == 1)
			{
				bakedPoints.Add(bakedPoints[0] + Vector3.right * 0.001f);
				cumulativeLengths.Add(0.001f);
				totalLength = 0.001f;
			}
		}
	}

	private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		// 標準 Catmull-Rom（tension = 0.5）
		float t2 = t * t;
		float t3 = t2 * t;

		return 0.5f * (
			(2f * p1) +
			(-p0 + p2) * t +
			(2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
			(-p0 + 3f * p1 - 3f * p2 + p3) * t3
		);
	}

	private void AppendBakedPoint(Vector3 p)
	{
		if (bakedPoints.Count == 0)
		{
			bakedPoints.Add(p);
			cumulativeLengths.Add(0f);
		}
		else
		{
			Vector3 last = bakedPoints[^1];
			float seg = (p - last).magnitude;
			if (seg < 1e-8f) return; // 避免零長度點
			bakedPoints.Add(p);
			cumulativeLengths.Add(cumulativeLengths[^1] + seg);
		}
	}

	// =========================
	// ===== 編輯器可視化 =====
	// =========================
	private void OnDrawGizmos()
	{
		// 畫出控制路點
		if (waypoints != null)
		{
			Gizmos.color = new Color(0.2f, 1f, 1f, 0.8f);
			for (int i = 0; i < WaypointCount; i++)
				if (waypoints[i]) Gizmos.DrawSphere(waypoints[i].position, 0.06f);
		}

		// 畫出樣條折線與方向箭頭
		if (bakedPoints != null && bakedPoints.Count >= 2)
		{
			Gizmos.color = Color.cyan;
			for (int i = 0; i < bakedPoints.Count - 1; i++)
				Gizmos.DrawLine(bakedPoints[i], bakedPoints[i + 1]);

			// 每隔若干長度畫方向箭頭
			Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.9f);
			float arrowStep = 1.0f;
			for (float s = 0; s < totalLength; s += arrowStep)
			{
				Vector3 p = EvaluatePositionByDistance(s);
				Vector3 t = EvaluateTangentByDistance(s);
				Vector3 n = new Vector3(-t.y, t.x, 0f);
				float aSize = 0.2f;
				Gizmos.DrawLine(p, p - t * aSize + n * (aSize * 0.4f));
				Gizmos.DrawLine(p, p - t * aSize - n * (aSize * 0.4f));
			}

			// loop 尾首相連輔助線（可選）
			if (loop && bakedPoints.Count >= 2)
			{
				Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
				Gizmos.DrawLine(bakedPoints[^1], bakedPoints[0]);
			}
		}
	}
}
