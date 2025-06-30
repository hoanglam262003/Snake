using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ClientMusicPlayer : Singleton<ClientMusicPlayer>
{
    [SerializeField] private AudioClip eatAudioClip;
    private AudioSource _audioSource;
    
    public override void Awake()
    {
        base.Awake();
        _audioSource = GetComponent<AudioSource>();
    }
    public void EatAudioClip()
    {
        _audioSource.clip = eatAudioClip;
        _audioSource.Play();
    }
}
