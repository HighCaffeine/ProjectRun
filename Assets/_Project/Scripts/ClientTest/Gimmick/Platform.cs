using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
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
            isMove = false; // 한 번만 이동 시작하도록 설정 
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
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasStarted)
        {
            hasStarted = true;
            Debug.Log("플레이어가 플랫폼에 들어왔습니다.");
            isMove = true;
        }

        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null)
        {
            players.Add(actor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.SetParent(null); // 플레이어를 플랫폼에서 분리
        }

        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null)
        {
            players.Remove(actor);
        }
    }

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
            yield return new WaitForSeconds(3f); // 도착 후 1초 대기
            yield return MoveTo(startPos.transform.position);
            yield return new WaitForSeconds(3f); // 출발 후 1초 대기
        }
    }

}