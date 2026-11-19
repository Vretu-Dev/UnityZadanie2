using UnityEngine;

public class ShipSailor : MonoBehaviour
{
    [Header("Ruch statku")]
    [Tooltip("Prędkość płynięcia w przód")]
    public float speed = 12f;

    [Tooltip("Kierunek poruszania się statku względem własnej osi (zwykle Z / Forward)")]
    public Vector3 moveDirection = Vector3.forward;

    [Header("Granice wody")]
    [Tooltip("Obiekt Water Surface. Jeśli puste, skrypt znajdzie go automatycznie.")]
    public Transform waterSurface;

    [Tooltip("Margines od krawędzi wody (o ile metrów przed krawędzią ma nastąpić reset)")]
    public float boundaryPadding = 10f;

    [Tooltip("Maksymalny dystans od startu (zapasowe zabezpieczenie)")]
    public float maxDistance = 1500f;

    [Header("Klimatyczne kołysanie statku")]
    [Tooltip("Czy statek ma delikatnie kołysać się na falach")]
    public bool enableBobbing = true;
    public float bobFrequency = 1.2f;
    public float rollAmount = 3.5f;   // Kołysanie na boki (lewo-prawo)
    public float pitchAmount = 1.5f;  // Kołysanie przód-tył

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Renderer waterRenderer;
    private Rigidbody rb;

    void Start()
    {
        // Zapamiętujemy dokładną pozycję i rotację początkową
        startPosition = transform.position;
        startRotation = transform.rotation;

        rb = GetComponent<Rigidbody>();

        // Automatyczne odnalezienie tafli wody
        if (waterSurface == null)
        {
            GameObject waterObj = GameObject.Find("Water Surface");
            if (waterObj != null) waterSurface = waterObj.transform;
        }

        if (waterSurface != null)
        {
            waterRenderer = waterSurface.GetComponent<Renderer>();
        }
    }

    void Update()
    {
        // 1. Płynięcie statku przed siebie
        transform.Translate(moveDirection.normalized * (speed * Time.deltaTime), Space.Self);

        // 2. Delikatne kołysanie na wodzie
        if (enableBobbing)
        {
            float roll = Mathf.Sin(Time.time * bobFrequency) * rollAmount;
            float pitch = Mathf.Cos(Time.time * bobFrequency * 0.8f) * pitchAmount;
            
            // Zachowujemy obecny kierunek Y statku, dodając tylko przechyły
            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(pitch, currentEuler.y, roll);
        }

        // 3. Sprawdzamy czy statek wypłynął poza obszar Water Surface
        if (HasLeftWaterArea())
        {
            TeleportToStart();
        }
    }

    private bool HasLeftWaterArea()
    {
        // Sprawdzenie na podstawie granic siatki Water Surface
        if (waterRenderer != null)
        {
            Bounds b = waterRenderer.bounds;
            float minX = b.min.x + boundaryPadding;
            float maxX = b.max.x - boundaryPadding;
            float minZ = b.min.z + boundaryPadding;
            float maxZ = b.max.z - boundaryPadding;

            if (transform.position.x < minX || transform.position.x > maxX ||
                transform.position.z < minZ || transform.position.z > maxZ)
            {
                return true;
            }
        }

        // Zapasowe sprawdzenie dystansu od punktu startu
        if (Vector3.Distance(startPosition, transform.position) > maxDistance)
        {
            return true;
        }

        return false;
    }

    public void TeleportToStart()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Rysowanie punktu startu w Scene View
        Gizmos.color = Color.cyan;
        Vector3 start = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawWireSphere(start, 2f);
        Gizmos.DrawRay(start, transform.forward * 10f);
    }
}
