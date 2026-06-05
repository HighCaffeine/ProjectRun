// using System.Collections;
// using System.Collections.Generic;
// using NUnit.Framework;
// using UnityEngine;

// public class DeadZone : MonoBehaviour
// {
//     private List<PlayerActor> players;
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player")) ActorManager.Instance.OnPlayerDead(other.name);
//     }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.red;
//         BoxCollider col = GetComponent<BoxCollider>();
//         if (col == null) return;

//         // ���� ���� �߽� ��ġ
//         Vector3 center = transform.TransformPoint(col.center);

//         // ������ �ݿ��� ���� ũ��
//         Vector3 size = Vector3.Scale(col.size, transform.lossyScale);

//         Gizmos.DrawWireCube(center, size);
//     }
// }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) ActorManager.Instance.OnPlayerDead(other.name);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerActor actor = other.GetComponent<PlayerActor>();
            // 이미 죽어서 공중에 떠 있는 상태가 아닐 때만 다시 죽임
            if (actor != null && !actor.isDead)
            {
                ActorManager.Instance.OnPlayerDead(other.name);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Vector3 center = transform.TransformPoint(col.center);
        Vector3 size = Vector3.Scale(col.size, transform.lossyScale);
        Gizmos.DrawWireCube(center, size);
    }
}