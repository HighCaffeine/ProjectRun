using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossTest : MonoBehaviour
{
    [SerializeField]
    private List<WayPoint> wayPoints;
    [SerializeField]
    private float moveSpeed = 2f;
    [SerializeField]
    private int currentIndex = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(BossRoutine());
    }

    // Update is called once per frame
    void Update()
    {

    }
    IEnumerator BossRoutine()
    {
        while (true)
        {
            WayPoint target = wayPoints[currentIndex];

            yield return StartCoroutine(BossMove(target.transform.position));//이동

            yield return new WaitForSeconds(target.deleyTime);//대기

            currentIndex++;

            if (currentIndex >= wayPoints.Count)
            {
                currentIndex = 0;
            }
        }
    }

    IEnumerator BossMove(Vector3 targetPos)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
           
            yield return null;
        }

    }


    void OnDrawGizmos()
    {
        if (wayPoints == null) return;

        Gizmos.color = Color.red;

        for (int i = 0; i < wayPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(wayPoints[i].transform.position,
                            wayPoints[i + 1].transform.position);
        }
    }
}
