using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner instance;

    public GameObject playerPrefab;
    private GameObject player;
    public GameObject deathEffect;
    public float respawnTime = 3f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }
    public void Die(string damager){
        // SpawnPlayer();
        if (player != null)
        {
            MatchManager.instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
            StartCoroutine(DieCo(damager));
        }
    }

    public IEnumerator DieCo(string damager){
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player);
        player = null;

        UIController.instance.deathText.text = "You were killed by " + damager;
        UIController.instance.deathScreen.SetActive(true);
        UIController.instance.overheatedMessage.gameObject.SetActive(false);

        yield return new WaitForSeconds(respawnTime);

        UIController.instance.deathScreen.SetActive(false);
        
        if (MatchManager.instance.state == MatchManager.GameState.Playing && player == null)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();
        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }
}
