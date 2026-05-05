using UnityEngine;

public class TestSpawner : MonoBehaviour
{
    private float spawnTimer;
    private float spawnInterval = 3f;
    private int maxZombies = 10;

    void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            if (GameObject.FindGameObjectsWithTag("Zombie").Length < maxZombies)
            {
                ZombieSpawner.Instance.Spawn(ZombieType.Thrower, speed: 3.5f, health: 100f, position: new Vector3(0, 0, 5));
            }
        }
    }
}