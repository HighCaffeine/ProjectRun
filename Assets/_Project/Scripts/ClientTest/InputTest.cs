using System.Collections.Generic;
using UnityEngine;

public class InputTest : MonoBehaviour
{
    [SerializeField] Animation anim;
    [SerializeField] List<AnimationClip> animationClips;

    bool isActive;

    void Start()
    {
        foreach (AnimationClip clip in animationClips)
        {
            clip.legacy = true;

            anim.AddClip(clip, clip.name);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isActive)
            {
                anim.Play(animationClips[0].name);
            }
            else
            {
                anim.Play(animationClips[1].name);  
            }

            isActive = !isActive;
        }
    }
}