using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeHMDPos : MonoBehaviour
{
    private Vector3 InitialPos;
    private Vector3 hmdPos;
    public GameObject HMD;
    public Transform CameraPos;

    // Start is called before the first frame update
    void Start()
    {
        InitialPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //hmdPos = HMD.transform.position;                         Can't work in world position
        //transform.position = InitialPos - hmdPos;                for some reason
        hmdPos = HMD.transform.localPosition;
        transform.position = CameraPos.position - hmdPos;
    }
}