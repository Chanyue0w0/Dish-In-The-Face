using UnityEngine;

/// <summary>
/// �M�d���a�ʵe�P���V����G
/// - �ھ� X ��V�]�w���⥪�k���V�]Y �b 0/180�^
/// - �ھڲ���/�Ĩ�/�Ʀ檬�A�]�w Animator �Ѽ�
/// </summary>
public class PlayerAnimationManager : MonoBehaviour
{
	[Header("Reference")]
	[SerializeField] private Animator animator;              // ���� Animator
	[SerializeField] private Transform characterTransform;   // �Ω�½���]�q�`�N�O���a���� Transform�^

	[Header("Animator Params")]
	[SerializeField] private string walkParam = "isWalk";
	[SerializeField] private string dashParam = "isDash";
	[SerializeField] private string slideParam = "isSlide";

	private void Reset()
	{
		// �b�s�边���s�W����ɹ��զ۰ʧ��
		if (!animator) animator = GetComponentInChildren<Animator>();
		if (!characterTransform) characterTransform = transform;
	}

	/// ��~���I��s�]�� Movement �I�s�^�G���J�ثe���ʦV�q�P���A
	public void UpdateFromMovement(Vector2 moveInput, bool isDashing, bool isSliding)
	{
		UpdateFacing(moveInput.x);
		UpdateStates(moveInput, isDashing, isSliding);
	}

	/// �u��s���V�]�i�bŪ����J��U�N�I�s�^
	public void UpdateFacing(float moveX)
	{
		if (!characterTransform) return;

		if (moveX < -Mathf.Epsilon)
			characterTransform.rotation = Quaternion.Euler(0f, 180f, 0f);
		else if (moveX > Mathf.Epsilon)
			characterTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
	}

	/// �̪��A���� Animator �ѼơG�Ʀ� > �Ĩ� > ���� > �ݾ��]�����^
	public void UpdateStates(Vector2 moveInput, bool isDashing, bool isSliding)
	{
		if (!animator) return;

		bool isWalk = moveInput != Vector2.zero;

		if (isSliding)
		{
			SetBool(walkParam, false);
			SetBool(dashParam, false);
			SetBool(slideParam, true);
		}
		else if (isDashing)
		{
			SetBool(walkParam, false);
			SetBool(dashParam, true);
			SetBool(slideParam, false);
		}
		else if (isWalk)
		{
			SetBool(walkParam, true);
			SetBool(dashParam, false);
			SetBool(slideParam, false);
		}
		else
		{
			SetBool(walkParam, false);
			SetBool(dashParam, false);
			SetBool(slideParam, false);
		}
	}

	private void SetBool(string param, bool value)
	{
		if (!string.IsNullOrEmpty(param))
			animator.SetBool(param, value);
	}
}
