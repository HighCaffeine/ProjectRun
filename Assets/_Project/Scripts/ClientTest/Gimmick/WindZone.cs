using UnityEngine;

public class WindZone : MonoBehaviour
{
    [Header("�ٶ� ����")]
    public Vector3 windDirection; // �ٶ� ����
    public float windPower = 1f; // ����


    private void OnTriggerStay(Collider other)
    {
        windDirection = transform.forward; // WindZone�� �� ������ �ٶ� �������� ����
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
            Debug.Log("�÷��̾ �ٶ� ���� ���Խ��ϴ�.");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerActor>())
        {
            Debug.Log("�÷��̾ �ٶ� ������ �������ϴ�.");
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
