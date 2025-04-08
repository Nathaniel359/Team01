using UnityEngine;
using UnityEngine.Video;

public class TVToggle : MonoBehaviour
{
    public Renderer screenRenderer;
    public Material tvOnMaterial;
    public Material tvOffMaterial;
    public VideoPlayer videoPlayer;

    private bool isOn = false;

    public void ToggleTV()
    {
        isOn = !isOn;
        UpdateTV();
    }

    void UpdateTV()
    {
        
        if (screenRenderer != null)
        {
            screenRenderer.material = isOn ? tvOnMaterial : tvOffMaterial;
        }

        // Video playback
        if (videoPlayer != null)
        {
            if (isOn) videoPlayer.Play();
            else videoPlayer.Pause();
        }
    }
}
