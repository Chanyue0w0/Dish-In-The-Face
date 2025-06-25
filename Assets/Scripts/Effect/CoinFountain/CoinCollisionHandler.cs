using UnityEngine;
using System.Collections.Generic;

public class CoinCollisionHandler : MonoBehaviour
{
    public GameObject coinPrefab;  // �A�n��ڥͦ����w�� Prefab
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    void OnParticleCollision(GameObject other)
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        // ���o�I���ƥ���x�s�� List ��
        int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);

        GameObject player1 = GameObject.Find("Player1");
        //GameObject player2 = GameObject.Find("Player2");

        for (int i = 0; i < numCollisionEvents; i++)
        {
            // �b�I����m�ͦ��w��
            GameObject obj = Instantiate(coinPrefab, collisionEvents[i].intersection, Quaternion.identity);
            //coinPrefab.GetComponent<AimPlayerShootSkill>().SetPlayerNumber(2);
            //coinPrefab.GetComponent<AimPlayerShootSkill>().SetTargetNumber(1);
            //obj.GetComponent<moveToPositionSkill>().SetStartPosition(player2.transform.position);
            //obj.GetComponent<moveToPositionSkill>().SetTargetPosition(player1.transform.position);
            //obj.GetComponent<moveToPositionSkill>().SetDelayTime(2f);
        }
    }
}
