using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    private List<PlayerActor> players;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) ActorManager.Instance.OnPlayerDead(other.name);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        // ���� ���� �߽� ��ġ
        Vector3 center = transform.TransformPoint(col.center);

        // ������ �ݿ��� ���� ũ��
        Vector3 size = Vector3.Scale(col.size, transform.lossyScale);

        Gizmos.DrawWireCube(center, size);
    }
}
