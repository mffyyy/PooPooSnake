using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BgmPlayer : MonoBehaviour
{
    private static BgmPlayer instance;

    public AudioSource audioSource;
    public AudioClip bgmClip;
    public bool persistAcrossScenes = true;
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            if (audioSource != null)
                audioSource.Stop();

            enabled = false;
            Destroy(this);
            return;
        }

        instance = this;
        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (audioSource == null || bgmClip == null)
            return;

        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }
}
