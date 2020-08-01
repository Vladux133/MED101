using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAudio : MonoBehaviour
{

    public AudioClip[] audioClip;
    public float VolumeScale;
    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayAudioClip(int soundToPlay)
    {
        audioSource.PlayOneShot(audioClip[soundToPlay], VolumeScale);
    }

}