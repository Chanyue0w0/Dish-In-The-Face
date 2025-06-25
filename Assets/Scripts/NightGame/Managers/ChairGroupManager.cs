using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{

	[Header("Chair List")]
	[SerializeField] private Transform[] chairList; // �Ҧ��Ȥl��m
	private HashSet<Transform> occupiedChairs = new HashSet<Transform>(); // ���b�Q�ϥΪ��Ȥl���X

	/// �M��@�ӪŪ��Ȥl�üаO���w���ΡC�䤣��h�^�� null�C
	public Transform FindEmptyChair()
	{
		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
			{
				occupiedChairs.Add(chair);
				return chair;
			}
		}
		return null;
	}

	/// ��ȤH���u������Ȥl�C
	public void ReleaseChair(Transform targetChair)
	{
		if (occupiedChairs.Contains(targetChair))
			occupiedChairs.Remove(targetChair);
	}
}
