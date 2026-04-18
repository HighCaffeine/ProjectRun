using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private float moveSpeed = 3f;

    [SerializeField]
    private List<PlayerActor> players = new List<PlayerActor>();
    private Vector3 lastPos;

    public void SetStartPos(GameObject start) { startPos = start; }
    public void SetEndPos(GameObject end) { endPos = end; }

    private void Start()
    {
        lastPos = transform.position;
        StartCoroutine(MoveLoop()); // 시작하자마자 이동
    }

    private void LateUpdate()
    {
        Vector3 delta = transform.position - lastPos;

        foreach (var actor in players)
        {
            if (actor != null)
                actor.SetPlatformDelta(delta);
        }

        lastPos = transform.position;
    }

    public void AddPlayer(PlayerActor actor)
    {
        if (actor == null) return;

        if (!players.Contains(actor))
        {
            players.Add(actor);
        }
    }

    public void RemovePlayer(PlayerActor actor)
    {
        if (actor == null) return;

        if (players.Contains(actor))
        {
            players.Remove(actor);
        }
    }

    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    IEnumerator MoveLoop()
    {
        while (true)
        {
            yield return MoveTo(endPos.position);
            yield return MoveTo(startPos.position);
        }
    }
}