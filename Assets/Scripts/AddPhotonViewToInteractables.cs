#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Photon.Pun;

public class AddPhotonViewToInteractables : MonoBehaviour
{
    [MenuItem("Tools/Photon/Add Photon View to Interactables")]
    public static void AddViewToInteractableObjects()
    {
        // Find all GameObjects in the scene that have the InteractableObject script (replace with your script name)
        InteractableObject[] interactableObjects = FindObjectsOfType<InteractableObject>();
        int count = 0;

        foreach (InteractableObject interactable in interactableObjects)
        {
            GameObject obj = interactable.gameObject;

            // Check if the GameObject already has a PhotonView
            if (obj.GetComponent<PhotonView>() == null)
            {
                // Add a PhotonView component
                obj.AddComponent<PhotonView>();
                Debug.Log($"Added PhotonView to: {obj.name}");
                count++;
            }
            else
            {
                Debug.Log($"{obj.name} already has a PhotonView.");
            }
        }

        Debug.Log($"Finished. Added PhotonView to {count} Interactable objects.");
    }
}
#endif