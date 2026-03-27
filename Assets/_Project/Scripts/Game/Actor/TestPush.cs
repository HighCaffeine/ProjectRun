using UnityEngine;

public class TestPush : MonoBehaviour
{
    [Header("Spawn Settings (S Key)")]

    public Transform playerPivot;
    // 스폰 시킬 플레이어 프리팹 (인스펙터에서 등록)
    public GameObject playerPrefab;
    // 나(A)의 정면 몇 미터 앞에 스폰 시킬지
    public float spawnDistanceOffset = 3f;

    [Header("Punch Settings (A Key)🎯")]
    public float punchForce = 40f;      // 펀치 최대 힘 (조금 더 세게 잡았습니다)
    public float punchRange = 10f;       // 펀치 사거리
    public float maxAngle = 60f;        // 정면 각도 (좌우 60도 = 총 120도 부채꼴)
    public float upwardsModifier = 2.5f; // 띄우는 힘 (맛도리 핵심)

    [Tooltip("3=묵직, 5=범위내 무조건 확정킬 느낌")]
    public float punchyExponent = 3f;   // 다항식 지수 (가짜 로그 감쇠)

    void Update()
    {
        // 1. S 키 누르면 정면에 스폰
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SpawnTargetPlayer();
        }

        // 2. A 키 누르면 '미친 타격감' 펀치 발동!
        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformPunch();
        }
    }

    // --- S 키: 정면에 타겟 스폰 ---
    void SpawnTargetPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("플레이어 프리팹을 인스펙터에 등록해 주세요!");
            return;
        }

        // 나(A)의 현재 위치 + 정면 방향 * 오프셋
        Vector3 spawnPos = transform.position + playerPivot.forward * spawnDistanceOffset;

        // 발이 땅에 파묻히지 않게 살짝 띄워서 스폰
        spawnPos.y += 0.5f;

        // 프리팹 생성 (A와 같은 회전값으로 스폰)
        GameObject spawned = Instantiate(playerPrefab, spawnPos, playerPivot.rotation);
        spawned.name = $"Dummy_Target_{Time.frameCount}"; // 이름 구분용

        Debug.Log($"정면에 {spawnPos} 위치로 타겟 B를 스폰했습니다.");
    }

    // --- A 키: 정면 타겟팅 펀치 (+다항식 감쇠) ---
    void PerformPunch()
    {
        // 1. 범위 내 모든 콜라이더 탐색
        Collider[] colliders = Physics.OverlapSphere(transform.position, punchRange);
        Rigidbody closestRb = null;
        float minDistance = float.MaxValue;

        // 2. 가장 가까운 정면의 타겟(B) 찾기
        foreach (Collider hit in colliders)
        {
            if (hit.gameObject == gameObject) continue; // 나 자신 제외

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // A(나) -> B(타겟) 방향
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

        // 3. 타겟이 있다면 '맛도리 공식' 적용해서 AddForce
        if (closestRb != null)
        {
            // 방향 결정 및 정규화
            Vector3 pushDir = closestRb.position - transform.position;
            if (minDistance == 0f) pushDir = transform.forward;
            else pushDir.Normalize();

            // 🌟 거리 비례 비선형(가짜 로그) 감쇠 계산: 1 - x^k 🎯
            // 팀원에게 설명한 그 '극대화된 감쇠 수치'가 여기서 나옵니다.
            float x = Mathf.Clamp01(minDistance / punchRange);
            float falloffMultiplier = 1f - Mathf.Pow(x, punchyExponent);

            // 위로 띄우기 (Y값 강제 추가)
            pushDir.y += upwardsModifier;
            pushDir.Normalize(); // 띄우는 방향 추가 후 다시 정규화

            // 4. 최종 힘 연산: 방향 * 힘 * 맛도리 계수 * 질량
            // 질량을 곱해서 거대 보스도 시원하게 날리기
            Vector3 finalForce = pushDir * punchForce * falloffMultiplier * closestRb.mass;

            // Impulse 모드로 즉각 타격!
            closestRb.AddForce(finalForce, ForceMode.Impulse);

            Debug.Log($"[{closestRb.name}] 명중! 거리: {minDistance:F1}m, 힘 비율: {falloffMultiplier * 100:F0}%");
        }
        else
        {
            Debug.LogWarning("정면 범위 내에 때릴 타겟 B가 없습니다.");
        }
    }
}