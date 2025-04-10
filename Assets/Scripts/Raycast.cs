using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CharacterRaycast : MonoBehaviour
{
    public LayerMask floor;
    public Transform cameraTransform;
    public Canvas settings;
    public GameObject settingsDefaultButton;
    // public Sprite[] images;
    // public Canvas inventoryDisplay;
    // public GameObject inventoryDefaultButton;
    // public GameObject inventoryFullMessage;

    private LineRenderer lineRenderer;
    private GameObject lastHitObject = null;
    private GameObject lastHoveredUI;
    private Canvas currentMenu;
    private GameObject currentObj;
    private bool isGrabbed;
    private Rigidbody rb;
    private int index;
    private GameObject[] inventory;
    private float[] raycastLength;
    private float rayLength = 10f;
    private int rayLengthIndex = 0;
    private float[] speedArr;
    private string[] speedTypesArr;
    private float speed;
    private int speedIndex = 0;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lineRenderer = GetComponent<LineRenderer>();
        isGrabbed = false;
        inventory = new GameObject[] {null, null, null};
        index = 0;
        raycastLength = new float[] {10, 25, 50};
        speedArr = new float[] {20, 10, 5};
        speedTypesArr = new string[] {"High", "Medium", "Low"};
    }

    void Update()
    {
        // Disable raycast and current highlights if settings or inventory are open
        //if (settings.isActiveAndEnabled || inventoryDisplay.isActiveAndEnabled) 
        if (settings.isActiveAndEnabled)
        {
            lineRenderer.enabled = false;
            if (lastHitObject != null)
            {
                if (lastHitObject.GetComponent<Outline>() != null)
                {
                    lastHitObject.GetComponent<Outline>().enabled = false;
                }
            }
            lastHitObject = null;
        }
        else
        {
            Vector3 origin = transform.position + transform.TransformDirection(new Vector3(0f, 1, 0f));
            Vector3 direction = cameraTransform.forward;
            Vector3 endPosition = origin + direction * rayLength;

            GameObject currentHitObject = null;

            // If raycast is on an object
            if (Physics.Raycast(origin, direction, out RaycastHit hit, rayLength)) 
            {
                endPosition = hit.point;
                currentHitObject = hit.collider.gameObject;

                if ((Input.GetButtonDown("js3") || Input.GetKeyDown(KeyCode.P)) && GetComponent<CharacterMovement>().enabled == true) 
                {
                    //GetComponent<CharacterMovement>().enabled = false;
                    CharacterController controller = GetComponent<CharacterController>();
                    controller.enabled = false;
                    transform.position = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                    controller.enabled = true;
                    //GetComponent<CharacterMovement>().enabled = true;
                }

                // Open menu on button press
                if (currentHitObject.GetComponentInChildren<Canvas>() != null && (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("js2")) && !isGrabbed) 
                {
                    // Disable movement
                    GetComponent<CharacterMovement>().enabled = false;
                            
                    currentHitObject.GetComponentInChildren<Canvas>().enabled = true;
                    // Close previous menus
                    if (currentMenu != null && currentMenu != currentHitObject.GetComponentInChildren<Canvas>())
                    {
                        currentMenu.enabled = false;
                    }
                    // Update reference
                    currentMenu = currentHitObject.GetComponentInChildren<Canvas>();
                    currentObj = currentHitObject;


                    Vector3 targetPos = cameraTransform.position;          
                    targetPos.y = currentMenu.transform.position.y;            
                    currentMenu.transform.LookAt(targetPos);                   
                    currentMenu.transform.Rotate(0, 180, 0); 
                }

                if (currentHitObject != lastHitObject)
                {
                    // Disable the outline on the last hit object
                    if (lastHitObject != null)
                    {
                        if (lastHitObject.GetComponent<Outline>() != null)
                        {
                            lastHitObject.GetComponent<Outline>().enabled = false;
                        }
                    }

                    // Enable the outline on the newly hit object
                    if (currentHitObject.GetComponent<Outline>() != null)
                    {
                        currentHitObject.GetComponent<Outline>().enabled = true;
                    }

                    // Update the reference
                    lastHitObject = currentHitObject;
                }
            }

            // Else remove outline from previous object
            else 
            {
                if (lastHitObject != null) {
                    if (lastHitObject.GetComponent<Outline>() != null)
                        lastHitObject.GetComponent<Outline>().enabled = false;
                    lastHitObject = null;
                }
            }

            // Menu interaction
            PointerEventData pointerData = new PointerEventData(EventSystem.current);       
            pointerData.position = Camera.main.WorldToScreenPoint(endPosition);
            List<RaycastResult> uiHits = new List<RaycastResult>();                         
            EventSystem.current.RaycastAll(pointerData, uiHits);                            

            bool hoveredUI = false;

            // Hover/select
            foreach (RaycastResult result in uiHits)    
            {
                GameObject target = result.gameObject;                                      

                endPosition = result.worldPosition;

                if (target.GetComponent<Selectable>() != null)
                {
                    // Highlight (hover)
                    if (lastHoveredUI != target)
                    {
                        if (lastHoveredUI != null)
                            ExecuteEvents.Execute(lastHoveredUI, pointerData, ExecuteEvents.pointerExitHandler);

                        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);
                        lastHoveredUI = target;
                    }

                    // Select
                    if (Input.GetKeyDown(KeyCode.K) || Input.GetButtonDown("js5"))
                    {
                        Button btn = target.GetComponent<Button>();
                        if (btn != null)
                            btn.onClick.Invoke();
                    }

                    // Only first hit
                    break; 
                }
            }
            if (!hoveredUI && lastHoveredUI != null)
            {
                ExecuteEvents.Execute(lastHoveredUI, pointerData, ExecuteEvents.pointerExitHandler);
                lastHoveredUI = null;
            }

            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, endPosition);
            }
        } 

        // Release item
        if (isGrabbed && (Input.GetButtonDown("js10") || Input.GetKeyDown(KeyCode.L))) 
        {
            currentObj.transform.SetParent(null);
            rb.useGravity = true;
            rb.isKinematic = false;
            isGrabbed = false;
        }

        // Open settings
        if (Input.GetKeyDown(KeyCode.I) || Input.GetButtonDown("js0"))
        {
            if (currentMenu != null)
                currentMenu.enabled = false;
            settings.gameObject.SetActive(true);
            // Resume is selected by default
            EventSystem.current.SetSelectedGameObject(null);            
            EventSystem.current.SetSelectedGameObject(settingsDefaultButton);     

            GetComponent<CharacterMovement>().enabled = false;
        }
    }

    public void Grab()
    {
        rb = currentObj.GetComponent<Rigidbody>();
        currentObj.transform.SetParent(cameraTransform);

        Vector3 origin = transform.position + transform.TransformDirection(new Vector3(0f, 1, 0f));
        Vector3 direction = cameraTransform.forward;
        currentObj.transform.position = origin + direction * 4f;

        //currentObj.transform.position += Vector3.up * 1.5f;
        rb.useGravity = false;
        rb.isKinematic = true;
        isGrabbed = true;

        if (currentMenu != null)
            currentMenu.enabled = false;

        GetComponent<CharacterMovement>().enabled = true;
    }

    // IEnumerator ShowMessage()
    // {
    //     inventoryFullMessage.SetActive(true);
    //     yield return new WaitForSeconds(2);
    //     inventoryFullMessage.SetActive(false);
    // }

    // public void Store()
    // {
    //     if (index == 2 && inventory[index] != null)
    //     {
    //         if (currentMenu != null)
    //             currentMenu.enabled = false;
    //         GetComponent<CharacterMovement>().enabled = true;
    //         StartCoroutine(ShowMessage());
    //         return;
    //     }

    //     // Store object in inventory
    //     inventory[index] = currentObj;

    //     // Assign appropriate image to the inventory menu
    //     if (currentObj.name.StartsWith("Sphere"))
    //         inventoryDisplay.transform.Find(index.ToString()).Find("Text (TMP)").GetComponent<Image>().sprite = images[0];
    //     else
    //         inventoryDisplay.transform.Find(index.ToString()).Find("Text (TMP)").GetComponent<Image>().sprite = images[1];

    //     // Increment index
    //     while (index < 2 && inventory[index] != null) 
    //         index += 1;

    //     // Remove object from world
    //     currentObj.SetActive(false);

    //     GetComponent<CharacterMovement>().enabled = true;
    // }

    public void Exit() 
    {
        if (currentMenu != null)
            currentMenu.enabled = false;
        GetComponent<CharacterMovement>().enabled = true;
    }

    public void Resume()
    {
        settings.gameObject.SetActive(false);
        lineRenderer.enabled = true;
        GetComponent<CharacterMovement>().enabled = true;
    }

    public void RaycastLength()
    {
        rayLengthIndex = (rayLengthIndex + 1) % 3;
        rayLength = raycastLength[rayLengthIndex];
        settings.transform.Find("Raycast Length").GetComponentInChildren<TextMeshProUGUI>().text = "Raycast Length: " + rayLength + "m";
    }

    // public void Inventory()
    // {
    //     settings.gameObject.SetActive(false);
    //     inventoryDisplay.gameObject.SetActive(true);
    //     EventSystem.current.SetSelectedGameObject(null);            
    //     EventSystem.current.SetSelectedGameObject(inventoryDefaultButton);
    // }

    // public void objZero()
    // {
    //     if (inventory[0] != null) 
    //     {
    //         index = 0;
    //         currentObj = inventory[index];
    //         inventory[index] = null;
    //         currentObj.SetActive(true);
    //         inventoryDisplay.transform.Find(index.ToString()).Find("Text (TMP)").GetComponent<Image>().sprite = null;
    //         Grab();
    //     }
    //     inventoryDisplay.gameObject.SetActive(false);
    //     GetComponent<CharacterMovement>().enabled = true;
    //     lineRenderer.enabled = true;
    // }

    // public void objOne()
    // {
    //     if (inventory[1] != null) 
    //     {
    //         index = 1;
    //         currentObj = inventory[index];
    //         inventory[index] = null;
    //         currentObj.SetActive(true);
    //         inventoryDisplay.transform.Find(index.ToString()).Find("Text (TMP)").GetComponent<Image>().sprite = null;
    //         Grab();
    //     }
    //     inventoryDisplay.gameObject.SetActive(false);
    //     GetComponent<CharacterMovement>().enabled = true;
    //     lineRenderer.enabled = true;
    //     if (inventory[0] == null)
    //         index = 0;
    // }

    // public void objTwo()
    // {
    //     if (inventory[2] != null) 
    //     {
    //         index = 2;
    //         currentObj = inventory[index];
    //         inventory[index] = null;
    //         currentObj.SetActive(true);
    //         inventoryDisplay.transform.Find(index.ToString()).Find("Text (TMP)").GetComponent<Image>().sprite = null;
    //         Grab();
    //     }
    //     inventoryDisplay.gameObject.SetActive(false);
    //     GetComponent<CharacterMovement>().enabled = true;
    //     lineRenderer.enabled = true;
    //     if (inventory[1] == null)
    //         index = 1;
    //     if (inventory[0] == null)
    //         index = 0;
    // }
    
    public void Speed()
    {
        speedIndex = (speedIndex + 1) % 3;
        speed = speedArr[speedIndex];
        transform.GetComponent<CharacterMovement>().speed = speed;
        settings.transform.Find("Speed").GetComponentInChildren<TextMeshProUGUI>().text = "Speed: " + speedTypesArr[speedIndex];
    }

    public void Quit()
    {
        Application.Quit();
    }
}
