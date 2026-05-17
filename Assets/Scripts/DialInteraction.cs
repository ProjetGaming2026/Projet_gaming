using UnityEngine;

public class DialInteraction : MonoBehaviour
{
    [Header("Settings")]
    public int currentNumber = 0;
    public float rotationPerStep = 36f;

    [Header("Reference")]
    public PadlockPuzzle padlockPuzzle; // drag manually in Inspector

    public void ScrollUp()
{
    currentNumber = (currentNumber + 1) % 10;
    transform.Rotate(-rotationPerStep, 0, 0); // changed + to -
    Debug.Log(gameObject.name + " is now: " + currentNumber);
    if (padlockPuzzle != null)
        padlockPuzzle.CheckCode();
    else
        Debug.Log("❌ padlockPuzzle is NULL on " + gameObject.name);
}

public void ScrollDown()
{
    currentNumber = (currentNumber - 1 + 10) % 10;
    transform.Rotate(rotationPerStep, 0, 0); // changed - to +
    Debug.Log(gameObject.name + " is now: " + currentNumber);
    if (padlockPuzzle != null)
        padlockPuzzle.CheckCode();
    else
        Debug.Log("❌ padlockPuzzle is NULL on " + gameObject.name);
}

    public int GetNumber() => currentNumber;
}