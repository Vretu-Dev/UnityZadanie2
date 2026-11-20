using UnityEngine;
using EasyPeasyFirstPersonController;

[RequireComponent(typeof(FirstPersonController))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Komponent Audio Source")]
    [Tooltip("AudioSource do odtwarzania kroków i pływania. Jeśli puste, skrypt doda go automatycznie.")]
    public AudioSource audioSource;

    [Header("Dźwięki kroków (Chodzenie)")]
    [Tooltip("Dodaj jeden lub kilka dźwięków kroków (np. po piasku). Skrypt będzie losować między nimi.")]
    public AudioClip[] footstepSounds;
    public float walkStepInterval = 0.5f;   // Odstęp między krokami przy chodzeniu
    public float sprintStepInterval = 0.35f; // Odstęp między krokami przy biegu

    [Header("Dźwięki pływania")]
    [Tooltip("Dźwięki poruszania się w wodzie (pluski / wiosłowanie ramionami)")]
    public AudioClip[] swimSounds;
    public float swimStrokeInterval = 0.8f;

    [Header("Dźwięki wpadania i wychodzenia z wody")]
    public AudioClip waterSplashEnter; // Dźwięk wskoczenia do wody
    public AudioClip waterExitSplash;  // Dźwięk wynurzenia / wyjścia na brzeg

    [Header("Głośność")]
    [Range(0f, 1f)] public float footstepVolume = 0.7f;
    [Range(0f, 1f)] public float swimVolume = 0.8f;
    [Range(0f, 1f)] public float splashVolume = 1.0f;

    private FirstPersonController fpc;
    private CharacterController cc;
    private float stepTimer = 0f;
    private bool wasInWater = false;

    void Start()
    {
        fpc = GetComponent<FirstPersonController>();
        cc = fpc.characterController;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Konfiguracja źródła dźwięku
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D (dźwięk słyszany bezpośrednio przez gracza)
        
        wasInWater = fpc.isInWater;
    }

    void Update()
    {
        if (fpc == null || cc == null) return;

        // 1. Wykrywanie wskoczenia / wyjścia z wody
        HandleWaterTransition();

        // 2. Obsługa dźwięków ruchu
        float speed = cc.velocity.magnitude;

        if (fpc.isInWater)
        {
            // Pływanie
            if (speed > 0.5f)
            {
                stepTimer += Time.deltaTime;
                float interval = fpc.input.sprint ? (swimStrokeInterval * 0.75f) : swimStrokeInterval;

                if (stepTimer >= interval)
                {
                    PlayRandomSound(swimSounds, swimVolume);
                    stepTimer = 0f;
                }
            }
            else
            {
                stepTimer = 0f;
            }
        }
        else if (fpc.isGrounded)
        {
            // Chodzenie / bieg po lądzie
            if (speed > 0.5f)
            {
                stepTimer += Time.deltaTime;
                float interval = fpc.input.sprint ? sprintStepInterval : walkStepInterval;

                if (stepTimer >= interval)
                {
                    PlayRandomSound(footstepSounds, footstepVolume);
                    stepTimer = 0f;
                }
            }
            else
            {
                stepTimer = 0f;
            }
        }
        else
        {
            // W powietrzu (skok / spadanie)
            stepTimer = 0f;
        }
    }

    private void HandleWaterTransition()
    {
        // Wskoczenie do wody
        if (!wasInWater && fpc.isInWater)
        {
            if (waterSplashEnter != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(waterSplashEnter, splashVolume);
            }
            stepTimer = 0f;
        }
        // Wyjście z wody na ląd
        else if (wasInWater && !fpc.isInWater)
        {
            if (waterExitSplash != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(waterExitSplash, splashVolume * 0.7f);
            }
        }

        wasInWater = fpc.isInWater;
    }

    private void PlayRandomSound(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0) return;

        // Wybieramy losowy klip z tablicy
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        // Lekka modyfikacja wysokości tonu (pitch), by każdy krok brzmiał unikalnie i naturalnie
        audioSource.pitch = Random.Range(0.92f, 1.08f);
        audioSource.PlayOneShot(clip, volume);
    }
}
