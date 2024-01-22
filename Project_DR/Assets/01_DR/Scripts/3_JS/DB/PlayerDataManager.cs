using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Js.Quest;

public static class PlayerDataManager
{
    /*************************************************
     *                 Public Fields
     *************************************************/
    public static string PlayerID => _id;
    public static int HP => _hp;
    public static int Gold => _gold;
    public static int Exp => _exp;
    public static int GoldIncrease => _gold_increase;
    public static int ExpIncrease => _exp_increase;
    public static int WeaponAtk => _weapon_atk;
    public static int WeaponCriRate => _weapon_cri_rate;
    public static int WeaponCriDamage => _weapon_cri_damage;
    public static int WeaponAtkRate => _weapon_atk_rate;
    public static int WeaponExp => _weapon_exp;
    public static int SkillLevel1_1 => _skill_level_1_1;
    public static int SkillLevel1_2 => _skill_level_1_2;
    public static int SkillLevel2_1 => _skill_level_2_1;
    public static int SkillLevel2_2 => _skill_level_2_2;
    public static int SkillLevel2_3 => _skill_level_2_3;
    public static int SkillLevel3 => _skill_level_3;
    public static int SkillLevel4_1 => _skill_level_4_1;
    public static int SkillLevel4_2 => _skill_level_4_2;
    public static int SkillLevel4_3 => _skill_level_4_3;
    public static int ClearCount => _clear_count;
    public static string ClearMBTIValue => _clear_mbti_value;
    public static string ClearTime => _clear_time;
    public static string QuestMain => _quest_main;
    public static int Tutorial => _tutorial;
    public static bool IsLocal => _isLocal;


    /*************************************************
     *                 Private Fields
     *************************************************/
    [Header("Player")]
    private static string _id = "";           // 플레이어의 ID
    private static int _hp;                   // 플레이어의 체력
    private static int _gold;                 // 플레이어의 소지금
    private static int _exp;                  // 플레이어의 경험치
    private static int _gold_increase;        // 경험치 증가량
    private static int _exp_increase;         // 골드 증가량

    [Header("Weapon")]
    private static int _weapon_atk;           // 무기 공격력
    private static int _weapon_cri_rate;      // 무기 치명타 확률
    private static int _weapon_cri_damage;    // 무기 치명타 공격력
    private static int _weapon_atk_rate;      // 무기 공격 간격
    private static int _weapon_exp;           // 무기 경험치

    [Header("Skill")]
    private static int _skill_level_1_1;      // 스킬 테라드릴 레벨
    private static int _skill_level_1_2;      // 스킬 테라드릴 레벨
    private static int _skill_level_2_1;      // 스킬 드릴연마 레벨
    private static int _skill_level_2_2;      // 스킬 드릴연마 레벨
    private static int _skill_level_2_3;      // 스킬 드릴연마 레벨
    private static int _skill_level_3;        // 스킬 그릴분쇄 레벨
    private static int _skill_level_4_1;      // 스킬 그린랜딩 레벨
    private static int _skill_level_4_2;      // 스킬 그린랜딩 레벨
    private static int _skill_level_4_3;      // 스킬 그린랜딩 레벨

    [Header("ClearData")]
    private static int _clear_count;          // 게임 클리어 횟수
    private static string _clear_mbti_value;  // 저장된 MBTI 수치
    private static string _clear_time;        // 클리어 한 날짜 및 시간
    private static string _quest_main;        // 메인 퀘스트 진행도(직렬화 데이터)
    private static int _tutorial;             // 튜토리얼 클리어 여부(0:False / 1:True)

    [Header("CallDataType")]
    private static bool _isLocal = false;     // 데이터를 불러올 방식(서버/로컬)


    /*************************************************
     *                 Public Methods
     *************************************************/
    /// <summary>
    /// GameManager를 트리거로 비동기로 코루틴을 실행 후, 
    /// <br></br>DB에서 데이터를 호출하고 PlayerDataManager의 데이터를 갱신한다.
    /// </summary>
    public static void Update(bool isUserDataManagerUpdate = false)
    {
        // 불러올 데이터 타입이 서버일 경우에만 업데이트되게 설정
        if (_isLocal.Equals(false))
        {
            // 유저 데이터 매니저에 코루틴을 요청해서 UpdateCoroutine을 실행
            UserDataManager.Instance.StartCoroutine(UpdateCoroutine(isUserDataManagerUpdate));
        }

        // 로컬일 경우
        else
        {
            UserDataManager.Instance.GetDataFromLocal();
        }
    }

