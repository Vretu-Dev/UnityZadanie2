using UnityEngine;
using UnityEngine.Rendering;
using EasyPeasyFirstPersonController;

public class UnderwaterEffect : MonoBehaviour
{
    [Header("Wysokość wody")]
    [Tooltip("Dokładna wysokość tafli wody w scenie")]
    public float waterHeight = 74.5f;

    [Tooltip("Opcjonalnie: przeciągnij obiekt Water Surface, aby wysokość brała się z niego")]
    public Transform waterSurface;

    [Header("Płynność przejścia (Fade)")]
    [Tooltip("Szybkość płynnego przejścia pod wodę i z powrotem (im wyższa, tym szybszy fade, np. 2.5 - 4.0)")]
    [Range(0.5f, 10f)]
    public float transitionSpeed = 3f;

    [Header("Wpływ kąta patrzenia")]
    [Tooltip("Jak mocno patrzenie w górę/dół wpływa na wynurzenie wzroku (0 = brak wpływu)")]
    [Range(0f, 1f)]
    public float lookSensitivity = 0.35f;

    [Tooltip("Wymagaj, by postać była w wodzie (FirstPersonController.isInWater)")]
    public bool checkPlayerInWater = true;

    [Header("Post-Processing Volume (Opcjonalnie)")]
    [Tooltip("Przeciągnij tutaj swój Box Volume – jego waga będzie płynnie sterowana od 0 do 1!")]
    public Volume underwaterVolume;

    [Header("Ustawienia mgły podwodnej")]
    public Color underwaterFogColor = new Color(0.04f, 0.18f, 0.25f, 1f); // Głęboki granat/morski
    public float fogStartDistance = 0f;
    public float fogEndDistance = 12f; // Zasięg widzenia we mgle pod wodą
    public float underwaterFarClip = 20f; // Zasięg widzenia kamery pod wodą

    [Header("Status na żywo (Tylko do odczytu)")]
    [SerializeField] private bool targetUnderwater = false;
    [SerializeField] [Range(0f, 1f)] private float currentTransition = 0f;
    [SerializeField] private float currentCameraY = 0f;
    [SerializeField] private float effectiveViewY = 0f;

    private Camera cam;
    private FirstPersonController fpc;

    private bool originalFog;
    private Color originalFogColor;
    private FogMode originalFogMode;
    private float originalFogStart;
    private float originalFogEnd;

    private CameraClearFlags originalClearFlags;
    private Color originalBgColor;
    private float originalFarClip;

    void Start()
    {
        fpc = GetComponent<FirstPersonController>();
        if (fpc == null) fpc = GetComponentInParent<FirstPersonController>();

        if (fpc != null && fpc.playerCamera != null)
        {
            cam = fpc.playerCamera.GetComponent<Camera>();
        }
        if (cam == null) cam = Camera.main ?? GetComponentInChildren<Camera>();

        if (waterSurface == null)
        {
            GameObject waterObj = GameObject.Find("Water Surface");
            if (waterObj != null) waterSurface = waterObj.transform;
        }

        // Automatycznie odnajdujemy Box Volume, jeśli użytkownik go nie przeciągnął
        if (underwaterVolume == null)
        {
            GameObject volObj = GameObject.Find("Box Volume");
            if (volObj != null) underwaterVolume = volObj.GetComponent<Volume>();
        }

        // Jeśli mamy Volume, włączamy go jako globalny dla płynnego sterowania wagą
        if (underwaterVolume != null)
        {
            underwaterVolume.isGlobal = true;
            underwaterVolume.weight = 0f;
        }

        // Zapamiętanie ustawień lądowych
        originalFog = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;

        if (originalFogEnd <= 0) originalFogEnd = 300f; // Domyślny duży dystans, jeśli mgła na lądzie była wyłączona

        if (cam != null)
        {
            originalClearFlags = cam.clearFlags;
            originalBgColor = cam.backgroundColor;
            originalFarClip = cam.farClipPlane;
        }
    }

    void Update()
    {
        if (cam == null) return;

        if (waterSurface != null)
        {
            waterHeight = waterSurface.position.y;
        }

        currentCameraY = cam.transform.position.y;

        // Kąt patrzenia: forward.y (patrzenie w górę podnosi punkt widzenia wzroku)
        float lookPitch = cam.transform.forward.y;
        effectiveViewY = currentCameraY + (lookPitch * lookSensitivity);

        bool playerInWater = !checkPlayerInWater || (fpc == null) || fpc.isInWater;

        // Cel: czy powinniśmy być pod wodą
        targetUnderwater = playerInWater && (effectiveViewY < waterHeight);

        // Płynne przesuwanie wartości transition w czasie (od 0.0 do 1.0)
        float targetVal = targetUnderwater ? 1f : 0f;
        currentTransition = Mathf.MoveTowards(currentTransition, targetVal, Time.deltaTime * transitionSpeed);

        // Aplikujemy płynny efekt
        ApplySmoothTransition(currentTransition);
    }

    private void ApplySmoothTransition(float t)
    {
        // 1. Płynne sterowanie Volume (rozmycie Depth of Field, winieta, barwa)
        if (underwaterVolume != null)
        {
            underwaterVolume.weight = t;
        }

        // 2. Płynna mgła (kolor i zasięg)
        if (t > 0.001f || originalFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(originalFogColor, underwaterFogColor, t);
            RenderSettings.fogStartDistance = Mathf.Lerp(originalFogStart, fogStartDistance, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(originalFogEnd, fogEndDistance, t);
        }
        else
        {
            RenderSettings.fog = originalFog;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogMode = originalFogMode;
        }

        // 3. Płynny zasięg kamery (Far Clip Plane)
        if (underwaterFarClip > 0)
        {
            cam.farClipPlane = Mathf.Lerp(originalFarClip, underwaterFarClip, t);
        }

        // 4. Płynne przełączenie tła/nieba (Skybox -> Kolor wody)
        cam.backgroundColor = Color.Lerp(originalBgColor, underwaterFogColor, t);

        // Przełączamy na jednolity kolor wody dopiero gdy jesteśmy głęboko zanurzeni (t > 0.8),
        // dzięki czemu przy wynurzaniu niebo płynnie wyłania się zza mgły
        if (t >= 0.8f)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
        else
        {
            cam.clearFlags = originalClearFlags;
        }
    }

    void OnDisable()
    {
        if (cam != null)
        {
            ApplySmoothTransition(0f);
        }
        if (underwaterVolume != null)
        {
            underwaterVolume.weight = 0f;
        }
    }
}
