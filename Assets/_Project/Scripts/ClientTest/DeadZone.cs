using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    { 
        Debug.Log($"[DeadZone] {other.name}이(가) 사망 구역에 진입했습니다.");
        ActorManager.Instance.OnPlayerDead(other.name);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        // 월드 기준 중심 위치
        Vector3 center = transform.TransformPoint(col.center);

        // 스케일 반영된 실제 크기
        Vector3 size = Vector3.Scale(col.size, transform.lossyScale);

        Gizmos.DrawWireCube(center, size);
    }
}
