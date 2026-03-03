using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Tooth ID (used for Slot pairing)")]
    public string toothID;

    [Header("SnapSpeed")]
    public float snapSpeed = 8f;

    [Header("Return Speed (when wrong placement)")]
    public float returnSpeed = 5f;

    [Header("Status")]
    public bool isPlaced = false;
    public bool isInsideAnySlot = false;
    public PuzzleSlot currentSlot = null;

    [Header("Label")]
    public GameObject labelObject;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip failClip;

    [Header("Fail Highlight")]
    public float failHighlightDuration = 2f;
    public Color failHighlightColor = new Color(1f, 0.55f, 0.1f, 1f);

    [SerializeField] private Renderer[] highlightRenderers; // 不填会自动 GetComponentsInChildren<Renderer>()

    private Material[][] _originalMats;
    private Coroutine _highlightCo;

    public bool canPlaceInCurrentSlot = false;

    public static System.Action<PuzzlePiece> OnAnyPiecePlaced;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    // --- For Return ---
    private Vector3 originalPos;
    private Quaternion originalRot;

    // --- Candidate slots (解决相邻槽抢 currentSlot) ---
    private readonly HashSet<PuzzleSlot> candidateSlots = new HashSet<PuzzleSlot>();

    private readonly HashSet<PuzzleSlot> allTouchedSlots = new HashSet<PuzzleSlot>();
   

    // --- Return coroutine guard ---
    private Coroutine returnCo;
    private bool isReturning;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        originalPos = transform.position;
        originalRot = transform.rotation;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        CacheOriginalMaterials();
    }

    //缓存原材质
    private void CacheOriginalMaterials()
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>(true);

        _originalMats = new Material[highlightRenderers.Length][];
        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            if (highlightRenderers[i] == null) continue;
            _originalMats[i] = highlightRenderers[i].sharedMaterials;
        }
    }

    //恢复原材质
    private void RestoreOriginalMaterials()
    {
        if (_originalMats == null || highlightRenderers == null) return;

        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            var r = highlightRenderers[i];
            if (r == null) continue;

            // 恢复 sharedMaterials（回到原材质）
            if (i < _originalMats.Length && _originalMats[i] != null)
                r.sharedMaterials = _originalMats[i];
        }
    }
    //失败高亮触发器
    private void TriggerFailHighlight()
    {
        if (_highlightCo != null) StopCoroutine(_highlightCo);
        _highlightCo = StartCoroutine(FailHighlightRoutine(failHighlightDuration));
    }

    //高亮协程
    private IEnumerator FailHighlightRoutine(float seconds)
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            yield break;

        // 用实例材质，避免改到全局 sharedMaterial
        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            var r = highlightRenderers[i];
            if (r == null) continue;

            var mats = r.materials; // 生成实例
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;

                // 尽量兼容 Standard/URP Lit：有哪个属性就改哪个
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", failHighlightColor);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", failHighlightColor);

                // 尝试开一下 emission（如果材质支持）
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", failHighlightColor * 1.5f);
                }
            }
            r.materials = mats;
        }

        yield return new WaitForSeconds(seconds);

        RestoreOriginalMaterials();
    }



    // Slot 会调用这两个函数
    public void RegisterCandidateSlot(PuzzleSlot slot)
    {
        if (slot == null) return;
        if (isPlaced) return;
        allTouchedSlots.Add(slot);
        if (!slot.isFilled && slot.acceptID == toothID)
        {
            candidateSlots.Add(slot);
            UpdateBestSlot();
        }
    }

    public void UnregisterCandidateSlot(PuzzleSlot slot)
    {
        if (slot == null) return;
        allTouchedSlots.Remove(slot);
        candidateSlots.Remove(slot);

        isInsideAnySlot = allTouchedSlots.Count > 0;
        UpdateBestSlot();
    }

    private void UpdateBestSlot()
    {
        // 清理空引用
        candidateSlots.RemoveWhere(s => s == null);

        

        PuzzleSlot bestSlot = null;
        float bestDist = float.PositiveInfinity;
        Vector3 p = transform.position;

        foreach (var s in candidateSlots)
        {
            if (s == null) continue;
            if (s.isFilled) continue;
            // ID 已在 Register 过滤，这里可不写
            float d = (s.transform.position - p).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestSlot = s;
            }
        }

        currentSlot = bestSlot;
        canPlaceInCurrentSlot = (currentSlot != null);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (isPlaced) return;
        if (isReturning) return;

        rb.isKinematic = false;
        rb.useGravity = false;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (isPlaced) return;

        // 松手瞬间再算一次，避免最后一帧被邻槽/顺序影响
        UpdateBestSlot();

        //  成功：松手时在“最近的正确槽位”里，就直接吸附（不需要距离角度限制）
        if (currentSlot != null && !currentSlot.isFilled)
        {
            currentSlot.isFilled = true;   // 先占槽，防止并发
            LockToSlot(currentSlot.transform);
            return;
        }
        //不在任何槽里，不进行任何操作
        if (!isInsideAnySlot)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            return;
        }

        // 失败：不在任何正确槽位候选里 -> 回弹
        PlayFailSound();
        StartReturnToOriginal();
    }

    private void StartReturnToOriginal()
    {
        if (returnCo != null) StopCoroutine(returnCo);
        returnCo = StartCoroutine(ReturnToOriginal());
    }

    private IEnumerator ReturnToOriginal()
    {
        isReturning = true;

        // 清理slot状态（不依赖 TriggerExit）
        candidateSlots.Clear();
        isInsideAnySlot = false;
        currentSlot = null;
        canPlaceInCurrentSlot = false;

        // 回弹期间避免再次被抓
        if (grabInteractable != null) grabInteractable.enabled = false;

        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float t = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            transform.position = Vector3.Lerp(startPos, originalPos, t);
            transform.rotation = Quaternion.Slerp(startRot, originalRot, t);
            yield return null;
        }

        transform.position = originalPos;
        transform.rotation = originalRot;

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        if (grabInteractable != null) grabInteractable.enabled = true;

        TriggerFailHighlight();//失败高亮

        isReturning = false;
        returnCo = null;
        




    }

    public void LockToSlot(Transform slotTransform)
    {
        if (isPlaced) return;
        isPlaced = true;

        // 一旦放置成功，不再允许 slot 竞争
        candidateSlots.Clear();
        isInsideAnySlot = false;
        currentSlot = null;
        canPlaceInCurrentSlot = false;

        OnAnyPiecePlaced?.Invoke(this);

        PlaySuccessSound();
        SendGrabberHaptic();

        if (labelObject != null) labelObject.SetActive(false);

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
            transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
            yield return null;
        }

        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    private void SendGrabberHaptic()
    {
        if (grabInteractable == null) return;
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

    public void ResetHomeToCurrent()
    {
        originalPos = transform.position;
        originalRot = transform.rotation;
    }

    public void SetLabelVisible(bool visible)
    {
        if (labelObject != null)
            labelObject.SetActive(visible);
    }

    public void ResetForNewRound(Vector3 pos, Quaternion rot, bool labelVisible)
    {
        StopAllCoroutines();

        candidateSlots.Clear();
        isPlaced = false;
        isInsideAnySlot = false;
        currentSlot = null;
        canPlaceInCurrentSlot = false;
        isReturning = false;
        returnCo = null;

        transform.position = pos;
        transform.rotation = rot;

        if (grabInteractable != null)
            grabInteractable.enabled = true;

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        originalPos = pos;
        originalRot = rot;

        SetLabelVisible(labelVisible);
    }
}