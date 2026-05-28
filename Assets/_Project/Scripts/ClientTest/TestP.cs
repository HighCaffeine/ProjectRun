using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class TestP : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    public float moveSpeed = 3f;

    private Vector3 targetPos;
    private Vector3 lastPos;

    private List<PlayerActor> players = new List<PlayerActor>();

    private void Start()
    {
        transform.position = pointA.position;

        targetPos = pointB.position;
        lastPos = transform.position;

        StartCoroutine(MoveLoop());
    }

    private void LateUpdate()
    {
        Vector3 delta = transform.position - lastPos;

        foreach (PlayerActor player in players)
        {
            if (player == null) continue;

            // 이동량 적용
            player.SetPlatformDelta(delta);

            // 부모 연결
            if (player.transform.parent != transform)
            {
                player.transform.SetParent(transform);
            }
        }

        lastPos = transform.position;
    }

    private IEnumerator MoveLoop()
    {
        while (true)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPos;

            targetPos =
                (targetPos == pointA.position)
                ? pointB.position
                : pointA.position;

            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerActor player = other.GetComponent<PlayerActor>();

        if (player == null) return;

        if (!players.Contains(player))
        {
            players.Add(player);
        }

        player.transform.SetParent(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerActor player = other.GetComponent<PlayerActor>();

        if (player == null) return;

        if (players.Contains(player))
        {
            players.Remove(player);
        }

        player.SetPlatformDelta(Vector3.zero);

        if (player.transform.parent == transform)
        {
            player.transform.SetParent(null);
        }
    }
}