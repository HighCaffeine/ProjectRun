using System.Collections;
using UnityEngine;
using TMPro;
public class ReSpawnTimer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float respawnTime;
    [SerializeField] private TextMeshProUGUI text;
    private void Awake()
    {
        StartCoroutine(ReSpawnDeley(respawnTime));
    }

    IEnumerator ReSpawnDeley(float time)
    {
        while (time > 0)
        {
            text.text = time.ToString();
            yield return new WaitForSeconds(1f);
            time--;
        }
        this.gameObject.SetActive(false);
    }
}
