using UnityEngine;

[ExecuteAlways]
public class MapModuleGrid : MonoBehaviour
{
    [Header("모듈 영역 설정")]
    public Vector3 areaSize = new Vector3(10, 0, 10);   // 추출할 전체 영역 크기
    public float cellSize = 1f;                         // 한 칸의 크기 (Cell)
    public Color gridColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("자동 스냅 기능")]
    public bool enableAutoSnap = true;                  // 켜두면 자식 오브젝트들이 Cell에 맞춰짐

    void Update()
    {
        // 에디터에서 자식 오브젝트(기믹 등)를 움직일 때 Cell에 자동으로 자석처럼 붙게
        if (enableAutoSnap && !Application.isPlaying)
        {
            foreach (Transform child in transform)
            {
                Vector3 pos = child.localPosition;
                pos.x = Mathf.Round(pos.x / cellSize) * cellSize;
                pos.y = Mathf.Round(pos.y / cellSize) * cellSize;
                pos.z = Mathf.Round(pos.z / cellSize) * cellSize;
                child.localPosition = pos;
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

        // 가로선
        for (float z = start.z; z <= end.z; z += cellSize)
        {
            Gizmos.DrawLine(new Vector3(start.x, transform.position.y, z), new Vector3(end.x, transform.position.y, z));
        }
        // 세로선
        for (float x = start.x; x <= end.x; x += cellSize)
        {
            Gizmos.DrawLine(new Vector3(x, transform.position.y, start.z), new Vector3(x, transform.position.y, end.z));
        }
    }
}