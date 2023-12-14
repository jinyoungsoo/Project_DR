using JetBrains.Annotations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Unity.VisualScripting;
using Rito.InventorySystem;
using static StatusData;
using OVR.OpenVR;

[System.Serializable]
public class ClearDatas
{
    public List<ClearData> list;
}
[System.Serializable]

public class ClearData
{
    public string MBTI;
    public string Date;
}

// DB에서 가져온 유저의 데이터를 관리하는 클래스

public class UserDataManager : MonoBehaviour
{
    #region 싱글톤 패턴


    private static UserDataManager m_Instance = null; // 싱글톤이 할당될 static 변수    

    public static UserDataManager Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<UserDataManager>();
            if(m_Instance == null)
            {
                GameObject obj = new GameObject("UserDataManager");
                m_Instance = obj.AddComponent<UserDataManager>();
                DontDestroyOnLoad(obj);
            }
            return m_Instance;
        }
    }
    #endregion

    #region 옵저버 패턴
    public delegate void UserDataUpdateDelegate();
    public event UserDataUpdateDelegate OnUserDataUpdate;
    public void UpdateUserData()
    {
        // 데이터가 변경될 때마다 호출
        OnUserDataUpdate?.Invoke();
    }
    #endregion

    #region 유저 데이터
    [Header("DB")]
    public bool dataLoadSuccess;    // 데이터 불러옴 여부

    [Header("User Data")]           // 유저 데이터
    public string PlayerID;
    public float DefaultHP;         // 초기 체력
    public float HP;                // 업그레이드 반영된 체력
    public float GainGold;
    public float GainExp;

    [SerializeField]
    private int _Level;
    public int Level 
    {
        get { return _Level; }
        set
        {
            _Level = value;
            OnUserDataUpdate?.Invoke();
        }
    }

    [SerializeField]
    private int _Exp;
    public int Exp // 플레이어 현재 경험치
    {
        get { return _Exp; }
        set
        {
            _Exp = value;
            OnUserDataUpdate?.Invoke();
        }
    }

    [SerializeField]
    private int _Gold;
    public int Gold  // 플레이어 현재 골드
    {
        get { return _Gold; }
        set
        {
            _Gold = value;
            OnUserDataUpdate?.Invoke();
        }
    }

    [Header("PC Status Data")]      // PC 스탯 업그레이드 데이터
    [SerializeField]
    private int _HPUpgrade;
    public int HPUpgrade  // 플레이어 체력
    {
        get { return _HPUpgrade; }
        set
        {
            _HPUpgrade = value;
            OnUserDataUpdate?.Invoke();
        }
    }
    [SerializeField]
    private int _GainGoldUpgrade;
    public int GainGoldUpgrade        // 플레이어 골드 증가량
    {
        get { return _GainGoldUpgrade; }
        set
        {
            _GainGoldUpgrade = value;
            OnUserDataUpdate?.Invoke();
        }
    }
    [SerializeField]
    private int _GainExpUpgrade;
    public int GainExpUpgrade         // 플레이어 경험치 증가량
    {
        get { return _GainExpUpgrade; }
        set
        {
            _GainExpUpgrade = value;
            OnUserDataUpdate?.Invoke();
        }
    }


    [Header("Weapon Data")]
    public int WeaponAtk;           // 공격력
    public int WeaponCriRate;       // 치명타 확률
    public int WeaponCriDamage;     // 치명타 증가율
    public int WeaponAtkRate;       // 공격 속도

    [Header("Skill Data")]
    public int TeraLv;                // 테라드릴 레벨
    public int GrinderLv;             // 드릴연마 레벨
    public int CrashLv;               // 드릴분쇄 레벨
    public int LandingLv;             // 드릴랜딩 레벨

    [Header("Quest Data")]
    public string QuestMain;          // 현재 퀘스트

    [Header("Clear Data")]
    public int ClearCount;            // 클리어 횟수
    private string JsonData;          // Json을 담을 직렬화된 클리어 데이터
    private ClearDatas _clearDatas;
    public ClearDatas clearDatas      // 클리어 데이터 리스트
    {
        get { return _clearDatas; }
        set
        {
            _clearDatas = value;
            if (value == null)
            {
                Debug.Log("클리어 데이터 없음. 신규 데이터 생성");
                _clearDatas = new ClearDatas();
            }
        }
    }
    private string decodedString;

    [Header("Setting Data")]          // 환경 설정
    public float rotationAmount = 45f;
    [Range(0, 100)]
    public float masterSound, sfx, backgroundSound;
    [Range(-5, 5)]
    public float brightness = 0;

    [Header("Inventory Data")]
    // 호출 순서 문제로 인해 static으로 설정
    public static Item[] items = new Item[Inventory.MaxCapacity];

    [Header("Reference Data")]
    private StatusData  statusData = new StatusData();
    public StatData statData = new StatData();   // 업그레이드 스탯 정보가 담긴 데이터
    #endregion

    // ####################### Awake #######################

    private void Awake()
    {
        // 싱글톤 패턴
        if (m_Instance == null)
        {
            m_Instance = this;
            DontDestroyOnLoad(this.gameObject);            
        }
        else
        { Destroy(gameObject); }

        //SetDebugData();
        Debug.Log("데이터 요청 시간 : " + GetCurrentDate());

        GetReferenceData();
        PlayerDataManager.Update(true); // 데이터 요청
    }
    public void Update()
    {
        if(Input.GetKeyDown("r"))
        {
            SetDebugData();
        }
    }

    // ####################### 데이터 로드 #######################

    // 참조 데이터 로드
    public void GetReferenceData()
    {
        statusData.GetData(statData);
    }

    // 로그인 후, DB에서 데이터 받아오기
    public void GetDataFromDB()
    {
        PlayerID = PlayerDataManager.PlayerID;
        Gold = PlayerDataManager.Gold;
        Exp = PlayerDataManager.Exp;

        // 업그레이드 먼저 불러오기
        HPUpgrade = PlayerDataManager.HP;
        GainGoldUpgrade = PlayerDataManager.GoldIncrease;
        GainExpUpgrade = PlayerDataManager.ExpIncrease;

        // HP 업그레이드 세팅
        DefaultHP = (float)DataManager.instance.GetData(1001, "Health", typeof(float));
        HP = DefaultHP;
        if(HPUpgrade != 0)
        {
            HP = DefaultHP + statData.upgradeHp[HPUpgrade-1].sum;
        }
        // 골드 획득량 업그레이드 세팅
        if (GainGoldUpgrade != 0)
        {
            GainGold = statData.upgradeGainGold[GainGoldUpgrade-1].sum;
        }
        // 경험치 획득량 업그레이드 세팅
        if (GainExpUpgrade != 0)
        {
            GainExp = statData.upgradeGainExp[GainExpUpgrade-1].sum;
        }

        WeaponAtk = PlayerDataManager.WeaponAtk;
        WeaponCriRate = PlayerDataManager.WeaponCriRate;
        WeaponCriDamage = PlayerDataManager.WeaponCriDamage;
        WeaponAtkRate = PlayerDataManager.WeaponAtkRate;

        TeraLv = PlayerDataManager.SkillLevel1;
        GrinderLv = PlayerDataManager.SkillLevel2;
        CrashLv = PlayerDataManager.SkillLevel3;
        LandingLv = PlayerDataManager.SkillLevel4;

        // 총 레벨
        Level = (HPUpgrade + GainGoldUpgrade + GainExpUpgrade);

        QuestMain = PlayerDataManager.QuestMain;
        ClearCount = PlayerDataManager.ClearCount;

        JsonData = PlayerDataManager.ClearMBTIValue;

        // json으로 변환된 string은 .NET Framework 디코딩이 필요
        decodedString = System.Web.HttpUtility.UrlDecode(JsonData);

        clearDatas = JsonUtility.FromJson<ClearDatas>(decodedString);

        if(clearDatas.list == null) // 리스트가 없으면 새로 만들기
        {
            clearDatas.list = new List<ClearData>();
        }

        // 데이터를 불러오고 해야할 이벤트가 있다면 이벤트 실행
        // Ex. 플레이어 상태창, 상점의 현재 골드 등
        dataLoadSuccess = true;
        Debug.Log("데이터 로드 시간 : " + GetCurrentDate());
    }

    // DB에 데이터를 요청하기 위한 메서드
    // 메서드를 action에 담아 호출하면, 데이터가 로드 시 코루틴은 멈춘다.
    public void DBRequst(Action action)
    {
        StartCoroutine(CheckData(action));
    }
    IEnumerator CheckData(Action action)
    {
        yield return new WaitForSeconds(0.1f);
        if (dataLoadSuccess)
        {
            Debug.Log(action + "데이터 로드 완료");
            action();
            yield break;
        }
        yield return null;
    }

    // ####################### 세이브 데이터 ####################### \\
    // 클리어 데이터 신규 저장
    public void SaveClearData(string MBTI)
    {
        // 넣을 데이터 생성
        ClearData newData = new ClearData();
        newData.Date = GetCurrentDate();             // 현재 시간
        newData.MBTI = MBTI;                         // 매개변수 MBTI
        Debug.Log(clearDatas);
        Debug.Log(clearDatas.list);
        Debug.Log(clearDatas.list.Count);

        clearDatas.list.Add(newData);                // 리스트에 추가
        ClearCount = clearDatas.list.Count;          // 클리어 데이터 리스트의 길이가 곧 클리어 카운트

        JsonData = JsonUtility.ToJson(clearDatas);   // json으로 변환

        // 저장 후 업데이트
        PlayerDataManager.Save("clear_mbti_value", JsonData);
        PlayerDataManager.Save("clear_count", ClearCount);
        PlayerDataManager.Update(true);
    }
    // 클리어 시간을 가져오는 함수
    private string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
    }

    // 플레이어 업그레이드 세이브
    public void SavePlayerUpgrade()
    {
        PlayerDataManager.Save("exp", Exp);
        PlayerDataManager.Save("hp", HPUpgrade);
        PlayerDataManager.Save("gold_increase", GainGoldUpgrade);
        PlayerDataManager.Save("exp_increase", GainExpUpgrade);
    }


    // ####################### 디버그용 PC 데이터 세팅 ####################### \\
    public void SetDebugData()
    {
        PlayerDataManager.Save("hp", 0);
        PlayerDataManager.Save("gold", 100000);
        PlayerDataManager.Save("exp", 100000);
        PlayerDataManager.Save("gold_increase", 0);
        PlayerDataManager.Save("exp_increase", 0);

        PlayerDataManager.Save("weapon_atk", 0);
        PlayerDataManager.Save("weapon_cri_rate", 0);        
        PlayerDataManager.Save("weapon_cri_damage", 0);
        PlayerDataManager.Save("weapon_atk_rate", 0);

        PlayerDataManager.Update(true);
    }

    // #######################  PC 데이터 세팅  ####################### \\

    public void PlayerStatusUpgrade(int hpLv, int gainGoldLv, int gainExpLv)
    {
        int newHpLv = hpLv;
        int newGainGoldLv = gainGoldLv;
        int newgainExpLv = gainExpLv;

        HPUpgrade = newHpLv;
        GainGoldUpgrade = newGainGoldLv;
        GainExpUpgrade = newgainExpLv;

        Level = HPUpgrade + GainGoldUpgrade + GainExpUpgrade;

        HP = DefaultHP;
        if (HPUpgrade != 0)
        {
            HP = DefaultHP + statData.upgradeHp[HPUpgrade - 1].sum;
        }
        // 골드 획득량 업그레이드 세팅
        if (GainGoldUpgrade != 0)
        {
            GainGold = statData.upgradeGainGold[GainGoldUpgrade - 1].sum;
        }
        // 경험치 획득량 업그레이드 세팅
        if (GainExpUpgrade != 0)
        {
            GainExp = statData.upgradeGainExp[GainExpUpgrade - 1].sum;
        }
    }


    public void AddGold(int num)
    {
        Gold += num;
    }
}