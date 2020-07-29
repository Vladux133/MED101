using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    // Start is called before the first frame update
    public void KillObject()
    {
        Destroy(this.gameObject);
    }
}
