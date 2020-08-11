using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetNotePositionScript : MonoBehaviour
{
    public GameObject[] noteObjects;
    public GameObject[] otherObjects;
    [SerializeField] private Vector3[] origNotePos;
    [SerializeField] private Quaternion[] origNoteRot;
    [SerializeField] private Vector3[] origOtherPos;
    [SerializeField] private Quaternion[] origOtherRot;
    private KeyCode[] KeycodeVal;

    // Start is called before the first frame update
    void Start()
    {
        origNotePos = new Vector3[noteObjects.Length];
        origNoteRot = new Quaternion[noteObjects.Length];
        origOtherPos = new Vector3[otherObjects.Length];
        origOtherRot = new Quaternion[otherObjects.Length];
        KeycodeVal = new KeyCode[noteObjects.Length];
        
        for (int i = 0; i <= noteObjects.Length - 1; i++)
        {
            origNotePos[i] = noteObjects[i].transform.position;
            origNoteRot[i] = noteObjects[i].transform.rotation;
            int k = i + 1;
            KeycodeVal[i] = (KeyCode)System.Enum.Parse(typeof(KeyCode), "Keypad" + k.ToString());
        }

        for (int m = 0; m <= otherObjects.Length-1;m++)
        {
            origOtherPos[m] = otherObjects[m].transform.position;
            origOtherRot[m] = otherObjects[m].transform.rotation;
        }
    }

    // Update is called once per frame
    void Update()
    {
            
        if (Input.anyKeyDown) 
        {
           // Debug.Log("Key pressed!");
            for (int j = 0; j <= noteObjects.Length-1; j++)
            {
                if (Input.GetKeyDown(KeycodeVal[j])) {
                    noteObjects[j].transform.SetPositionAndRotation(origNotePos[j], origNoteRot[j]);
                    //Debug.Log("Numpad" + j + " pressed!");
                }
            }

            if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                for (int i = 0; i <= noteObjects.Length - 1; i++)
                {
                    noteObjects[i].transform.SetPositionAndRotation(origNotePos[i], origNoteRot[i]);
                }

                for (int n = 0; n <= otherObjects.Length - 1; n++)
                {
                    otherObjects[n].transform.SetPositionAndRotation(origOtherPos[n], origOtherRot[n]);
                }
                //Debug.Log("Numpad0 pressed!");
            }
        }
     }   
}
