using UnityEngine;

public class PlatformTrigger : MonoBehaviour
{
    private Platform platform;

    private void Awake()
    {
        platform = GetComponentInParent<Platform>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);
        PlayerActor actor = other.GetComponent<PlayerActor>();

        if (actor != null && actor.IsLocal)
        {
            platform.AddPlayer(actor);
        }
    }
    

    private void OnTriggerExit(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();

        if (actor != null && actor.IsLocal)
        {
            platform.RemovePlayer(actor);
        }
    }
}