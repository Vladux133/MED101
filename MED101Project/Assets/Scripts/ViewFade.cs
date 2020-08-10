using UnityEngine;
using Valve.VR;

public class ViewFade : MonoBehaviour
{
   
    // Start is called before the first frame update
    public void FadeToBlack(float FadeDuration)
    {
        Debug.Log("FADING TO BLACK");
        SteamVR_Fade.View(Color.black, FadeDuration);
     }

    // Update is called once per frame
    public void FadeToClear(float FadeDuration)
    {
        SteamVR_Fade.View(Color.clear, FadeDuration);
    }
}
