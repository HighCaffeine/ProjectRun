using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    [SerializeField]
    private Monster monsterPrefab;

    private List<Monster> spawnedMonsters = new();

    [SerializeField]
    private int maxSpawnCount = 20;

    [SerializeField]
    private int currentSpawnCount = 0;

    [SerializeField]
    private float spawnDelay = 1f;

    private float timer;

    private bool playerInside;


    [SerializeField]
    private List<Transform> spawnPoints;
    [SerializeField]
    int spawnIndex = 0; 
    private void Update()
    {
        if (!playerInside)
            return;

        if (currentSpawnCount >= maxSpawnCount)
            return;


        for (int i = 0; i < spawnPoints.Count; i++)
        {
            spawnIndex = currentSpawnCount;
            break;

        }
        Monster monster = Instantiate(monsterPrefab,spawnPoints[spawnIndex].position, Quaternion.identity);
        Debug.Log(spawnPoints[spawnIndex].position);
        spawnedMonsters.Add(monster);

        currentSpawnCount++;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        foreach (var monster in spawnedMonsters)
        {
            if (monster != null)
            {
                Destroy(monster.gameObject);
            }
        }
        currentSpawnCount = 0;

        spawnedMonsters.Clear();
    }
}