using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : BaseGimmick
{
    [SerializeField]
    bool isMove = false;
    [SerializeField]
    private bool hasStarted = false;
    [SerializeField]
    GameObject startPos;
    [SerializeField]
    GameObject endPos;

    [SerializeField]
    float moveSpeed = 3f;

    [SerializeField]
    private List<PlayerActor> players = new List<PlayerActor>();
    private Vector3 lastPos;

    void Start()
    {
        lastPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMove)
        {
            StartCoroutine(MoveLoop());
            isMove = false; // �� ���� �̵� �����ϵ��� ���� 
        }
    }
    void LateUpdate()
    {
        Vector3 delta = transform.position - lastPos;

        foreach (var actor in players)
        {
            actor.SetPlatformDelta(delta);
        }

        lastPos = transform.position;
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        // 이동 시작 명령이 오면 1회만 코루틴 실행
        if (ntf.state == (byte)eGimmickState.On_Activate && !hasStarted)
        {
            hasStarted = true;
            StartCoroutine(MoveLoop());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 탑승자 등록만 수행
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null) players.Add(actor);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) other.transform.SetParent(null);
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null) players.Remove(actor);
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player") && !hasStarted)
    //     {
    //         hasStarted = true;
    //         Debug.Log("�÷��̾ �÷����� ���Խ��ϴ�.");
    //         isMove = true;
    //     }

    //     PlayerActor actor = other.GetComponent<PlayerActor>();
    //     if (actor != null)
    //     {
    //         players.Add(actor);
    //     }
    // }

    // private void OnTriggerExit(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         other.transform.SetParent(null); // �÷��̾ �÷������� �и�
    //     }

    //     PlayerActor actor = other.GetComponent<PlayerActor>();
    //     if (actor != null)
    //     {
    //         players.Remove(actor);
    //     }
    // }

    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator MoveLoop()
    {
        while (true)
        {
            yield return MoveTo(endPos.transform.position);
            yield return new WaitForSeconds(3f); // ���� �� 1�� ���
            yield return MoveTo(startPos.transform.position);
            yield return new WaitForSeconds(3f); // ��� �� 1�� ���
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPos.transform.position, endPos.transform.position);
    }
}
