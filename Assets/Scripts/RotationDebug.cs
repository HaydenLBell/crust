using UnityEngine;

public class RotationDebug : MonoBehaviour
{
    private void Update()
    {
        Debug.Log($"Root Y: {transform.eulerAngles.y} | " +
                  $"PlayerGraphics Y: {transform.Find("PlayerGraphics")?.eulerAngles.y} | " +
                  $"Camera Y: {transform.Find("Camera")?.eulerAngles.y}");
    }
}