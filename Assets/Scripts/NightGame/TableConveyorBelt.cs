using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class TableConveyorBelt : MonoBehaviour
{
	[Header("Path (Waypoints)")]
	[SerializeField] private List<Transform> waypoints = new List<Transform>(); // ���I�]�ܤ� 2 �ӡ^
	[SerializeField] private bool loop = false;

	[Header("Spline Settings")]
	[SerializeField, Range(4, 200)] private int samplesPerSegment = 20; // �C�q���u�M�H�I�ơ]�V���V���ơB�����V�j�^
	[SerializeField] private bool autoRebuildOnChange = true;

	[Header("Ride Settings")]
	[SerializeField, Min(0.01f)] private float speed = 20f;  // m/s
	[SerializeField] private bool snapOnStart = true;
	[SerializeField] private float exitSideOffset = 0.2f;

	// === Boarding Surface ===
	private Collider2D boardCollider; // ���w�ΨӧP�_�u�i�H�f���v���I�����]�q�`�O�ୱ/��e�a���^
	public Collider2D BoardCollider
	{
		get
		{
			if (!boardCollider) boardCollider = GetComponent<Collider2D>();
			return boardCollider;
		}
	}

	// ---- �M�H�᪺��ơ]���u�I�B���ת�^ ----
	private List<Vector3> bakedPoints = new List<Vector3>();
	private List<float> cumulativeLengths = new List<float>(); // �P bakedPoints ����
	private float totalLength = 0f;

	// ====== ��~�ݩ� ======
	public bool Loop => loop;
	public bool SnapOnStart => snapOnStart;
	public float ExitSideOffset => exitSideOffset;
	public float Speed => speed;
	public int WaypointCount => waypoints?.Count ?? 0;
	public float TotalLength => totalLength;

	private void Awake()
	{
		// ���A�j��]�� Trigger�F���\����I��
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

	/// ���o�� i �Ӹ��I�@�ɮy�С]�O�d�� API ��K�L��^
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

	/// �Ǧ^�G�b���u�W�u�Z���_�I s�]���ء^�v��������m
	public Vector3 EvaluatePositionByDistance(float s)
	{
		if (!HasCurve()) return transform.position;
		s = WrapDistance(s);
		int i = FindSampleIndexByDistance(s, out float segStartLen);
		float segLen = cumulativeLengths[i + 1] - segStartLen;
		float t = segLen > 1e-6f ? (s - segStartLen) / segLen : 0f;
		return Vector3.LerpUnclamped(bakedPoints[i], bakedPoints[i + 1], t);
	}

	/// �Ǧ^�G�b���u�W�Z�� s ���u�����u�V�q�v
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

	/// 2D ���k�u�]�i�Ω�U���ɰ����קK�d��^
	public Vector3 EvaluateLeftNormalByDistance(float s)
	{
		Vector3 t = EvaluateTangentByDistance(s);
		return new Vector3(-t.y, t.x, 0f);
	}

	/// ��@�ɮy�Ч�v�즱�u�W�A�^�ǡu�Z�� s�v
	public float ProjectPointToDistance(Vector3 worldPos)
	{
		if (!HasCurve()) return 0f;

		// ���ʲ��G��M�H�I���̪��
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

		// �A�b�۾F�u�q�W���@����v�Ӥ�
		int i0 = Mathf.Max(0, closest - 1);
		int i1 = Mathf.Min(bakedPoints.Count - 2, closest);
		float bestS = cumulativeLengths[closest]; // �w�]���I����

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
	/// ���ը������b�Ʀ檺���a�A�êu�u���a��J��V�v�⪱�a�����즹��]BoardCollider�^�~��
	/// �Z���� exitSideOffset�C�Y��J��V�P�Ʀ��V�ۦP�A�h�������B�~��Ʀ�C
	/// </summary>
	public bool TryCancelSlideAndEject(Rigidbody2D rb, float s, int slideDir, Vector2 inputDir, float sameDirDotThreshold = 0.75f)
	{
		if (rb == null) return false;

		// �ثe��i��V�]�� slideDir �ץ��^
		Vector2 tan = ((Vector2)EvaluateTangentByDistance(s));
		if (tan.sqrMagnitude < 1e-6f) tan = Vector2.right;
		tan.Normalize();
		Vector2 travel = (slideDir >= 0) ? tan : -tan;

		// �Y���a��J�P�Ʀ��V�P�V �� ������
		if (inputDir.sqrMagnitude > 1e-6f)
		{
			Vector2 inNorm = inputDir.normalized;
			if (Vector2.Dot(inNorm, travel) >= Mathf.Clamp01(sameDirDotThreshold))
				return false;
		}

		// �P�_�a�l�󰾤W�U�Υ��k�]�M�w�h�X�b�^
		bool beltIsVertical = Mathf.Abs(travel.y) >= Mathf.Abs(travel.x);

		// ���k�u�]�H�u��i��V�v���ǡ^�A�ѵL��J�ɪ��w�]�h����V
		Vector2 leftOfTravel = ((Vector2)EvaluateLeftNormalByDistance(s)).normalized;
		if (slideDir < 0) leftOfTravel = -leftOfTravel; // �ϦV��i�ɡA��/�k�ۤ�

		// �p��h�X��V�]�Ȧb�����b�W�� +/-1�^
		Vector2 exitDir;
		if (beltIsVertical)
		{
			// �����k�h�X�G�u���Ϊ��a input.x�A�_�h�Υ���
			float signX = Mathf.Abs(inputDir.x) >= 0.2f ? Mathf.Sign(inputDir.x)
						: (Mathf.Abs(leftOfTravel.x) > 1e-6f ? Mathf.Sign(leftOfTravel.x) : 1f);
			exitDir = new Vector2(signX, 0f);
		}
		else
		{
			// ���W�U�h�X�G�u���Ϊ��a input.y�A�_�h�Υ���
			float signY = Mathf.Abs(inputDir.y) >= 0.2f ? Mathf.Sign(inputDir.y)
						: (Mathf.Abs(leftOfTravel.y) > 1e-6f ? Mathf.Sign(leftOfTravel.y) : 1f);
			exitDir = new Vector2(0f, signY);
		}

		// �ؼ��I�G�� BoardCollider �� AABB �~�t�W�A�u exitDir ���̾a�~�I�A�A�~�� ExitSideOffset
		float margin = Mathf.Max(0.01f, ExitSideOffset);
		Vector2 target;

		if (BoardCollider)
		{
			Bounds b = BoardCollider.bounds;

			if (beltIsVertical)
			{
				// �����h�X�Gx �� min/max�Ay ���b�ୱ���׽d�򤺡A�קK�h�l�׸�
				float x = exitDir.x >= 0f ? b.max.x : b.min.x;
				float y = Mathf.Clamp(rb.position.y, b.min.y, b.max.y);
				Vector2 support = new Vector2(x, y);
				target = support + exitDir * margin;
			}
			else
			{
				// �����h�X�Gy �� min/max�Ax ���b�ୱ�e�׽d��
				float y = exitDir.y >= 0f ? b.max.y : b.min.y;
				float x = Mathf.Clamp(rb.position.x, b.min.x, b.max.x);
				Vector2 support = new Vector2(x, y);
				target = support + exitDir * margin;
			}
		}
		else
		{
			// �S�� BoardCollider�G�q��e���u��m�~��
			Vector2 p = EvaluatePositionByDistance(s);
			target = p + exitDir * margin;
		}

		rb.MovePosition(target);
		return true;
	}


	/// �ھڡu�q s �X�o���Z���W�q ds�v�]�i���i�t�^�o��s�� s�]�۰ʳB�z loop�^
	public float AdvanceByDistance(float s, float ds)
	{
		if (!HasCurve()) return 0f;
		return WrapDistance(s + ds);
	}

	/// �K�Q�G�Υثe�t�׻P dt �p��u�Z���첾 ds�v
	public float StepDistance(float deltaTime) => speed * Mathf.Max(0f, deltaTime);

	/// �K�Q�G�q��e�@�ɮy�СA�p��u�_�l s�v�C
	public float ComputeStartDistanceFromWorld(Vector3 worldPos) => ProjectPointToDistance(worldPos);

	// === ���I/��V�P�w�]���V�q��ݨD�^ ===
	public Vector3 GetHeadPosition() => EvaluatePositionByDistance(0f);
	public Vector3 GetTailPosition() => EvaluatePositionByDistance(Mathf.Max(0f, totalLength));
	public bool HasEnds() => totalLength > 0f && !loop;

	/// �M�w�G��������q�u�Y(0)�v�Ρu��(totalLength)�v�}�l�A�õ��X��i��V(+1 �e�i�A-1 �ϦV)
	public void DecideStartAndDirection(Vector3 worldPos, out float startS, out int dirSign)
	{
		if (!HasEnds())
		{
			// loop�G�S�����I�����F���N���v s�A��V�̤��u�P���V���u�V�q���I�n��
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
			dirSign = +1; // �Y �� ��
		}
		else
		{
			startS = totalLength;
			dirSign = -1; // �� �� �Y
		}
	}

	/// �f���ɨC�V���i�]��V +1/-1�^
	public float StepAlong(float s, int dirSign, float deltaTime)
	{
		float ds = StepDistance(deltaTime) * Mathf.Sign(dirSign);
		float next = AdvanceByDistance(s, ds);
		if (!loop)
			next = Mathf.Clamp(next, 0f, totalLength);
		return next;
	}

	// =========================
	// ===== �����غc�y�{ =====
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
		int lo = 0, hi = cumulativeLengths.Count - 2; // �ܤ֨��I
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

	/// �N���I�H Catmull-Rom �ɶ��A�M�H����u + ���ת�
	private void BakeSpline()
	{
		bakedPoints.Clear();
		cumulativeLengths.Clear();
		totalLength = 0f;

		// �����I�}�C
		List<Vector3> ctrl = new List<Vector3>();
		for (int i = 0; i < WaypointCount; i++)
			ctrl.Add(GetPoint(i));

		if (loop)
		{
			ctrl.Insert(0, ctrl[WaypointCount - 1]);
			ctrl.Add(ctrl[1]); // ��l�Ĥ@��
			ctrl.Add(ctrl[2]); // ��l�ĤG��
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
			// �קK�q�P�q���Ƴ̫�@�I�]���D�̫�@�q�^
			if (seg < effectiveSeg - 1 && bakedPoints.Count > 0)
			{
				bakedPoints.RemoveAt(bakedPoints.Count - 1);
				cumulativeLengths.RemoveAt(cumulativeLengths.Count - 1);
			}
		}

		totalLength = cumulativeLengths.Count > 0 ? cumulativeLengths[^1] : 0f;

		// ���@�G�Y totalLength �X�G�� 0�A�ܤ֫O�d���I
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
		// �з� Catmull-Rom�]tension = 0.5�^
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
			if (seg < 1e-8f) return; // �קK�s�����I
			bakedPoints.Add(p);
			cumulativeLengths.Add(cumulativeLengths[^1] + seg);
		}
	}

	// =========================
	// ===== �s�边�i���� =====
	// =========================
	private void OnDrawGizmos()
	{
		// �e�X������I
		if (waypoints != null)
		{
			Gizmos.color = new Color(0.2f, 1f, 1f, 0.8f);
			for (int i = 0; i < WaypointCount; i++)
				if (waypoints[i]) Gizmos.DrawSphere(waypoints[i].position, 0.06f);
		}

		// �e�X�˱���u�P��V�b�Y
		if (bakedPoints != null && bakedPoints.Count >= 2)
		{
			Gizmos.color = Color.cyan;
			for (int i = 0; i < bakedPoints.Count - 1; i++)
				Gizmos.DrawLine(bakedPoints[i], bakedPoints[i + 1]);

			// �C�j�Y�z���׵e��V�b�Y
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

			// loop �����۳s���U�u�]�i��^
			if (loop && bakedPoints.Count >= 2)
			{
				Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
				Gizmos.DrawLine(bakedPoints[^1], bakedPoints[0]);
			}
		}
	}
}
