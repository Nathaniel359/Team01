using UnityEngine;
using System.Collections;

public class LightToggle : MonoBehaviour
{
    public Light[] lights;
    private bool isOn = false;
    public float fadeDuration = 1f; // seconds
    public float targetIntensity = 2f; // default light brightness

    public void ToggleLight()
    {
        isOn = !isOn;

        foreach (var light in lights)
        {
            if (light != null)
            {
                StartCoroutine(FadeLight(light, isOn ? targetIntensity : 0f));
            }
        }
    }

    private IEnumerator FadeLight(Light light, float target)
    {
        // If fading in from disabled, set intensity to 0 and enable
        if (target > 0 && !light.enabled)
        {
            light.intensity = 0f;
            light.enabled = true;
        }

        float start = light.intensity;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            light.intensity = Mathf.Lerp(start, target, t);
            yield return null;
        }

        light.intensity = target;

        // Disable light if fully faded out
        if (Mathf.Approximately(target, 0f))
            light.enabled = false;
    }
}
