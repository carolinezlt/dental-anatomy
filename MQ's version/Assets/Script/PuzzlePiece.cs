using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Tooth ID( used for Slot pairing)")]
    public string toothID;

    [Header("SnapSpeed")]
    public float snapSpeed = 8f;

    [Header("Status")]
    public bool isPlaced = false;


    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    /// <summary>
    /// 当 Slot 判定成功时调用，自动吸附到槽位
    /// </summary>
    public void LockToSlot(Transform slotTransform)
    {
        if (isPlaced) return;
        isPlaced = true;

       
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
}
