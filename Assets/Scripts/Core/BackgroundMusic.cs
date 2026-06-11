using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    void Awake()
    {
        AudioClip clip = SoundManager.GetClip("magic_crystal_backgroundmusic");
        if (clip == null) return;
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.Play();
    }
}
