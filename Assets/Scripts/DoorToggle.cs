using UnityEngine;
using System.Collections;

public class DoorToggle : MonoBehaviour
{
    public Transform doorHinge; // Assign the pivot point of the door
    public float openAngle = 90f; // How far it opens
    public float speed = 2f; // Opening/closing speed
    public bool isOpen = false;

    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Coroutine currentAnim;

    void Start()
    {
        if (doorHinge == null)
            doorHinge = transform;

        closedRotation = doorHinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    public void ToggleDoor()
    {
        if (currentAnim != null)
            StopCoroutine(currentAnim);

        currentAnim = StartCoroutine(RotateDoor(isOpen ? openRotation : closedRotation, isOpen ? closedRotation : openRotation));
        isOpen = !isOpen;

        if (audioSource != null)
        {
            AudioClip clipToPlay = isOpen ? openSound : closeSound;
            if (clipToPlay != null)
            {
                audioSource.pitch = Random.Range(0.8f, 1.2f);
                audioSource.PlayOneShot(clipToPlay);
            }
                
        }
    }

    private IEnumerator RotateDoor(Quaternion from, Quaternion to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            doorHinge.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
        doorHinge.localRotation = to;
    }
}
