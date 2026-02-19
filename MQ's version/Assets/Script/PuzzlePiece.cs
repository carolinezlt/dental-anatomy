using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Tooth ID( used for Slot pairing)")]
    public string toothID;

    [Header("SnapSpeed")]
    public float snapSpeed = 8f;

    [Header("Return Speed (when wrong placement)")]
    public float returnSpeed = 5f;

    [Header("Status")]
    public bool isPlaced = false;
    public bool isInsideAnySlot = false;
    public PuzzleSlot currentSlot = null;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    [Header("Label")]
    public GameObject labelObject;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip failClip;

    // --- For Return ---
    private Vector3 originalPos;
    private Quaternion originalRot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        originalPos = transform.position;
        originalRot = transform.rotation;
        // Register grab start & end events
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }
    private void OnGrab(SelectEnterEventArgs args)
    {
        // If already placed correctly, do nothing
        if (isPlaced) return;

        

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (!isPlaced)
        {
            if (isPlaced) return;

            // ---- Case 1
            if (!isInsideAnySlot)
            {
                rb.isKinematic = false;
                rb.useGravity = false;
                return;
            }

            // ---- Case 2
            if (currentSlot != null && currentSlot.acceptID != toothID)
            {
                PlayFailSound();
                StartCoroutine(ReturnToOriginal());
                return;
            }

            // ---- Case 3
            PlayFailSound();
            StartCoroutine(ReturnToOriginal());
        }
    }

    private IEnumerator ReturnToOriginal()
    {
        rb.isKinematic = true; // Turn off physics during snapping

        float t = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            transform.position = Vector3.Lerp(startPos, originalPos, t);
            transform.rotation = Quaternion.Lerp(startRot, originalRot, t);
            yield return null;
        }

        transform.position = originalPos;
        transform.rotation = originalRot;

        rb.isKinematic = false;
        rb.useGravity = false;
    }


    public void LockToSlot(Transform slotTransform)
    {
        if (isPlaced) return;
        isPlaced = true;

        PlaySuccessSound();
        SendGrabberHaptic();
        labelObject.SetActive(false);
       
        if (grabInteractable != null)
            grabInteractable.enabled = false;

       
        rb.isKinematic = true;
        rb.useGravity = false;

        
        StartCoroutine(SnapToSlot(slotTransform));
    }


    private IEnumerator SnapToSlot(Transform target)
    {
        float t = 0f;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * snapSpeed;

            transform.position = Vector3.Lerp(startPos, target.position, t);
            transform.rotation = Quaternion.Lerp(startRot, target.rotation, t);

            yield return null;
        }

   
        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    private void SendGrabberHaptic()
    {
        if (grabInteractable == null) return;

        // current hand
        var interactor = grabInteractable.firstInteractorSelecting;
        if (interactor is XRBaseControllerInteractor controllerInteractor)
        {
            controllerInteractor.xrController.SendHapticImpulse(0.7f, 0.2f);
        }
    }
    private void PlaySuccessSound()
    {
        if (audioSource != null && successClip != null)
            audioSource.PlayOneShot(successClip);
    }

    private void PlayFailSound()
    {
        if (audioSource != null && failClip != null)
            audioSource.PlayOneShot(failClip);
    }


    //Added by Caroline to set original position to current position (for current difficulty level)
    public void ResetHomeToCurrent()
    {
        originalPos = transform.position;
        originalRot = transform.rotation;
    }

    //Added by Caroline to toggle label visibility
    public void SetLabelVisible(bool visible)
    {
        if (labelObject != null)
            labelObject.SetActive(visible);
    }
}
