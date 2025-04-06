using UnityEngine;

public class PlayerTeleportation : MonoBehaviour
{
    public Camera playerCamera;
    public Transform player; // Assign the Player (Character) GameObject here
    public float rayLength = float.PositiveInfinity;
    public LayerMask floorLayer; // Assign Floor layer in the Inspector

    public static bool canTP = true;

    private CharacterController charCntrl;

    void Start()
    {
        charCntrl = player.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (!canTP) return;

        if (Input.GetKeyDown(KeyCode.Y) || Input.GetButtonDown("js3"))
        {
            TeleportToPointer();
        }
    }

    void TeleportToPointer()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        // use LayerMask to ensure hitting only the floor
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayer))
        {
            // Disable movement during teleportation
            if (charCntrl != null)
            {
                charCntrl.enabled = false; // Disable the CharacterController during teleport
            }

            player.position = hit.point + new Vector3(0, 0.5f, 0);

            // Re-enable the CharacterController after teleportation
            if (charCntrl != null)
            {
                charCntrl.enabled = true; // Re-enable CharacterController
            }
        }
    }

    public static void LockTP()
    {
        canTP = false;
    }

    public static void UnlockTP()
    {
        canTP = true;
    }
}
