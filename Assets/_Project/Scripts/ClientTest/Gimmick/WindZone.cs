using UnityEngine;

public class WindZone : MonoBehaviour
{
    [Header("바람 설정")]
    public Vector3 windDirection = Vector3.left; // 바람 방향
    public float windPower = 1f; // 세기


    private void OnTriggerStay(Collider other)
    {
        PlayerActor player = other.GetComponent<PlayerActor>();
        if (player != null)
        {
            player.SetWind(windDirection, windPower);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerActor>())
        {
            Debug.Log("플레이어가 바람 존에 들어왔습니다.");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerActor>())
        {
            Debug.Log("플레이어가 바람 존에서 나갔습니다.");
        }
    }

    private void OnDrawGizmos()
    {
        // 바람 존의 범위를 시각적으로 표시하기 위해 Gizmos를 사용
        Gizmos.color = new Color(0, 1, 0, 0.5f); // 반투명한 녹색
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
        }
    }
}
