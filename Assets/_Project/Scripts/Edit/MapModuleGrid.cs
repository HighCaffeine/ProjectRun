using UnityEngine;
using System;

[ExecuteAlways]
public class MapModuleGrid : MonoBehaviour
{
    [Header("모듈 영역 설정")]
    public Vector3 areaSize = new Vector3(10, 0, 10);   // 추출할 전체 영역 크기
    public float cellSize = 1f;                         // 한 칸의 크기 (Cell)
    public Color gridColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("자동 스냅 기능")]
    public bool enableAutoSnap = true;                  // 켜두면 자식 오브젝트들이 Cell에 맞춰짐

    [Header("Y축 스냅 설정")]
    public bool enableSnapY = false;
    public float snapStepY = 0.5f;

    [Header("Nav 추출")]
    public bool includeNavMesh = true;

    void Update()
    {
        if (enableAutoSnap && !Application.isPlaying)
        {
            SnapAllChildren(transform);
        }
    }

    private void SnapAllChildren(Transform parentTransform)
    {
        foreach (Transform child in parentTransform)
        {
            // 위치가 변경된 오브젝트만 스냅 연산
            if (child.hasChanged)
            {
                Vector3 pos = child.localPosition;
                Vector3 scale = child.localScale;

                // (위치 - 크기 절반)을 그리드에 맞춘 뒤 다시 크기 절반을 더해줌
                pos.x = Mathf.Round((pos.x - scale.x / 2f) / cellSize) * cellSize + (scale.x / 2f);

                if (enableSnapY)
                {
                    pos.y = Mathf.Round(pos.y / snapStepY) * snapStepY;
                }

                pos.z = Mathf.Round((pos.z - scale.z / 2f) / cellSize) * cellSize + (scale.z / 2f);

                child.localPosition = pos;

                // 연산이 끝났으므로 플래그 초기화
                child.hasChanged = false;
            }

            //자식들 검사
            if (child.childCount > 0)
            {
                SnapAllChildren(child);
            }
        }
    }

    void OnDrawGizmos()
    {
        // 전체 추출 영역 테두리
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, areaSize);

        // 내부 Cell 단위 바닥 그리드 그리기
        Gizmos.color = gridColor;
        Vector3 start = transform.position - (areaSize / 2f);
        Vector3 end = transform.position + (areaSize / 2f);

        // 가로선 마지막 안잘리게 0.01추가
        for (float z = start.z; z <= end.z + 0.01f; z += cellSize)
        {
            Gizmos.DrawLine(new Vector3(start.x, transform.position.y, z), new Vector3(end.x, transform.position.y, z));
        }
        for (float x = start.x; x <= end.x + 0.01f; x += cellSize)
        {
            Gizmos.DrawLine(new Vector3(x, transform.position.y, start.z), new Vector3(x, transform.position.y, end.z));
        }
    }
}