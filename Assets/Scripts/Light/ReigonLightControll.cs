using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReigonLightControll : MonoBehaviour
{
	[Header("Rotation Settings")]
	[SerializeField] private bool isRotationEnabled = true; // 是否啟用旋轉功能
	[SerializeField] private float rotationSpeed = 30f;     // 每秒旋轉角度（度）

	// Update is called once per frame
	void Update()
	{
		if (isRotationEnabled)
		{
			// 以 Z 軸為中心進行旋轉（可依需求改為 X 或 Y）
			transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
		}
	}

	// 提供外部切換旋轉狀態的方法
	public void SetRotationEnabled(bool enabled)
	{
		isRotationEnabled = enabled;
	}

	// 提供外部調整速度的方法
	public void SetRotationSpeed(float speed)
	{
		rotationSpeed = speed;
	}
}
