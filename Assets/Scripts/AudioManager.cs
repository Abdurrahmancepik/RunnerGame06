using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager inst;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip backgroundMusic;
    public AudioClip coinSound;
    public AudioClip jumpSound;
    public AudioClip moveSound; // Saða sola geçiþ sesi
    public AudioClip gameOverSound;

    private void Awake()
    {
        inst = this;
    }

    private void Start()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayCoinSound()
    {
        PlaySFX(coinSound);
    }

    public void PlayJumpSound()
    {
        PlaySFX(jumpSound);
    }

    public void PlayMoveSound()
    {
        PlaySFX(moveSound);
    }

    public void PlayGameOverSound()
    {
        // Müzik dursun
        if (musicSource != null) musicSource.Stop();

        PlaySFX(gameOverSound);
    }
}
