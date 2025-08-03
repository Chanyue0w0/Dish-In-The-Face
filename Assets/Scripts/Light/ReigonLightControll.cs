using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReigonLightControll : MonoBehaviour
{
	[Header("Rotation Settings")]
	[SerializeField] private bool isRotationEnabled = true;  // �O�_�ҥα���\��
	[SerializeField] private bool isClockwise = true;        // �O�_���ɰw����
	[SerializeField] private float rotationSpeed = 30f;      // �C����ਤ�ס]�ס^

	void Update()
	{
		if (isRotationEnabled)
		{
			// �p���ڱ��ਤ�סA�ھڱ����V���t
			float angle = rotationSpeed * Time.deltaTime;
			angle *= isClockwise ? -1f : 1f; // ���ɰw���t�A�f�ɰw�����]�]��Z�b���V���f�ɰw�^

			transform.Rotate(Vector3.forward, angle);
		}
	}

	// ���ѥ~����������ҥΪ��A����k
	public void SetRotationEnabled(bool enabled)
	{
		isRotationEnabled = enabled;
	}

	// ���ѥ~���]�w����t�ת���k
	public void SetRotationSpeed(float speed)
	{
		rotationSpeed = speed;
	}

	// ���ѥ~���]�w�����V����k
	public void SetRotationDirection(bool clockwise)
	{
		isClockwise = clockwise;
	}

	// ������V�]���ɰw <-> �f�ɰw�^
	public void ToggleRotationDirection()
	{
		isClockwise = !isClockwise;
	}
}
