using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableRecorder : MonoBehaviour
{
    public RecordTransformHierarchy recorder;
    
    // Start is called before the first frame update
    void EnableRecord()
    {
        Debug.Log("RECORDER ENABLED!");
        recorder.enabled = true;
    }
}
