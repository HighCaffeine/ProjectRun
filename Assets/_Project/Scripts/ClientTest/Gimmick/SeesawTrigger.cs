using UnityEngine;
using System.Collections.Generic;

public class SeesawTrigger : MonoBehaviour
{
    public Rigidbody boardRb;
    public float maxForce = 100f;
    public float maxDistance = 1.5f; // 보드 반 길이

    [SerializeField]
    private List<Transform> players = new List<Transform>();

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Trigger Enter: " + collision.transform.name);
        if (collision.transform.GetComponent<PlayerActor>())
            players.Add(collision.transform);
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.transform.GetComponent<PlayerActor>())
            players.Remove(collision.transform);
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.isHost) return;

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
