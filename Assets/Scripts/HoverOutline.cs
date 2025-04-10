using UnityEngine;

public class HoverOutline : MonoBehaviour
{
    public Camera playerCamera;
    public float rayLength = 20f;
    public Color lightOutlineColor = Color.white;
    public Color heavyOutlineColor = Color.green;
    public Color interactOutlineColor = Color.yellow;

    private Outline lastHighlighted; // Store the last highlighted object

    public static bool raycastingEnabled = true;


    private void Start()
    {

    }

    void Update()
    {

        Vector3 rayOrigin = playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, playerCamera.transform.forward, out hit, rayLength))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Grab") || hitObject.CompareTag("HeavyGrab"))
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

                    if(light != null || tv != null)
                    {
                        outline.OutlineColor = interactOutlineColor;
                    } else if (hitObject.CompareTag("Grab"))
                    {
                        outline.OutlineColor = lightOutlineColor;
                    } else
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
