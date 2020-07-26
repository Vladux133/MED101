using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeParentObj : MonoBehaviour
{
    public GameObject PlayerGO;
    //private Vector3 OrigPos;
    public GameObject NewParent;
    
    // Start is called before the first frame update
    public void DeParent()
    {
        //OrigPos = PlayerGO.transform.localPosition;
        PlayerGO.transform.SetParent(NewParent.transform, true);
        //PlayerGO.transform.localPosition = OrigPos;
    }
}
