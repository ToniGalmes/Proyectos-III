﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPos;
    [SerializeField] private Light light;


    public void Activate()
    {
        GameManager.Instance.respawnPos = respawnPos.position;
        GameManager.Instance.checkpointSceneIdex = gameObject.scene.buildIndex;
        FindObjectOfType<PlayerController>().RestoreHealth();
        light.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            UIHelperController.Instance.EnableHelper(UIHelperController.HelperAction.NailSword, transform.position+ Vector3.up * 2);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            UIHelperController.Instance.DisableHelper();
        }
    }
}
