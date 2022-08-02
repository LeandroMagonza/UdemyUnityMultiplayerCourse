using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static SpawnManager instance;
    public Transform[] spawnPoints;

    private void Awake(){
        instance = this;
    }

    void Start()
    {
        foreach (Transform spawn in spawnPoints){
            spawn.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    public Transform GetSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
