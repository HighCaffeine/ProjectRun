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
            other.GetComponent<PlayerActor>().ResetWind();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 start = transform.position;
        Vector3 end = transform.position + transform.forward * 20;

        Vector3 mid = (start + end) / 2;
        float length = Vector3.Distance(start, end);

        Gizmos.matrix = Matrix4x4.TRS(mid, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3(0.1f, 0.1f, length));
    }
}
