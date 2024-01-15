using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Audio;
using OVR;
using System.Text;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    #region 싱글톤
    public static AudioManager Instance
    {
        get
        {
            // 만약 싱글톤 변수에 아직 오브젝트가 할당되지 않았다면
            if (m_Instance == null)
            {
                // 씬에서 GameManager 오브젝트를 찾아 할당
                //m_Instance = FindObjectOfType<AudioManager>();
                GameObject audioManager = new GameObject("AudioManager");
                m_Instance = audioManager.AddComponent<AudioManager>();
                m_Instance.GetComponent<AudioManager>().Initialized();

            }

            // 싱글톤 오브젝트를 반환
            return m_Instance;
        }
    }
    private static AudioManager m_Instance; // 싱글톤이 할당될 static 변수    
    #endregion

    public Sound backGroundMusic;
    public SerializableDictionary<string, Sound> musicSounds = new SerializableDictionary<string, Sound>();
    public SerializableDictionary<string, Sound> sfxSounds = new SerializableDictionary<string, Sound>();
    public AudioMixer audioMixer;
    public AudioSource musicSource, sfxSource;
    
    private string[] audioPath = new string[2];


    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드될 때 마다 오디오 초기화
        AudioInit();
    }

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    private void Start()
    {
        if(backGroundMusic != null)
        {
            PlayMusic(backGroundMusic.name);
        }
    }


    // 초기화
    private void Initialized()
    {
        audioMixer = Resources.Load<AudioMixer>("Audio/ProjectDR_AudioMixer");
        musicSource = this.gameObject.AddComponent<AudioSource>();
        sfxSource = this.gameObject.AddComponent<AudioSource>();
        musicSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("BGM")[0];
        sfxSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
    }
    // 오디오 초기화
    public void AudioInit()
    {
        musicSounds.Clear();
        sfxSounds.Clear();
        musicSounds = new SerializableDictionary<string, Sound>();
        sfxSounds = new SerializableDictionary<string, Sound>();
    }

    #region ######################_Play Audio_#####################
    /// <summary> BGM을 재생하는 메서드 </summary>
    public void PlayMusic(string name)
    {
        Sound sound = musicSounds[name];
        if (sound == null)
        {
            GFunc.Log("Sound Not Found");
        }
        else
        {
            musicSource.clip = sound.clip;
            musicSource.Play();
        }
    }
    /// <summary> 사운드 이펙트를 재생하는 메서드 </summary>
    public void PlaySFX(string name)
    {
        Sound sound = sfxSounds[name];
        if (sound == null)
        {
            GFunc.Log("SFX Not Found");
        }
        else
        {
            sfxSource.PlayOneShot(sound.clip);
        }
    }
    #endregion

    #region ##################_Set Audio Volume_#################
    public void MasterVolume(float volume)
    {
        audioMixer.SetFloat("Master", volume);
    }
    public void MusicVolume(float volume)
    {

        audioMixer.SetFloat("BGM", volume);

    }
    public void SFXVolume(float volume)
    {
        audioMixer.SetFloat("SFX", volume);

    }
    #endregion

    #region ######################_Add Audio_#####################
    /// <summary>
    /// BGM을 추가하는 메서드
    /// </summary>
    /// <param name="name">"Audio/BGM/" 경로의 파일명을 입력</param>
    public void AddBGM(string name)
    {
        audioPath[0] = "Audio/BGM/";
        audioPath[1] = name;
        string path = GFunc.SumString(audioPath);
        AudioClip audio = Resources.Load<AudioClip>(path);
        GFunc.Log(path);

        if (audio == null)
        {
            GFunc.Log("BGM을 찾을 수 없습니다.");
            return;
        }

        Sound newSound = new Sound();
        newSound.name = name;
        newSound.clip = audio;

        musicSounds.Add(name, newSound);
    }
    /// <summary>
    /// 사운드 이펙트를 추가하는 메서드
    /// </summary>
    /// <param name="name">"Audio/SFX/" 경로의 파일명을 입력</param>
    public void AddSFX(string name)
    {
        audioPath[0] = "Audio/SFX/";
        audioPath[1] = name;
        AudioClip audio = Resources.Load<AudioClip>(GFunc.SumString(audioPath));

        if (audio == null)
        {
            GFunc.Log("SFX을 찾을 수 없습니다.");
            return;
        }

        Sound newSound = new Sound();
        newSound.name = name;
        newSound.clip = audio;

        sfxSounds.Add(name, newSound);
    }
    /// <summary>BGM을 지정하는 메서드 </summary>
    public void SetBGM(Sound bgm)
    {
        backGroundMusic = bgm;
    }
    #endregion
}