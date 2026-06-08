using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettingInfo : MonoBehaviour
{
    //게임에 필요한 세팅 값들
    protected float frame { get; private set; }
    protected struct SoundValue 
    {
        private float mainSound;
        private float backgroundSound;
        private float sfxSound;

        public float GetMainSound() { return mainSound; }
        public float GetBackGroundSound() { return backgroundSound; }
        public float GetSFXSound() { return sfxSound; }
    } 

    //json으로 설정값 읽어서 기본 세팅.
    void Awake()
    {
        
    }
}
