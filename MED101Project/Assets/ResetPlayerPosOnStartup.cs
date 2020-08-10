using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetPlayerPosOnStartup : MonoBehaviour
{
    public GameObject playerRig;
    public GameObject mover;
    public GameObject vrCam;
    
    // Start is called before the first frame update
    void Start()
    {

        Vector3 GlobalCameraPosition = vrCam.transform.position;  //get the global position of VRcamera
        Vector3 GlobalPlayerPosition = playerRig.transform.position;

        Vector3 GlobalOffsetCameraPlayer = new Vector3(GlobalCameraPosition.x - GlobalPlayerPosition.x, 0, GlobalCameraPosition.z - GlobalPlayerPosition.z);

        Vector3 newRigPosition = new Vector3(mover.transform.position.x - GlobalOffsetCameraPlayer.x, playerRig.transform.position.y, mover.transform.position.z - GlobalOffsetCameraPlayer.z);
        playerRig.transform.position = newRigPosition;

    }

}
