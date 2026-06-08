using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    public static PathVisualizer Instance;

    void Awake()
    {
        Instance = this;
    }

    // ���� �԰ݰ� ������ ��Ŷ ������ ���� (�ùķ��̼� ��)
    // �����δ� ��Ʈ��ũ ���̺귯���� ���� ���ۿ��� �Ľ��� ���� ����Ͻø� �˴ϴ�.
    private Vector3[] receivedPath = new Vector3[10];
    private int receivedPathCount = 0;

    // ����� ���� ����
    public Color pathColor = Color.green;
    public Color waypointColor = Color.red;
    public bool showOnlyWhenSelected = false;

    /// <summary>
    /// ��Ʈ��ũ ��⿡�� ��Ŷ�� ������ �� �Լ��� ȣ���Ͽ� �����͸� �����մϴ�.
    /// </summary>
    /// <param name="count">��ȿ�� ��� ���� ����</param>
    /// <param name="pathArray">�����κ��� ���� ��ǥ �迭</param>
    public void OnReceivePathPacket(int count, P_PacketVector3[] pathArray)
    {
        // 1. ������ ��ȿ�� �˻�
        if (count > 10) count = 10; // ���� ����(Max 10)�� ���� ������ġ

        // 2. ������ ���� (��Ʈ��ũ ���۰� ����� �� �����Ƿ� �� ���� ����)
        receivedPathCount = count;
        for (int i = 0; i < count; i++)
        {
            receivedPath[i].Set(pathArray[i].x, pathArray[i].y, pathArray[i].z);
        }
    }

    /// <summary>
    /// ������ �� �信 �׸��� �׸��� ����Ƽ �ݹ� �Լ��Դϴ�.
    /// </summary>
    void OnDrawGizmos()
    {
        if (showOnlyWhenSelected) return;
        DrawPath();
    }

    // ������Ʈ�� ���õǾ��� ���� �׸��� �ʹٸ� �� �Լ� ���
    void OnDrawGizmosSelected()
    {
        if (showOnlyWhenSelected)
            DrawPath();
    }

    void DrawPath()
    {
        // ��ΰ� ���ų� ���� 2�� �̸��̸� �׸� �� ����
        if (receivedPathCount < 2) return;

        // 1. ��� �� �׸���
        Gizmos.color = pathColor;
        for (int i = 0; i < receivedPathCount - 1; i++)
        {
            Vector3 start = receivedPath[i];
            Vector3 end = receivedPath[i + 1];

            // (�ɼ�) �ٴڿ� ������ �ʵ��� ��¦ ����� �׸���
            // start.y += 0.1f; 
            // end.y += 0.1f;

            Gizmos.DrawLine(start, end);
        }

        // 2. ��������Ʈ(���̴� ��) ǥ��
        Gizmos.color = waypointColor;
        for (int i = 0; i < receivedPathCount; i++)
        {
            Gizmos.DrawSphere(receivedPath[i], 0.3f); // ������ 0.3�� ��
        }
    }

    // ---------------------------------------------------------
    // [�׽�Ʈ��] �����Ϳ��� ��Ŭ������ ������ ��� �����͸� �־�� ���
    // ---------------------------------------------------------
    [ContextMenu("Test Simulate Receive Path")]
    public void TestReceive()
    {
        P_PacketVector3[] dummyPath = new P_PacketVector3[10];

        // ������ ��� ������ ���� (���� ��ġ ����)
        var pos = this.transform.position;
        dummyPath[0].Set(pos);
        dummyPath[1].Set(this.transform.position + new Vector3(20, 0, 20));
        dummyPath[2].Set(this.transform.position + new Vector3(50, 0, 50));
        dummyPath[3].Set(this.transform.position + new Vector3(80, 0, 20));

        // ������ ���� �ùķ��̼� (�� 4��)
        OnReceivePathPacket(4, dummyPath);

        //Debug.Log("Simulated Path Received!");
    }
}
