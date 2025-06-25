using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{
	[Header("Chair List")]
	[SerializeField] private Transform[] chairList; // �Ҧ��Ȥl��m
	private HashSet<Transform> occupiedChairs = new HashSet<Transform>(); // ���b�Q�ϥΪ��Ȥl���X

	/// �H���M��@�ӪŪ��Ȥl�üаO���w���ΡC�䤣��h�^�� null�C
	public Transform FindEmptyChair()
	{
		// �إߤ@�ӲM��Ӧs��Ҧ��|���Q���Ϊ��Ȥl
		List<Transform> availableChairs = new List<Transform>();

		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
				availableChairs.Add(chair);
		}

		// �Y�S���ŴȤl�A�^�� null
		if (availableChairs.Count == 0)
			return null;

		// �H������䤤�@�i�Ȥl
		Transform selectedChair = availableChairs[Random.Range(0, availableChairs.Count)];

		// �аO���w����
		occupiedChairs.Add(selectedChair);

		return selectedChair;
	}

	/// ��ȤH���u������Ȥl�C
	public void ReleaseChair(Transform targetChair)
	{
		if (occupiedChairs.Contains(targetChair))
			occupiedChairs.Remove(targetChair);
	}
}
