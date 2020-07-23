
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetAnimBool : MonoBehaviour
{
    public Animator AnimatorObject;

    public void SetBool(string BoolName)
    {
        //Debug.Log(BoolName + " : " + AnimatorObject.GetBool(BoolName));
        AnimatorObject.SetBool(BoolName, !AnimatorObject.GetBool(BoolName));
        //Debug.Log(BoolName + " : " + AnimatorObject.GetBool(BoolName));
    }
}
