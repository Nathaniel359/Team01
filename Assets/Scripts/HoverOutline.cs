using System.Collections;
using UnityEngine;
using TMPro;

// Handles highlighting of objects in the scene
public class HoverOutline : MonoBehaviour
{
    public Camera playerCamera;
    public float rayLength = 20f;
    public Color lightOutlineColor = Color.white;
    public Color heavyOutlineColor = Color.black;
    public Color interactOutlineColor = Color.yellow;

    public GameObject tooltipPrefab; // assign in Inspector
    private GameObject currentTooltip;
    public static bool tooltipEnabled = true;

    private Outline lastHighlighted; // Store the last highlighted object

    public static bool raycastingEnabled = true;

    private LineRenderer lineRenderer;

    private void Start()
    {
        lineRenderer = transform.GetComponent<LineRenderer>();
        StartCoroutine(HighlightAllObjectsWithTags());
    }

    // Briefly highlight all objects (helps fix outline bugs)
    private IEnumerator HighlightAllObjectsWithTags()
    {
        string[] tags = { "Grab", "HeavyGrab", "InteractOnly" };

        foreach (string tag in tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                Outline outline = obj.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = obj.AddComponent<Outline>();
                    outline.OutlineMode = Outline.Mode.OutlineVisible;

                    if (tag == "InteractOnly")
                    {
                        outline.OutlineColor = interactOutlineColor;
                    }
                    else if (tag == "Grab")
                    {
                        outline.OutlineColor = lightOutlineColor;
                    }
                    else
                    {
                        outline.OutlineColor = Color.black;
                    }

                    outline.OutlineWidth = 10f;
                }

                outline.enabled = true;
                var rend = obj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = new Material(rend.material); // breaks material sharing
                }
            }
        }

        yield return new WaitForSeconds(1f);

        foreach (string tag in tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                Outline outline = obj.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = false;
                }
            }
        }
    }

    void Update()
    {
        Vector3 rayOrigin = lineRenderer.GetPosition(0);
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, playerCamera.transform.forward, out hit, rayLength))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Grab") || hitObject.CompareTag("HeavyGrab") || hitObject.CompareTag("InteractOnly"))
            {
                // Try to get an existing Outline component or add one if it doesn't exist
                Outline outline = hitObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = hitObject.AddComponent<Outline>();
                    outline.OutlineMode = Outline.Mode.OutlineVisible;
                    //check if interactable:
                    var light = hit.collider.GetComponent<LightToggle>();
                    var tv = hit.collider.GetComponent<TVToggle>();
                    var sit = hit.collider.GetComponent<SitTarget>();
                    var door = hit.collider.GetComponent<DoorToggle>();

                    if (hitObject.CompareTag("InteractOnly") || light != null || tv != null || sit != null || door != null)
                    {
                        outline.OutlineColor = interactOutlineColor;
                    }
                    else if (hitObject.CompareTag("Grab"))
                    {
                        outline.OutlineColor = lightOutlineColor;
                    }
                    else
                    {
                        outline.OutlineColor = Color.black;
                    }

                    outline.OutlineWidth = 10f;
                    outline.enabled = false; // Keep disabled until hovered
                }

                // If this is a new object being hovered over, update highlighting
                if (lastHighlighted != outline)
                {
                    ResetLastHighlight();
                    outline.enabled = true;
                    lastHighlighted = outline;
                }

                if (currentTooltip == null)
                {
                    currentTooltip = Instantiate(tooltipPrefab);
                }

                if (tooltipEnabled && (hitObject.CompareTag("Grab") || hitObject.CompareTag("HeavyGrab") || hitObject.CompareTag("InteractOnly")))
                {
                    Vector3 directionToCamera = (playerCamera.transform.position - hit.point).normalized;
                    currentTooltip.transform.position = hit.point + directionToCamera * 1.5f + Vector3.up * 0.8f;
                    currentTooltip.transform.LookAt(playerCamera.transform); // face the camera
                    currentTooltip.transform.Rotate(0, 180f, 0); // flip to face correctly

                    TextMeshProUGUI tooltipText = currentTooltip.GetComponentInChildren<TextMeshProUGUI>();
                    if (tooltipText != null)
                    {
                        tooltipText.text = GetTooltipText(hitObject);
                    }
                }
                else
                {
                    ResetLastHighlight();
                }

            }
            else
            {
                ResetLastHighlight();
            }
        }
        else
        {
            ResetLastHighlight();
        }

        if (VRGrab.isGrabbing && tooltipEnabled)
        {
            GameObject grabbed = VRGrab.instance.grabbedObject;
            if (grabbed != null)
            {
                if (currentTooltip == null)
                {
                    currentTooltip = Instantiate(tooltipPrefab);
                }

                // Position tooltip above the object
                Vector3 camDirection = (playerCamera.transform.position - grabbed.transform.position).normalized;
                currentTooltip.transform.position = grabbed.transform.position + camDirection * 1.5f + Vector3.up * 0.8f;
                currentTooltip.transform.LookAt(playerCamera.transform);
                currentTooltip.transform.Rotate(0, 180f, 0);

                TextMeshProUGUI tooltipText = currentTooltip.GetComponentInChildren<TextMeshProUGUI>();
                if (tooltipText != null)
                {
                    tooltipText.text = "Press Y to release";
                }
            }
        }

    }

    private void ResetLastHighlight()
    {
        if (lastHighlighted != null)
        {
            lastHighlighted.enabled = false;
            lastHighlighted = null;
        }

        // Only destroy tooltip if NOT grabbing
        if (currentTooltip != null && !VRGrab.isGrabbing)
        {
            Destroy(currentTooltip);
            currentTooltip = null;
        }
    }


    public static void DisableRaycasting()
    {
        raycastingEnabled = false;
    }

    public static void EnableRaycasting()
    {
        raycastingEnabled = true;
    }

    private string GetTooltipText(GameObject obj)
    {;

        if (VRGrab.isGrabbing)
        {
            return "Press Y to release";
        }

        if (obj.CompareTag("Grab"))
            return "Press X to grab";
        if (obj.CompareTag("InteractOnly"))
            return "Press X to interact";

        // Or check components
        if (obj.GetComponent<LightToggle>() != null)
            return "Press X to toggle light";
        if (obj.GetComponent<TVToggle>() != null)
            return "Press X to toggle TV";
        if (obj.GetComponent<SitTarget>() != null)
            return "Press X to sit";
        if (obj.GetComponent<DoorToggle>() != null)
            return "Press X to open/close door";

        return "";
    }

}
