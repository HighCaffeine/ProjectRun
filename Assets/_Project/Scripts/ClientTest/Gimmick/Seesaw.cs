using System.Collections.Generic;
using UnityEngine;

public class Seesaw : MonoBehaviour
{
    public float force = 50f;

    private Rigidbody rb;
    private List<Transform> players = new List<Transform>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter: " + other.name);
        if (other.GetComponent<PlayerActor>())
        {
            players.Add(other.transform);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerActor>())
        {
            players.Remove(other.transform);
        }
    }

    void FixedUpdate()
    {
        foreach (var player in players)
        {
            Vector3 localPos = transform.InverseTransformPoint(player.position);

            // x축 기준으로 좌우 판단
            float direction = localPos.x;

            // 힘 적용 (끝으로 갈수록 강하게)
            rb.AddTorque(Vector3.forward * -direction * force);
        }
    }
}
