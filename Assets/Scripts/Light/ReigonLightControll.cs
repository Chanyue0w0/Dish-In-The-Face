using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReigonLightControll : MonoBehaviour
{
	[Header("Rotation Settings")]
	[SerializeField] private bool isRotationEnabled = true; // �O�_�ҥα���\��
	[SerializeField] private float rotationSpeed = 30f;     // �C����ਤ�ס]�ס^

	// Update is called once per frame
	void Update()
	{
		if (isRotationEnabled)
		{
			// �H Z �b�����߶i�����]�i�̻ݨD�אּ X �� Y�^
			transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
		}
	}

	// ���ѥ~���������બ�A����k
	public void SetRotationEnabled(bool enabled)
	{
		isRotationEnabled = enabled;
	}

	// ���ѥ~���վ�t�ת���k
	public void SetRotationSpeed(float speed)
	{
		rotationSpeed = speed;
	}
}
