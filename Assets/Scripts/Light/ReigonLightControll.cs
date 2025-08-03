using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReigonLightControll : MonoBehaviour
{
	[Header("Rotation Settings")]
	[SerializeField] private bool isRotationEnabled = true;  // 是否啟用旋轉功能
	[SerializeField] private bool isClockwise = true;        // 是否順時針旋轉
	[SerializeField] private float rotationSpeed = 30f;      // 每秒旋轉角度（度）

	void Update()
	{
		if (isRotationEnabled)
		{
			// 計算實際旋轉角度，根據旋轉方向正負
			float angle = rotationSpeed * Time.deltaTime;
			angle *= isClockwise ? -1f : 1f; // 順時針為負，逆時針為正（因為Z軸正向為逆時針）

			transform.Rotate(Vector3.forward, angle);
		}
	}

	// 提供外部切換旋轉啟用狀態的方法
	public void SetRotationEnabled(bool enabled)
	{
		isRotationEnabled = enabled;
	}

	// 提供外部設定旋轉速度的方法
	public void SetRotationSpeed(float speed)
	{
		rotationSpeed = speed;
	}

	// 提供外部設定旋轉方向的方法
	public void SetRotationDirection(bool clockwise)
	{
		isClockwise = clockwise;
	}

	// 切換方向（順時針 <-> 逆時針）
	public void ToggleRotationDirection()
	{
		isClockwise = !isClockwise;
	}
}
