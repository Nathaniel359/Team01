using UnityEngine;

// Handles sitting
public class SitTarget : MonoBehaviour
{
    public GameObject player;
    public CharacterController controller;
    public Transform sitPoint;

    public AudioSource audioSource;
    public AudioClip audioClip;

    private CharacterMovement movementScript;

    private bool isSitting = false;
    private float originalHeight;
    private Vector3 originalCenter;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Character");

        if(player == null )
        {
            controller = player.GetComponent<CharacterController>();
            movementScript = player.GetComponent<CharacterMovement>();
            originalHeight = controller.height;
            originalCenter = controller.center;
        }
        
    }

    public void ToggleSit()
    {
        if (!isSitting)
            Sit();
        else
            Stand();
    }

    void Sit()
    {
        isSitting = true;

        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(audioClip);
        }

        // Snap to sit position
        player.transform.position = sitPoint.position;

        // Shrink height significantly for low sitting
        controller.height = 0.6f;
        controller.center = new Vector3(originalCenter.x, 0.3f, originalCenter.z); // half of height
    }


    void Stand()
    {
        isSitting = false;

        controller.height = originalHeight;
        controller.center = originalCenter;
    }

    void Update()
    {
        if (isSitting && movementScript != null)
        {
            float inputX = Input.GetAxis("Horizontal");
            float inputY = Input.GetAxis("Vertical");

            if (Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f)
            {
                Stand();
            }
        }
    }
}
