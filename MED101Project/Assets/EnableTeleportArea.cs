using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableTeleportArea : MonoBehaviour
{
    public GameObject[] TeleportAreaObjects;

     void OnTriggerEnter(Collider other)
    {
        if(other.transform.CompareTag("Player"))
        {
            for (int i = 0; i <= TeleportAreaObjects.Length-1;i++)
            {
                TeleportAreaObjects[i].SetActive(true);
            }
        }
    }
}
