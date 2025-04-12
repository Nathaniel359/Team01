using UnityEngine;

public class InteractionController : MonoBehaviour
{
    public Camera mainCamera;
    public float rayLength = 10f;

    private LineRenderer lineRenderer;

    private void Start()
    {
        lineRenderer = transform.GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            ToggleOnOff();
        }
    }

    void ToggleOnOff()
    {
        Vector3 rayOrigin = lineRenderer.GetPosition(0);//mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, mainCamera.transform.forward, out hit, rayLength))
        {
            if (hit.collider.CompareTag("InteractOnly"))
            {
                // Try to toggle a light
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

                var door = hit.collider.GetComponent<DoorToggle>();
                if (door != null)
                {
                    door.ToggleDoor();
                    return;
                }

                var sit = hit.collider.GetComponent<SitTarget>();
                if (sit != null)
                {
                    sit.ToggleSit();
                    return;
                }
            }
        }
    }

}

