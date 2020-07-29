
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetAnimBoolFixed : MonoBehaviour
{
    public Animator AnimatorObject;
    public string BoolName;

    void OnEnable()
    {
        //Debug.Log(BoolName + " : " + AnimatorObject.GetBool(BoolName));
        AnimatorObject.SetBool(BoolName, !AnimatorObject.GetBool(BoolName));
        //Debug.Log(BoolName + " : " + AnimatorObject.GetBool(BoolName));
    }
}