    public static void UpdateTutorial(string column = "tutorial")
    {
        // 데이터 매니저에 코루틴을 요청해서 SearchColumnCoroutine을 실행
        DataManager.Instance.StartCoroutine(SearchTutorialCoroutine(column: column));
    }

    /// <summary>
    /// GameManager를 트리거로 비동기로 코루틴을 실행 후, DB에 데이터를 
    /// 저장한다. <br></br>평균적으로 값이 업데이트 되는데 1~3초가 걸린다.
    /// <br></br>사용 예제는 다음과 같다
    /// <br></br>PlayerDataManager.Save("gold", "2000");
    /// <br></br>PlayerDataManager.Save("weapon_cri_rate", "300.5");
    /// </summary>
    public static void Save(string column, string value, bool isUpdate = false)
    {
        // 게임 매니저에 코루틴을 요청해서 SaveCoroutine을 실행
        UserDataManager.Instance.StartCoroutine(SaveCoroutine(column, value, isUpdate));
    }
    public static void Save(string column, object value, bool isUpdate = false)
    {
        // 게임 매니저에 코루틴을 요청해서 SaveCoroutine을 실행
        UserDataManager.Instance.StartCoroutine(SaveCoroutine(column, value.ToString(), isUpdate));
    }

    /// <summary>
    /// 플레이어의 ID를 설정한다.
    /// </summary>
    public static void SetID(string id)
    {
        _id = id;
    }

    /// <summary>
    /// Quest Data를 수동으로 변경한다. 로컬 세이브 데이터 호출용
    /// </summary>
    /// <param name="input"></param>
    public static void SetQuestMain(string input)
    {
        _quest_main = input;
    }

    // 로컬로 데이터를 불러오게끔 설정
    public static void SetCallDataForLocal()
    {
        _isLocal = true;
    }

    // 튜토리얼 여부 호출
    public static int GetTutorial()
    {
        return PlayerPrefs.GetInt("isTutorial");
    }

    // 튜토리얼 여부 저장
    public static void SaveTutorial(int input)
    {
        PlayerPrefs.SetInt("isTutorial", input);
    }


    /*************************************************
     *                 Private Methods
     *************************************************/
    // 폼을 생성 후 반환한다.
    private static WWWForm MakeForm(string command, string id,
        string column = "", string value = "")
    {
        WWWForm form = new WWWForm();
        form.AddField("command", command);
        form.AddField("id", id);
        form.AddField("column", column);
        form.AddField("value", value);

        return form;
    }

    // PlayerDataManager의 데이터를 갱신한다.
    private static void UpdatePlayerDataManager(
        int hp, int gold, int exp, int goldIncrease, int expIncrease,
        int weaponAtk, int weaponCriRate, int weaponCriDamage, int weaponAtkRate,
        int weaponExp, int skillLevel1_1, int skillLevel1_2, int skillLevel2_1, int skillLevel2_2, int skillLevel2_3, 
        int skillLevel3, int skillLevel4_1, int skillLevel4_2, int skillLevel4_3,
        string questMain, int clearCount, string clearMBTIValue, string clearTime, int tutorial
        )
    {
        // [Player]
        _hp = hp;                                  // 플레이어의 체력
        _gold = gold;                              // 플레이어의 소지금
        _exp = exp;                                // 플레이어의 경험치
        _gold_increase = goldIncrease;             // 경험치 증가량
        _exp_increase = expIncrease;               // 골드 증가량

        // [Weapon]
        _weapon_atk = weaponAtk;                   // 무기 공격력
        _weapon_cri_rate = weaponCriRate;          // 무기 치명타 확률
        _weapon_cri_damage = weaponCriDamage;      // 무기 치명타 공격력
        _weapon_atk_rate = weaponAtkRate;          // 무기 공격 간격
        _weapon_exp = weaponExp;                   // 무기 경험치

        // [Skill]
        _skill_level_1_1 = skillLevel1_1;          // 스킬 테라드릴 레벨
        _skill_level_1_2 = skillLevel1_2;          // 스킬 테라드릴 레벨
        _skill_level_2_1 = skillLevel2_1;          // 스킬 드릴연마 레벨
        _skill_level_2_2 = skillLevel2_2;          // 스킬 드릴연마 레벨
        _skill_level_2_3 = skillLevel2_3;          // 스킬 드릴연마 레벨
        _skill_level_3 = skillLevel3;              // 스킬 그릴분쇄 레벨
        _skill_level_4_1 = skillLevel4_1;          // 스킬 그린랜딩 레벨
        _skill_level_4_2 = skillLevel4_2;          // 스킬 그린랜딩 레벨
        _skill_level_4_3 = skillLevel4_3;          // 스킬 그린랜딩 레벨

        // [ClearData]
        ////// TODO: 클리어 데이터의 직렬화를 변환하는 함수를 구현 및
        ////// 연동하기
        _quest_main = questMain;                   // 메인 퀘스트 진행도(직렬화 데이터)
        _clear_count = clearCount;                 // 게임 클리어 횟수
        _clear_mbti_value = clearMBTIValue;        // 저장된 MBTI 수치(직렬화 데이터)
        _clear_time = clearTime;                   // 클리어 한 날짜 및 시간(직렬화 데이터)
        _tutorial = tutorial;                      // 튜토리얼 클리어 여부

        _tutorial = tutorial;

        // 퀘스트 목록을 생성 & 업데이트
        QuestManager.Instance.CreateQuestFromDataTable();
        QuestManager.Instance.UpdateUserQuestData();
    }

