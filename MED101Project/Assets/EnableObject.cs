using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class EnableObject : MonoBehaviour
{
    public GameObject[] GObject;
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            Debug.Log("Player Entered collider");
            for (int i = 0; i <= GObject.Length - 1; i++)
            {
                GObject[i].SetActive(true);
            }
        }
    }
}
