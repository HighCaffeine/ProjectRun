using UnityEngine;

public class TestPush : MonoBehaviour
{
    [Header("Spawn Settings (S Key)")]

    public Transform playerPivot;
    public GameObject playerPrefab;
    public float spawnDistanceOffset = 3f;

    [Header("Punch Settings")]
    public float punchForce = 40f;      // 최대 힘
    public float punchRange = 10f;       // 사거리
    public float maxAngle = 60f;        // 정면 각도 (좌우 60도)
    public float upwardsModifier = 2.5f;

    public float punchyExponent = 3f;   // 다항식 지수

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SpawnTargetPlayer();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformPunch();
        }
    }

    void SpawnTargetPlayer()
    {
        if (playerPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = transform.position + playerPivot.forward * spawnDistanceOffset;

        spawnPos.y += 0.5f;

        GameObject spawned = Instantiate(playerPrefab, spawnPos, playerPivot.rotation);
        spawned.name = $"Dummy_Target_{Time.frameCount}";

        //Debug.Log($"정면에 {spawnPos} 위치로 타겟 B를 스폰했습니다.");
    }

    void PerformPunch()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, punchRange);
        Rigidbody closestRb = null;
        float minDistance = float.MaxValue;

        foreach (Collider hit in colliders)
        {
            if (hit.gameObject == gameObject) continue;

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dirToTarget = rb.position - transform.position;
                float distance = dirToTarget.magnitude;

                // 정면 각도 체크
                float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

                // 시야각 안이고 가장 가까운 놈 선정
                if (angleToTarget <= maxAngle && distance < minDistance)
                {
                    closestRb = rb;
                    minDistance = distance;
                }
            }
        }

        if (closestRb != null)
        {
            // 방향 결정 및 정규화
            Vector3 pushDir = closestRb.position - transform.position;
            if (minDistance == 0f) pushDir = transform.forward;
            else pushDir.Normalize();

            float x = Mathf.Clamp01(minDistance / punchRange);
            float falloffMultiplier = 1f - Mathf.Pow(x, punchyExponent);

            // 위로 띄우기 (Y값 강제 추가)
            pushDir.y += upwardsModifier;
            pushDir.Normalize(); // 띄우는 방향 추가 후 다시 정규화

            Vector3 finalForce = pushDir * punchForce * falloffMultiplier * closestRb.mass;

            closestRb.AddForce(finalForce, ForceMode.Impulse);

            //Debug.Log($"[{closestRb.name}] 명중 거리: {minDistance:F1}m, 힘 비율: {falloffMultiplier * 100:F0}%");
        }
    }
}