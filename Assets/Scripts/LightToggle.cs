using UnityEngine;

public class LightToggle : MonoBehaviour
{
    public Light light;

    private bool isOn = false;

    public void ToggleLight()
    {
        isOn = !isOn;
        if (light != null)
            light.enabled = isOn;
    }
}