    // Data를 PlayerData에 맞게 변환한다.
    private static List<PlayerData> ConvertDataToPlayerData(string data)
    {
        // DB에서 받아온 데이터를 객체화
        string jsonData = "{\"items\": " + data + "}";

        // PlayerData 리스트에 배열을 파싱
        List<PlayerData> playerDataList = ParseJsonArray(jsonData);

        return playerDataList;
    }

    // JSON 배열을 파싱한다.
    private static List<PlayerData> ParseJsonArray(string json)
    {
        return JsonUtility.FromJson<ListWrapper>(json).items;
    }

    // JSON 배열을 감싸는 클래스
    [Serializable]
    private class ListWrapper
    {
        public List<PlayerData> items;
    }

    // 파싱된 데이터를 PlayerDataManager에 업데이트 한다.
    private static void AssignPlayerDataToManager(
        List<PlayerData> playerDataList)
    {
        foreach (var playerData in playerDataList)
        {
            // PlayerDataManager의 정보를 업데이트
            UpdatePlayerDataManager(playerData.hp, playerData.gold, playerData.exp,
                playerData.gold_increase, playerData.exp_increase, playerData.weapon_atk,
                playerData.weapon_cri_rate, playerData.weapon_cri_damage, playerData.weapon_atk_rate,
                playerData.weapon_exp, 
                playerData.skill_level_1_1, playerData.skill_level_1_2, playerData.skill_level_2_1, playerData.skill_level_2_2, playerData.skill_level_2_3, playerData.skill_level_3,
                playerData.skill_level_4_1, playerData.skill_level_4_2, playerData.skill_level_4_3, playerData.quest_main, playerData.clear_count, playerData.clear_mbti_value,
                playerData.clear_time, playerData.tutorial);
        }
    }

    // 파싱된 데이터 중 tutorial만 업데이트 한다.
    private static void UpdateTutorialData(
         List<PlayerData> playerDataList)
    {
        foreach (var playerData in playerDataList)
        {
            _tutorial = playerData.tutorial;
        }
    }


