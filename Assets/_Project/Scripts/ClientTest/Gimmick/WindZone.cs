using UnityEngine;

public class WindZone : MonoBehaviour
{
    [Header("바람 설정")]
    public Vector3 windDirection; // 바람 방향
    public float windPower = 1f; // 세기


    private void OnTriggerStay(Collider other)
    {
        windDirection = transform.forward; // WindZone의 앞 방향을 바람 방향으로 설정
        PlayerActor player = other.GetComponent<PlayerActor>();
        if (player != null)
        {
            player.SetWind(transform.forward, windPower);
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
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 20);

        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
        }
    }
}
