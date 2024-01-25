using BNG;
using Js.Crafting;
using Js.Quest;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;
using Slider = UnityEngine.UI.Slider;

public class PlayerOption : MonoBehaviour
{
    [Header("Reference")]
    public GameObject player;
    public GameObject optionUI;      // 켜지고 꺼질 옵션의 UI
    private PlayerRotation rotation;
    public ScreenFader fader;

    [Header("Scene")]
    public string loginSceneName;    // 이동할 로그인씬 명

    [Header("TMP Text")]
    public TMP_Text rotationText;    // 회전속도 텍스트

    [Header("Brightness")]
    public Volume volume;
    private ColorAdjustments brightness;

    [Header("Slider")]
    public Slider masterSlider;      // 마스터 사운드 슬라이더
    public Slider backgroundSlider;  // 배경음 슬라이더
    public Slider soundEffectSlider; // 효과음 슬라이더
    public Slider brightSlider;      // 화면 밝기 슬라이더

    public VREmulator vrEmulator;

    [Header("Input")]
    //[Tooltip("The key(s) to use to toggle locomotion type")]
    public List<ControllerBinding> optionInput = new List<ControllerBinding>() { ControllerBinding.None };
    public InputActionReference InputAction = default;

    private void Start()
    {
        // 에디터에서만 vr에뮬레이터 켜기
#if PC_Build
        vrEmulator.enabled = true;
#endif


        rotation = player.GetComponent<PlayerRotation>();
        fader = player.GetComponentInChildren<ScreenFader>();
        volume.profile.TryGet<ColorAdjustments>(out brightness);
        if (brightness == null)
            GFunc.LogError("볼륨을 찾을 수 없음");

        // 세팅 값 가져오기
        GetSettingValue();
    }

    // 환경설정 UI 입력
    public virtual void CheckOptionToggleInput()
    {
        // Check for bound controller button
        for (int x = 0; x < optionInput.Count; x++)
        {
            if (InputBridge.Instance.GetControllerBindingValue(optionInput[x]))
            {
                if (optionUI)
                {
                    optionUI.SetActive(!optionUI.activeSelf);
                }
            }
        }
    }

    // 환경설정 토글
    private void OnEnable()
    {
        InputAction.action.performed += ToggleActive;
    }

    private void OnDisable()
    {
        InputAction.action.performed -= ToggleActive;
    }

    public void ToggleActive(InputAction.CallbackContext context)
    {
        if (optionUI)
        {
            optionUI.SetActive(!optionUI.activeSelf);
        }
    }

    // 회전 속도 변경
    public void SetRotation()
    {
        rotation.SetRotation();
        rotationText.text = string.Format(rotation.SnapRotationAmount + "˚");
        UserDataManager.Instance.rotationAmount = rotation.SnapRotationAmount;
    }
    public void SetRotation(float value)
    {
        rotation.SetRotation(value);
        rotationText.text = string.Format(rotation.SnapRotationAmount + "˚");
    }

    // 메인으로 이동
    public void GotoMain()
    {
        // 플레이어 리셋 함수 호출
        UserData.ResetPlayer();
        UserDataManager.Instance.DestroyUserDataManager();

        fader.DoFadeIn();
        StartCoroutine(SceneChange(loginSceneName));
    }
    IEnumerator SceneChange(string sceneName)
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(sceneName);
    }

    // 게임 종료
    public void QuitGame()
    {
        Application.Quit();
    }

    // 화면 밝기 조정
    public void BrightnessSlider(float value)
    {
        brightness.postExposure.value= value;
        UserDataManager.Instance.brightness = value; // 유저 데이터에 저장
    }

    // 마스터 조절 버튼
    public void SetMasterButton(float value)
    {
        float newValue = ValueCheck(masterSlider.value, value);
        masterSlider.value = newValue;
        SetMasterSlider(newValue);
    }
    // 배경음 조절 버튼
    public void SetBackgroundButton(float value)
    {
        float newValue = ValueCheck(backgroundSlider.value, value);
        backgroundSlider.value = newValue;
        SetBGMSlider(newValue);
    }
    // 사운드 이펙트 조절 버튼
    public void SetSoundEffectButton(float value)
    {
        float newValue = ValueCheck(soundEffectSlider.value, value);
        soundEffectSlider.value = newValue;
        SetSFXSlider(newValue);

    }

    // 마스터 사운드 조정
    public void SetMasterSlider(float value)
    {
        if (!AudioManager.Instance)
        {
            GFunc.Log("오디오 매니저를 찾을 수 없습니다.");
            return;
        }
        AudioManager.Instance.SetMasterVolume(value);
        masterSlider.value = value;
        UserDataManager.Instance.masterSound = value; // 유저 데이터에 저장
    }
    // 배경음 조정
    public void SetBGMSlider(float value)
    {
        if(!AudioManager.Instance)
        {
            //GFunc.Log("오디오 매니저를 찾을 수 없습니다.");
            return;
        }
        AudioManager.Instance.SetBGMVolume(value);
        backgroundSlider.value = value;
        UserDataManager.Instance.backgroundSound = value; // 유저 데이터에 저장
    }

    // 효과음 조정 
    public void SetSFXSlider(float value)
    {
        if (!AudioManager.Instance)
        {
            GFunc.Log("오디오 매니저를 찾을 수 없습니다.");
            return;
        }
        AudioManager.Instance.SetSFXVolume(value);
        soundEffectSlider.value = value;
        UserDataManager.Instance.sfx = value; // 유저 데이터에 저장
    }

    public void GetSettingValue()
    {
        if(!UserDataManager.Instance)
        {
            GFunc.LogError("User Data Manager를 찾지 못했습니다.");
            return;
        }

        SetRotation(UserDataManager.Instance.rotationAmount);
        SetMasterSlider(UserDataManager.Instance.masterSound);
        SetBGMSlider(UserDataManager.Instance.backgroundSound);
        SetSFXSlider(UserDataManager.Instance.sfx);
        BrightnessSlider(UserDataManager.Instance.brightness);
    }

    // 값 체크
    float ValueCheck(float slider, float value)
    {
        float newValue = slider + value;
        if (100 < newValue)
            newValue = 100;
        else if (newValue < 0)
            newValue = 0;
        return newValue;
    }
}