    /*************************************************
     *                  Coroutines
     *************************************************/
    // DB에서 데이터를 가져오기 위한 비동기 코루틴
    private static IEnumerator UpdateCoroutine(bool isUserDataManagerUpdate)
    {
        // 폼 생성
        WWWForm form = MakeForm("search_all", PlayerID);

        string url = SecureURLHandler.GetURL();
        // using문을 사용하여 메모리 누수를 해결
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            // 에러가 발생했을 경우
            if (www.isNetworkError || www.isHttpError)
            {
                GFunc.LogWarning(www.downloadHandler.text);
            }

            // 정상일 경우
            else
            {
                // 가져온 데이터를 파싱한다.
                List<PlayerData> playerDataList =
                    ConvertDataToPlayerData(www.downloadHandler.text);

                // 파싱된 데이터를 PlayerDataManager에 넣는다.
                AssignPlayerDataToManager(playerDataList);
                // isUserDataManagerUpdate == true
                if (isUserDataManagerUpdate)
                {
                    UserDataManager.Instance.GetDataFromDB();
                }

                // 디버그
                DebugData();
            }

            // using문을 사용해도 메모리 누수가 발생하여
            // 추가로 Dipose()함수를 호출해서 할당 해제
            www.Dispose();
        }
    }

    // 단일 칼럼을 가져오기 위한 코루틴
    private static IEnumerator SearchTutorialCoroutine(string command = "search", string column = "tutorial")
    {
        // 폼 생성
        WWWForm form = MakeForm(command, PlayerID, column);

        string url = SecureURLHandler.GetURL();
        // using문을 사용하여 메모리 누수를 해결
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            // 에러가 발생했을 경우
            if (www.isNetworkError || www.isHttpError)
            {
                GFunc.LogWarning(www.downloadHandler.text);
            }

            // 정상일 경우
            else
            {
                // 가져온 데이터를 출력
                GFunc.Log(www.downloadHandler.text);
                List<PlayerData> playerDataList = 
                    ConvertDataToPlayerData(www.downloadHandler.text);

                // 파싱된 데이터를 PlayerDataManager에 넣는다.
                UpdateTutorialData(playerDataList);

                GFunc.Log($"tutorial: {_tutorial}");
                

            }

            // using문을 사용해도 메모리 누수가 발생하여
            // 추가로 Dipose()함수를 호출해서 할당 해제
            www.Dispose();
        }
    }

    // DB에서 데이터를 저장하기 위한 비동기 코루틴
    private static IEnumerator SaveCoroutine(string column, string value,
        bool isUpdate)
    {

        // 폼 생성
        WWWForm form = MakeForm("add", PlayerID, column, value);

        string url = SecureURLHandler.GetURL();
        // using문을 사용하여 메모리 누수를 해결
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            // 에러가 발생했을 경우
            if (www.isNetworkError || www.isHttpError)
            {
                GFunc.LogWarning(www.downloadHandler.text);
            }

            // 정상적으로 처리되었을 경우
            else
            {
                GFunc.Log("정상적으로 DB에 데이터가 저장되었습니다.");

                // 업데이트 요청을 받았을 경우
                if (isUpdate)
                {
                    Update();
                }
            }

            // using문을 사용해도 메모리 누수가 발생하여
            // 추가로 Dipose()함수를 호출해서 할당 해제
            www.Dispose();
        }
    }


    /*************************************************
     *                    Debugs
     *************************************************/
    public static void DebugData()
    {
        GFunc.Log("DebugData");
        GFunc.Log($"id: {PlayerDataManager.PlayerID}, hp: {PlayerDataManager.HP}, gold: {PlayerDataManager.Gold}, " +
            $"exp: {PlayerDataManager.Exp}, gold_increase: {PlayerDataManager.GoldIncrease}, exp_increase: {PlayerDataManager.ExpIncrease}, " +
            $"weapon_atk: {PlayerDataManager.WeaponAtk}, weapon_cri_rate: {PlayerDataManager.WeaponCriRate}, weapon_cri_damage: {PlayerDataManager.WeaponCriDamage}, " +
            $"weapon_atk_rate: {PlayerDataManager.WeaponAtkRate}, weapon_exp: {PlayerDataManager.WeaponExp}, " +
            $"skill_level_1_1: {PlayerDataManager.SkillLevel1_1},  skill_level_1_2: {PlayerDataManager.SkillLevel1_2}," +
            $"skill_level_2_1: {PlayerDataManager.SkillLevel2_1}, skill_level_2_2: {PlayerDataManager.SkillLevel2_2},skill_level_2_3: {PlayerDataManager.SkillLevel2_3}," +
            $"skill_level_3: {PlayerDataManager.SkillLevel3}, " +
            $"skill_level_4_1: {PlayerDataManager.SkillLevel4_1}, skill_level_4_2: {PlayerDataManager.SkillLevel4_2}, skill_level_4_3: {PlayerDataManager.SkillLevel4_3}," +
            $"quest_main: {PlayerDataManager.QuestMain}, " + $"clear_count: {PlayerDataManager.ClearCount}, clear_mbti_value: {PlayerDataManager.ClearMBTIValue}, clear_time: {PlayerDataManager.ClearTime}," +
            $"tutorial: { PlayerDataManager.Tutorial}");
    }


}
