using UnityEngine;

public class DeParentObj : MonoBehaviour
{
    public GameObject PlayerGO;
    public Transform TaxiObj;
    //private Vector3 OrigPos;
    public GameObject NewParent;
    
    // Start is called before the first frame update
    public void DeParent()
    {
        //OrigPos = PlayerGO.transform.localPosition;

        Debug.Log("SETTING TO ZERO");
        PlayerGO.transform.localPosition = Vector3.zero;
        Debug.Log(PlayerGO.transform.localPosition);
        PlayerGO.transform.parent = NewParent.transform;

        //PlayerGO.transform.SetParent(NewParent.transform, true);
        //PlayerGO.transform.position = TaxiObj.position;

        //PlayerGO.transform.localPosition = TaxiObj.transform.localPosition + PlayerGO.transform.localPosition;
        //Debug.Log("TaxiObj Local: " + TaxiObj.transform.localPosition);
        //Debug.Log("PlayerObj Local: " + PlayerGO.transform.localPosition);
        //Debug.Log("TaxiObj Global: " + TaxiObj.transform.position);
        //Debug.Log("PlayerObj Global: " + PlayerGO.transform.position);
    }
}
