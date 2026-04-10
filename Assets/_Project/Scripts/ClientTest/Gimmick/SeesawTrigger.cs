using UnityEngine;
using System.Collections.Generic;

public class SeesawTrigger : MonoBehaviour
{
    public Rigidbody boardRb;
    public float maxForce = 100f;
    public float maxDistance = 1.5f; // 보드 반 길이

    [SerializeField]
    private List<Transform> players = new List<Transform>();

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter: " + other.name);
        if (other.GetComponent<PlayerActor>())
            players.Add(other.transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerActor>())
            players.Remove(other.transform);
    }

    void FixedUpdate()
    {
        players.RemoveAll(p => p == null);

        foreach (var player in players)
        {
            Vector3 localPos = boardRb.transform.InverseTransformPoint(player.position);

            float normalized = Mathf.Clamp(localPos.x / maxDistance, -1f, 1f);

            float force = normalized * maxForce;

            /* float threshold = 0.2f; // 최소 힘 적용을 위한 임계값

             if (Mathf.Abs(normalized) < threshold)
                 continue;*/

            boardRb.AddForceAtPosition(
                Vector3.down * Mathf.Abs(force),
                player.position
            );
        }
    }
}