using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    static SoundManager instance;
    static readonly Dictionary<string, AudioClip> clips = new();
    AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (instance != null) return;
        GameObject go = new(nameof(SoundManager));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SoundManager>();
        go.AddComponent<BackgroundMusic>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        source = gameObject.AddComponent<AudioSource>();
    }

    public static AudioClip GetClip(string name)
    {
        if (clips.TryGetValue(name, out AudioClip clip)) return clip;
        clip = Resources.Load<AudioClip>("Sounds/" + name);
#if UNITY_EDITOR
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Sounds/{name}.ogg");
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Sounds/{name}.mp3");
#endif
        clips[name] = clip;
        return clip;
    }

    static void Play(string name)
    {
        if (instance == null) Init();
        AudioClip clip = GetClip(name);
        if (clip != null) instance.source.PlayOneShot(clip);
    }

    public static void PlayClass() => Play("selected class");
    public static void PlayReward() => Play("selected spell and relic");
    public static void PlayShot() => Play("shooting sound");
}
