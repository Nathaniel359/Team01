using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    CharacterController charCntrl;
    [Tooltip("The speed at which the character will move.")]
    public float speed = 5f;
    [Tooltip("The camera representing where the character is looking.")]
    public GameObject cameraObj;
    [Tooltip("Should be checked if using the Bluetooth Controller to move. If using keyboard, leave this unchecked.")]
    public bool joyStickMode;

    [Header("Footstep Settings")]
    public AudioSource footstepSource;
    public AudioClip footstepClip;

    private float stepTimer = 0f;

    void Start()
    {
        charCntrl = GetComponent<CharacterController>();
    }

    void Update()
    {
        float horComp = Input.GetAxis("Horizontal");
        float vertComp = Input.GetAxis("Vertical");

        if (joyStickMode)
        {
            horComp = Input.GetAxis("Vertical");
            vertComp = Input.GetAxis("Horizontal") * -1;
        }

        Vector3 moveVect = Vector3.zero;

        Vector3 cameraLook = cameraObj.transform.forward;
        cameraLook.y = 0f;
        cameraLook = cameraLook.normalized;

        Vector3 forwardVect = cameraLook;
        Vector3 rightVect = Vector3.Cross(forwardVect, Vector3.up).normalized * -1;

        moveVect += rightVect * horComp;
        moveVect += forwardVect * vertComp;

        moveVect *= speed;

        charCntrl.SimpleMove(moveVect);

        // Footstep Sound Logic
        if (charCntrl.velocity.magnitude > 0.1f && charCntrl.isGrounded)
        {
            // Adjust step interval based on movement speed
            float adjustedInterval = Mathf.Clamp(3f / speed, 0.2f, 1f); // tweak this formula as needed

            stepTimer += Time.deltaTime;
            if (stepTimer >= adjustedInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }

    }

    void PlayFootstep()
    {
        if (footstepSource != null && footstepClip != null)
        {
            footstepSource.pitch = Random.Range(0.5f, 1.5f); // slight variation
            footstepSource.PlayOneShot(footstepClip);
        }
    }

}
