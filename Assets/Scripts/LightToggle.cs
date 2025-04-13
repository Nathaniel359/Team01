using UnityEngine;
using System.Collections;

// Handles light toggling and fading
public class LightToggle : MonoBehaviour
{
    public Light[] lights;
    private bool isOn = false;
    public float fadeDuration = 1f; // seconds
    public float targetIntensity = 3f; // default light brightness
    public AudioSource audioSource;
    public AudioClip turnOnClip;
    public AudioClip turnOffClip;


    public void ToggleLight()
    {
        isOn = !isOn;

        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(isOn ? turnOnClip : turnOffClip);
        }

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
        Debug.Log($"Starting FadeLight: Target Intensity = {target}, Current Intensity = {light.intensity}");

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
            Debug.Log($"Fading: t = {t}, Intensity = {light.intensity}");
            yield return null;
        }

        light.intensity = target;
        Debug.Log($"Fade complete: Final Intensity = {light.intensity}");

        // Disable light if fully faded out
        if (Mathf.Approximately(target, 0f))
            light.enabled = false;
    }
}
