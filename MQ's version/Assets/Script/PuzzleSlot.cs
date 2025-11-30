using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleSlot : MonoBehaviour
{
    [Header("Accept ID")]
    public string acceptID;

    [Header("Accepted Max Distance")]
    public float maxDistance= 0.08f;  

    [Header("Accepted Max Angle")]
    public float maxAngle=25f;       

    [Header("Status")]
    public bool isFilled = false;

    private void OnTriggerStay(Collider other)
    {

        Debug.Log($"TriggerStay:{other.name}");
        if (isFilled) return;

       
        PuzzlePiece piece = other.GetComponentInParent<PuzzlePiece>();
        if (piece == null) return;
        if (piece.isPlaced) return;          
        if (piece.toothID != acceptID) return; 

        
        Transform toothTransform = piece.transform;

        
        float dist = Vector3.Distance(toothTransform.position, transform.position);
        //if (dist > maxDistance) return;

        
        float angle = Quaternion.Angle(toothTransform.rotation, transform.rotation);
        //if (angle > maxAngle) return;
        Debug.Log($"[SLOT DEBUG] Dist={dist}, MaxDist={maxDistance}, Angle={angle}, MaxAngle={maxAngle}");

        piece.LockToSlot(transform);
        isFilled = true;
    }
}
