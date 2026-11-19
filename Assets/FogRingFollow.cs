using UnityEngine;

public class FogRingFollow : MonoBehaviour
{
    [Header("Cel do śledzenia")]
    [Tooltip("Gracz lub kamera. Jeśli puste, znajdzie FirstPersonController automatycznie.")]
    public Transform target;

    [Header("Wysokość")]
    [Tooltip("Czy trzymać stałą wysokość Y (zalecane, by mgła nie podskakiwała przy skakaniu)")]
    public bool lockY = true;
    public float fixedY = 74.5f; // Wysokość tafli wody

    [Header("Efekt życia (Rotacja)")]
    [Tooltip("Prędkość powolnego obracania się mgły na horyzoncie (np. 0.5 - 1.5)")]
    public float rotationSpeed = 0.8f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("FirstPersonController");
            if (player != null) target = player.transform;
            else if (Camera.main != null) target = Camera.main.transform;
        }

        if (lockY && fixedY == 0)
        {
            fixedY = transform.position.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Podążamy za graczem w poziomie X i Z
        float newY = lockY ? fixedY : target.position.y;
        transform.position = new Vector3(target.position.x, newY, target.position.z);

        // 2. Bardzo powolna rotacja, żeby mgła żyła
        if (rotationSpeed != 0)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}