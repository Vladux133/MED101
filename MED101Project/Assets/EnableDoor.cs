using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class EnableDoor : MonoBehaviour
{
    public GameObject[] Door;
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.CompareTag("Player"))
        {
            Debug.Log("Player Entered collider");
            for (int i = 0; i <= Door.Length - 1; i++)
            {
                Door[i].GetComponent<CircularDrive>().enabled = true;
            }
        }
    }
}
