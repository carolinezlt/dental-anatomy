using UnityEngine;

public class PuzzleSlot : MonoBehaviour
{
    [Header("Accept ID")]
    public string acceptID;

    [Header("Status")]
    public bool isFilled = false;

    private void OnTriggerEnter(Collider other)
    {
        var piece = other.GetComponentInParent<PuzzlePiece>();
        if (piece == null) return;
        if (piece.isPlaced) return;
        if (isFilled) return;

        piece.RegisterCandidateSlot(this);
    }

    private void OnTriggerStay(Collider other)
    {
        // Stay ×ö¶µµ×£¬±ÜÃâ Enter Â©µô
        var piece = other.GetComponentInParent<PuzzlePiece>();
        if (piece == null) return;
        if (piece.isPlaced) return;
        if (isFilled) return;

        piece.RegisterCandidateSlot(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var piece = other.GetComponentInParent<PuzzlePiece>();
        if (piece == null) return;

        piece.UnregisterCandidateSlot(this);
    }

    public void ResetSlot()
    {
        isFilled = false;
    }
}