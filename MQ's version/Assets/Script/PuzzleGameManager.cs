using System.Collections.Generic;
using UnityEngine;

public class PuzzleGameManager : MonoBehaviour
{
    [Header("Difficulty Toggles")]
    public bool labelsOn = true;
    public bool shuffleWithinSide = false;
    public bool swapLeftRight = false;

    [Header("Layout Points")]
    public Transform[] leftPoints;
    public Transform[] rightPoints;

    [Header("Teeth Pieces")]
    public PuzzlePiece[] leftTeeth;
    public PuzzlePiece[] rightTeeth;

    private void Start()
    {
        ApplyDifficultyFromSettings();
        RebuildForCurrentDifficulty();
    }

    // ====== Runtime switch API (UI按钮会调用它们) ======
    public void SwitchToEasy()
    {
        GameSettings.difficulty = 0;
        ApplyDifficultyFromSettings();
        RebuildForCurrentDifficulty();
    }

    public void SwitchToHard()
    {
        GameSettings.difficulty = 1;
        ApplyDifficultyFromSettings();
        RebuildForCurrentDifficulty();
    }

    // ====== One place to rebuild layout/labels/home ======
    private void RebuildForCurrentDifficulty()
    {
        ApplyLayout();
        ApplyLabels();
        ResetAllHomePositions(); // 把“当前摆放”记录为每颗牙的home，方便你做“放错弹回”
    }

    private void ApplyDifficultyFromSettings()
    {
        if (GameSettings.difficulty == 0) // Easy
        {
            labelsOn = true;
            shuffleWithinSide = false;
            swapLeftRight = false;
        }
        else // Hard
        {
            labelsOn = false;
            shuffleWithinSide = true;
            swapLeftRight = true;
        }
    }

    public void ApplyLayout()
    {
        List<PuzzlePiece> left = new List<PuzzlePiece>(leftTeeth);
        List<PuzzlePiece> right = new List<PuzzlePiece>(rightTeeth);

        if (shuffleWithinSide)
        {
            Shuffle(left);
            Shuffle(right);
        }

        if (swapLeftRight)
        {
            var temp = left;
            left = right;
            right = temp;
        }

        PlaceGroup(left, leftPoints);
        PlaceGroup(right, rightPoints);
    }

    private void PlaceGroup(List<PuzzlePiece> pieces, Transform[] points)
    {
        int count = Mathf.Min(pieces.Count, points.Length);
        for (int i = 0; i < count; i++)
        {
            pieces[i].transform.position = points[i].position;
            pieces[i].transform.rotation = points[i].rotation;
        }
    }

    private void ApplyLabels()
    {
        foreach (var p in leftTeeth)
            if (p != null) p.SetLabelVisible(labelsOn);

        foreach (var p in rightTeeth)
            if (p != null) p.SetLabelVisible(labelsOn);
    }

    private void ResetAllHomePositions()
    {
        foreach (var p in leftTeeth)
            if (p != null) p.ResetHomeToCurrent();

        foreach (var p in rightTeeth)
            if (p != null) p.ResetHomeToCurrent();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
