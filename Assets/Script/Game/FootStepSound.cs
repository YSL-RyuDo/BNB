using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootStepSound : MonoBehaviour
{
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip AttackClip;
    [SerializeField] private AudioSource source;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.9f;

    private void Awake()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
    }

    public void OnFootstep()
    {
        if (source == null) return;
        if (footstepClips == null || footstepClips.Length == 0) return;

        int idx = Random.Range(0, footstepClips.Length);
        source.PlayOneShot(footstepClips[idx], volume);
    }
    public void OnAttack()
    {
        if (source == null) return;
        if (AttackClip == null) return;

        source.PlayOneShot(AttackClip, volume);
    }

}
