using System.Collections;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;

public class HitShaker : MonoBehaviour
{
    public ShakeData HitShake;
    
    private void start()
    {
        CameraShakerHandler.Shake(HitShake);
    }

    
}
