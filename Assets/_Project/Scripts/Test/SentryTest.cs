using UnityEngine;
using Sentry.Unity;

public class SentryTest : MonoBehaviour
{
    private void Start()
    {
        SentrySdk.CaptureMessage("Test event");
    }
}
