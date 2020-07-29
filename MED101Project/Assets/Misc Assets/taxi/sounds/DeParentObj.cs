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
        if (NewParent == null)
        {
            PlayerGO.transform.SetParent(null, true);
            //PlayerGO.transform.position = TaxiObj.transform.position;
        } else
        {
            PlayerGO.transform.SetParent(NewParent.transform, true);
            //PlayerGO.transform.position = TaxiObj.transform.position;
        }
        //PlayerGO.transform.localPosition = TaxiObj.transform.localPosition + PlayerGO.transform.localPosition;
        //Debug.Log("TaxiObj Local: " + TaxiObj.transform.localPosition);
        //Debug.Log("PlayerObj Local: " + PlayerGO.transform.localPosition);
        //Debug.Log("TaxiObj Global: " + TaxiObj.transform.position);
        //Debug.Log("PlayerObj Global: " + PlayerGO.transform.position);
    }
}
