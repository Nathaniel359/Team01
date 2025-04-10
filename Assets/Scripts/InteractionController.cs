using UnityEngine;

public class InteractionController : MonoBehaviour
{
    public Camera mainCamera;
    public float rayLength = 10f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleOnOff();
        }
    }

    void ToggleOnOff()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // cast a ray from center of the view
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayLength))
        {
            // Try to toggle a lamp
            var light = hit.collider.GetComponent<LightToggle>();
            if (light != null)
            {
                light.ToggleLight();
                return;
            }

            // Try to toggle a TV
            var tv = hit.collider.GetComponent<TVToggle>();
            if (tv != null)
            {
                tv.ToggleTV();
                return;
            }
        }
    }

}

