using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEditor;

public class InteractableMenuController : MonoBehaviourPun
{
    public Slider selectedSlider;
    public float sliderSpeed = 50f;
    private InteractableObject currentInteractableWithMenu;
    public GameObject lightMenuCanvas;
    public TextMeshProUGUI rotateLabel;
    public TextMeshProUGUI scaleLabel;

    private void Start()
    {
        GameObject[] menus = GameObject.FindGameObjectsWithTag("ObjectMenu");


        foreach (GameObject menu in menus)
        {
            if (menu.name == "Light Grab Canvas")
            {
                lightMenuCanvas = menu;
            }
        }

        List<Slider> menuSliders = new List<Slider>();

        if (lightMenuCanvas != null)
        {
            menuSliders.AddRange(lightMenuCanvas.GetComponentsInChildren<Slider>());

            foreach (Slider sld in menuSliders)
            {
                if (sld.gameObject.name == "Rotate")
                {
                    rotateLabel = sld.GetComponentInChildren<TextMeshProUGUI>();
                }
                else if (sld.gameObject.name == "Scale")
                {
                    scaleLabel = sld.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
        }
    }

    public void getInteractable(InteractableObject obj)
    {
        currentInteractableWithMenu = obj;
    }

    public void getSlider(Slider slider)
    {
        selectedSlider = slider;
    }

    void Update()
    {
        if (photonView.IsMine && currentInteractableWithMenu != null && selectedSlider != null)
        {
            float horizontal = Input.GetAxis("Horizontal");
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                float adjustmentSpeed = selectedSlider.gameObject.name == "Scale" ? sliderSpeed * 0.5f : sliderSpeed;
                float newValue = selectedSlider.value + horizontal * Time.deltaTime * adjustmentSpeed;
                selectedSlider.value = newValue; // Update the slider locally

                // Call RPC to update the interactable object on all clients
                if (selectedSlider.gameObject.name == "Rotate")
                {
                    photonView.RPC("RpcRotateObject", RpcTarget.All, currentInteractableWithMenu.GetComponent<PhotonView>().ViewID, newValue);
                    if (rotateLabel != null)
                    {
                        rotateLabel.text = $"Rotate: {Mathf.RoundToInt(newValue)}°";
                    }
                }
                else if (selectedSlider.gameObject.name == "Scale")
                {
                    photonView.RPC("RpcScaleObject", RpcTarget.All, currentInteractableWithMenu.GetComponent<PhotonView>().ViewID, newValue);
                    if (scaleLabel != null)
                    {
                        scaleLabel.text = $"Scale: {newValue:F1}x";
                    }
                }
            }
        }
        else if (currentInteractableWithMenu != null && selectedSlider != null)
        {
            // For remote clients, the slider value will be updated by the RPC,
            // and the labels are updated here based on the local slider value.
            if (selectedSlider.gameObject.name == "Rotate" && rotateLabel != null)
            {
                rotateLabel.text = $"Rotate: {Mathf.RoundToInt(selectedSlider.value)}�";
            }
            else if (selectedSlider.gameObject.name == "Scale" && scaleLabel != null)
            {
                scaleLabel.text = $"Scale: {selectedSlider.value:F1}x";
            }
        }
    }

    [PunRPC]
    void RpcRotateObject(int viewID, float rotationY)
    {
        GameObject targetObject = PhotonView.Find(viewID).gameObject;
        if (targetObject != null)
        {
            targetObject.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            // Update the slider value on the remote client to reflect the change
            if (selectedSlider != null && selectedSlider.gameObject.name == "Rotate")
            {
                selectedSlider.value = rotationY;
            }
        }
        else
        {
            Debug.LogError($"Could not find GameObject with viewID: {viewID} for rotation.");
        }
    }

    [PunRPC]
    void RpcScaleObject(int viewID, float scaleFactor)
    {
        GameObject targetObject = PhotonView.Find(viewID).gameObject;
        if (targetObject != null)
        {
            targetObject.transform.localScale = Vector3.one * scaleFactor;
            // Update the slider value on the remote client
            if (selectedSlider != null && selectedSlider.gameObject.name == "Scale")
            {
                selectedSlider.value = scaleFactor;
            }
        }
        else
        {
            Debug.LogError($"Could not find GameObject with viewID: {viewID} for scaling.");
        }
    }
}