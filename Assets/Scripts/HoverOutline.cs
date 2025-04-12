using System.Collections;
using UnityEngine;

// Handles highlighting of objects in the scene
public class HoverOutline : MonoBehaviour
{
    public Camera playerCamera;
    public float rayLength = 20f;
    public Color lightOutlineColor = Color.white;
    public Color heavyOutlineColor = Color.green;
    public Color interactOutlineColor = Color.yellow;

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
                        outline.OutlineColor = heavyOutlineColor;
                    }

                    outline.OutlineWidth = 5f;
                }

                outline.enabled = true;
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
                        outline.OutlineColor = heavyOutlineColor;
                    }

                    outline.OutlineWidth = 5f;
                    outline.enabled = false; // Keep disabled until hovered
                }

                // If this is a new object being hovered over, update highlighting
                if (lastHighlighted != outline)
                {
                    ResetLastHighlight();
                    outline.enabled = true;
                    lastHighlighted = outline;
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

    private void ResetLastHighlight()
    {
        if (lastHighlighted != null)
        {
            lastHighlighted.enabled = false;
            lastHighlighted = null;
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
}
