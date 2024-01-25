using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Js.Quest;
using Js.Crafting;
using BNG;

public static class UserData
{
    #region #######################_GET_#######################

    public static StatData GetStat()
    {
        return UserDataManager.Instance.statData;
    }

    public static GameResult GetResult()
    {
        return UserDataManager.Instance.result;
    }
    #endregion

    #region #######################_골드_#######################

    /// <summary>골드를 획득하는 메서드 </summary>
    public static void AddGold(int value)
    {
        UserDataManager.Instance.Gold += value;
        AudioManager.Instance.PlaySFX("SFX_Item_Gold_Gain_01");
    }
    /// <summary>골드를 소모하는 메서드 </summary>
    public static void SpendGold(int value)
    {
        UserDataManager.Instance.Gold -= value;
    }
    #endregion

    #region #######################_EXP_#######################
    /// <summary>현재 경험치를 반환 </summary>
    public static int GetExp()
    {
        return UserDataManager.Instance.Exp;
    }
    /// <summary>경험치를 더해주는 메서드 </summary>
    public static void AddExp(int value)
    {
        UserDataManager.Instance.Exp += value;
    }
    /// <summary>경험치를 소모하는 메서드 </summary>
    public static void SpendExp(int value)
    {
        UserDataManager.Instance.Exp -= value;
    }
    #endregion

    #region #######################_몬스터_######################

    /// <summary>일반 몬스터 처치 시 획득하는 경험치와 EXP </summary>
    public static void KillMonster(int exp, int gold = 0)
    {
        UserDataManager.Instance.result.AddMonsterNormal(gold, exp);
    }
    /// <summary>엘리트 몬스터 처치 시 획득하는 경험치와 EXP </summary>
    public static void KillElite(int exp, int gold = 0)
    {
        UserDataManager.Instance.result.AddMonsterElite(gold, exp);
    }
    /// <summary>보스 몬스터 처치 시 획득하는 경험치와 EXP </summary>
    public static void KillBoss(int exp, int gold = 0)
    {
        UserDataManager.Instance.result.AddMonsterBoss(gold, exp);
    }
    #endregion

    #region ######################_결과보상_#####################

    /// <summary>퀘스트 보상을 결과에 추가하는 메서드 </summary>
    public static void AddQuestScore(Quest quest)
    {
        UserDataManager.Instance.result.AddQuestScore(quest);
    }   
    /// <summary>획득한 아이템을 결과에 추가하는 메서드 </summary>
    public static void AddItemScore(int id)
    {
        UserDataManager.Instance.result.AddItemScore(id);
    }

    /// <summary>획득한 모든 골드를 계산해주는 메서드 </summary>
    public static int GoldCalculator()
    {
        GameResult result = UserDataManager.Instance.result;

        int monsterGold = result.monster.normal.gold + result.monster.elite.gold + result.monster.boss.gold;

        int itemGold = 0;
        for (int i = 0; i < result.item.Count; i++)
        {
            itemGold += result.item[i].gold;
        }

        int questGold = 0;
        for (int i = 0; i < result.quest.Count; i++)
        {
            questGold += result.quest[i].gold;
        }

        int totalGold = monsterGold + itemGold + questGold;

        int additionalGold = Mathf.RoundToInt(totalGold * UserDataManager.Instance.GainGold);   // 추가 골드 더하기

        return totalGold + additionalGold;
    }

    /// <summary>획득한 모든 골드를 계산해주는 메서드 </summary>
    public static int ExpCalculator()
    {
        GameResult result = UserDataManager.Instance.result;

        int monsterExp = result.monster.normal.exp + result.monster.elite.exp + result.monster.boss.exp;

        int itemExp = 0;
        for (int i = 0; i < result.item.Count; i++)
        {
            itemExp += result.item[i].exp;
        }

        int questExp = 0;
        for (int i = 0; i < result.quest.Count; i++)
        {
            questExp += result.quest[i].exp;
        }

        int totalExp = monsterExp + itemExp + questExp;

        int additionalExp = Mathf.RoundToInt(totalExp * UserDataManager.Instance.GainExp);   // 추가 경험치 더하기

        return totalExp + additionalExp;
    }
    // 남은 재료 아이템을 계산해주는메서드
    public static void MaterialItemCalculator() 
    {
        List<(int, int)> itemList = Unit.GetInventoryMaterialItems();
        for (int i = 0; i < itemList.Count; i++)
        {
            int itemID = itemList[i].Item1;
            int itemAmount = itemList[i].Item2;
            //GFunc.Log($"ID: {itemID} / Amount: {itemAmount}");
            for (int j = 0; j < itemAmount; j++)
            {
                UserData.AddItemScore(itemID);
            }
        }
    }


    public static void ResetResult()
    {
        UserDataManager.Instance.result = new GameResult();
    }

    #endregion

    #region ####################_UserData_#####################

    /// <summary>업그레이드가 반영된 플레이어의 체력을 반환</summary>
    public static float GetMaxHP()
    {
        UserDataManager.Instance.MaxHP = UserDataManager.Instance.DefaultHP;
        if (UserDataManager.Instance.HPLv != 0)
        {
            UserDataManager.Instance.MaxHP = UserDataManager.Instance.DefaultHP + UserDataManager.Instance.statData.upgradeHp[UserDataManager.Instance.HPLv - 1].sum;
        }
        return UserDataManager.Instance.MaxHP + GetEffectMaxHP();
    }
    public static float GetEffectMaxHP()
    {
        return (UserDataManager.Instance.MaxHP * (UserDataManager.Instance.effectMaxHP / 100));
    }
    /// <summary>현재 플레이어의 체력을 반환</summary>
    public static float GetHP()
    {        
        return UserDataManager.Instance.CurHP;
    }
    
    public static void OnDamage(float damage)
    {
        UserDataManager.Instance.CurHP -= damage;
    }
    public static void SetCurHealth(float health)
    {
        UserDataManager.Instance.CurHP = health;
    }

    /// <summary> 플레이어의 현재 체력을 증감 </summary>
    public static void SetCurrentHealth(float amount)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player == null)
        {
            GFunc.Log("플레이어 찾을 수 없음");
            return; 
        }

        if(0 < amount)
        {
            player.GetComponent<PlayerHealth>().RestoreHealth(amount);
        }

        else if(amount < 0)
        {
            player.GetComponent<Damageable>().DealDamage(amount);
        }
    }

    // 해당 ID의 스킬을 호출한다.
    public static void ActiveSkill(int id, float _value = 0)
    {
        SkillManager.instance.ActiveSkill(id, value : _value);
    }

    #endregion

    #region ####################_DrillData_#####################


    /// <summary>업그레이드가 반영된 드릴의 기본 데미지를 반환</summary>
    public static float GetDrillDamage()
    {
        float damage = Data.GetFloat(1100, "Damage");
        if(UserDataManager.Instance.WeaponAtkLv != 0)
        {
            damage = damage + UserDataManager.Instance.statData.upgradeAtk[UserDataManager.Instance.WeaponAtkLv - 1].sum1;
        }

        return damage;  
    }
    /// <summary>업그레이드가 반영된 드릴의 회전 데미지를 반환</summary>
    public static float GetDrillSpinDamage()
    {
        float spinDamage = Data.GetFloat(1100, "DotDamage");
        if (UserDataManager.Instance.WeaponAtkLv != 0)
        {
            spinDamage = spinDamage + UserDataManager.Instance.statData.upgradeAtk[UserDataManager.Instance.WeaponAtkLv - 1].sum2;
        }
        return spinDamage;
    }
    /// <summary>업그레이드가 반영된 치명타 확률을 반환</summary>
    public static float GetCritChance()
    {
        float critChance = Data.GetFloat(1100, "CritChance");
        if (UserDataManager.Instance.WeaponCriRateLv != 0)
        {
            critChance = critChance + UserDataManager.Instance.statData.upgradeCrit[UserDataManager.Instance.WeaponCriRateLv - 1].sum1;
        }
        return critChance;
    }
    /// <summary>업그레이드가 반영된 치명타 배율을 반환</summary>
    public static float GetCritIncrease()
    {
        float critIncrease = Data.GetFloat(1100, "CritIncrease");
        if (UserDataManager.Instance.WeaponCriDamageLv != 0) 
        { 
            critIncrease = critIncrease + UserDataManager.Instance.statData.upgradeCritDmg[UserDataManager.Instance.WeaponCriDamageLv - 1].sum1;
        }
        return critIncrease;
    }
    /// <summary>업그레이드가 반영된 공격 속도를 반환</summary>
    public static float GetAttackSpeed()
    {
        float attackSpeed = Data.GetFloat(1100, "AttackSpeed");
        if (UserDataManager.Instance.WeaponAtkRateLv != 0)
        {
            attackSpeed = attackSpeed + UserDataManager.Instance.statData.upgradeAtkSpd[UserDataManager.Instance.WeaponAtkRateLv - 1].sum1;
        }
        return attackSpeed + UserDataManager.Instance.effectAttackRate;
    }
    /// <summary>업그레이드가 반영된 최대 드릴 회전 속도를 반환</summary>
    public static float GetMaxSpin()
    {
        float maxSpin = Data.GetFloat(1100, "MaxSpiralSpeed");
        if (UserDataManager.Instance.WeaponAtkRateLv != 0)
        {
            maxSpin = maxSpin + UserDataManager.Instance.statData.upgradeAtkSpd[UserDataManager.Instance.WeaponAtkRateLv - 1].sum2;
        }
        return maxSpin;
    }
    /// <summary>업그레이드가 반영된 최대 드릴 회전 속도를 반환</summary>
    public static float GetSpinForce()
    {
        float spinForce = Data.GetFloat(1100, "SpiralForce");
        if (UserDataManager.Instance.WeaponAtkRateLv != 0)
        {
            spinForce = spinForce + UserDataManager.Instance.statData.upgradeAtkSpd[UserDataManager.Instance.WeaponAtkRateLv - 1].sum3;
        }
        return spinForce;
    }

    public static float GetEffectDamage()
    {
        return UserDataManager.Instance.effectDamage;
    }

    public static float GetEffectCritDamage()
    {
        return UserDataManager.Instance.effectCritDamage;
    }

    public static float GetEffectDrillSize()
    {
        return UserDataManager.Instance.effectDrillSize;
    }
    public static void EffectMaxHP(float value)
    {
        UserDataManager.Instance.effectMaxHP += value;
    }
    #endregion

    #region ####################_Skill_1_Data_#####################

    /// <summary>업그레이드가 반영된 테라드릴의 데미지를 반환</summary>
    public static float GetTeraIncrease()
    {
        float teraIncrease = Data.GetFloat(721100, "Value1");
        if (UserDataManager.Instance.Skill1Lv_1 != 0)
        {
            teraIncrease = teraIncrease + UserDataManager.Instance.statData.upgradeSkill1[UserDataManager.Instance.Skill1Lv_1 - 1].sum1;
        }
        return teraIncrease;
    }
    /// <summary>업그레이드가 반영된 테라드릴의 사이즈를 반환</summary>
    public static float GetTeraDrillSize()
    {
        float teraSize = Data.GetFloat(721101, "Value1");
        if (UserDataManager.Instance.Skill1Lv_1 != 0)
        {
            teraSize = teraSize + UserDataManager.Instance.statData.upgradeSkill1[UserDataManager.Instance.Skill1Lv_1 - 1].sum2;
        }
        return teraSize;
    }
    /// <summary>업그레이드가 반영된 테라드릴의 지속시간를 반환</summary>
    public static float GetTeraCoolDown()
    {
        float coolDown = Data.GetFloat(721100, "Value2");
        if (UserDataManager.Instance.Skill1Lv_2 != 0)
        {
            coolDown = coolDown + UserDataManager.Instance.statData.upgradeSkill1[UserDataManager.Instance.Skill1Lv_2 - 1].sum3;
        }
        return coolDown;
    }
    #endregion

    #region ####################_Skill_2_Data_#####################
    /// <summary>업그레이드가 반영된 드릴연마의 치명타 데미지를 반환</summary>
    public static float GetGinderIncrease()
    {
        float grinderIncre = Data.GetFloat(721115, "Value1");
        if (UserDataManager.Instance.Skill2Lv_1 != 0)
        {
            grinderIncre = grinderIncre + UserDataManager.Instance.statData.upgradeSkill2[UserDataManager.Instance.Skill2Lv_1 - 1].sum1;
        }
        return grinderIncre;
    }  
    /// <summary>업그레이드가 반영된 드릴연마의 치명타 확률 반환</summary>
    public static float GetGrinderCritChance()
    {
        float grinderCrit = Data.GetFloat(721114, "Value1");
        if (UserDataManager.Instance.Skill2Lv_2 != 0)
        {
            grinderCrit = grinderCrit + UserDataManager.Instance.statData.upgradeSkill2[UserDataManager.Instance.Skill2Lv_2 - 1].sum2;
        }
        return grinderCrit;
    }
    /// <summary>업그레이드가 반영된 드릴연마의 지속시간 반환</summary>
    public static float GetGrinderMaxTime()
    {
        float grinderMaxTime = Data.GetFloat(721114, "Value4");
        if (UserDataManager.Instance.Skill2Lv_3 != 0)
        {
            grinderMaxTime = grinderMaxTime + UserDataManager.Instance.statData.upgradeSkill2[UserDataManager.Instance.Skill2Lv_3 - 1].sum3;
        }
        return grinderMaxTime;
    }
    #endregion

    #region ####################_Skill_3_Data_#####################

    /// <summary>인덱스 입력 시 업그레이드가 반영된 드릴 분쇄 증가값 반환</summary>
    public static float GetSmashDamage(int index)
    {
        float smashDebuff = 0;
        switch (index)
        {
            case 1:
                smashDebuff = GetSmashDamage_Stack1();
                break;
            case 2:
                smashDebuff = GetSmashDamage_Stack2();
                break;
            case 3:
                smashDebuff = GetSmashDamage_Stack3();
                break;
        }


        return smashDebuff; 
    }


    /// <summary>업그레이드가 반영된 드릴 분쇄의 1단계 증가값 반환</summary>
    public static float GetSmashDamage_Stack1()
    {
        float smashDamage1 = Data.GetFloat(722216, "Value1");
        if (UserDataManager.Instance.Skill3Lv != 0)
        {
            smashDamage1 = smashDamage1 + UserDataManager.Instance.statData.upgradeSkill3[UserDataManager.Instance.Skill3Lv - 1].sum1;
        }
        return smashDamage1;
    }

    /// <summary>업그레이드가 반영된 드릴 분쇄의 2단계 증가값 반환</summary>
    public static float GetSmashDamage_Stack2()
    {
        float smashDamage2 = Data.GetFloat(722216, "Value2");
        if (UserDataManager.Instance.Skill3Lv != 0)
        {
            smashDamage2 = smashDamage2 + UserDataManager.Instance.statData.upgradeSkill3[UserDataManager.Instance.Skill3Lv - 1].sum2;
        }
        return smashDamage2;
    }

    /// <summary>업그레이드가 반영된 드릴 분쇄의 3단계 증가값 반환</summary>
    public static float GetSmashDamage_Stack3()
    {
        float smashDamage3 = Data.GetFloat(722216, "Value3");
        if (UserDataManager.Instance.Skill3Lv != 0)
        {
            smashDamage3 = smashDamage3 + UserDataManager.Instance.statData.upgradeSkill3[UserDataManager.Instance.Skill3Lv - 1].sum3;
        }
        return smashDamage3;
    }
    #endregion

    #region ####################_Skill_4_Data_#####################

    /// <summary>업그레이드가 반영된 드릴 랜딩의 카운트</summary>
    public static int SetDrillLandingCount()
    {
        int landingCount = Data.GetInt(720217, "Value4");
        if (UserDataManager.Instance.Skill4Lv_1 != 0)
        {
            landingCount = Mathf.RoundToInt(landingCount + UserDataManager.Instance.statData.upgradeSkill4[UserDataManager.Instance.Skill4Lv_1 - 1].sum1);
        }
        return landingCount;
    }
    /// <summary>업그레이드가 반영된 드릴 랜딩의 치명타 데미지</summary>
    public static float GetLandingCritIncrease()
    {
        float landingIncrease = Data.GetFloat(720217, "Value2");
        if (UserDataManager.Instance.Skill4Lv_2 != 0)
        {
            landingIncrease = landingIncrease + UserDataManager.Instance.statData.upgradeSkill4[UserDataManager.Instance.Skill4Lv_2 - 1].sum2;
        }
        return landingIncrease;
    }
    /// <summary>업그레이드가 반영된 드릴 랜딩의 넉백 힘</summary>
    public static float GetLandingForce()
    {
        float landingForce = Data.GetFloat(720217, "Value3");
        if (UserDataManager.Instance.Skill4Lv_3 != 0)
        {
            landingForce = landingForce + UserDataManager.Instance.statData.upgradeSkill4[UserDataManager.Instance.Skill4Lv_3 - 1].sum3;
        }
        return landingForce;
    }



    /// <summary>현재 드릴 랜딩 스킬 사용 가능 횟수 요청 </summary>
    public static int GetDrillLandingCount() 
    {
        return UserDataManager.Instance.drillLandingCount;
    }
    /// <summary>랜딩 스킬 사용시 카운트 -1</summary>
    public static void ActiveLandingSkill()
    {
        UserDataManager.Instance.drillLandingCount--;
        if(UserDataManager.Instance.drillLandingCount < 0)
        {
            UserDataManager.Instance.drillLandingCount = 0;
        }
    }
    #endregion

    #region ####################_Quest_#####################

    public static string GetQuest()
    {
        return UserDataManager.Instance.QuestMain;
    }
    // 퀘스트 유무 확인
    public static bool QuestCheck()
    {
        return String.IsNullOrEmpty(UserDataManager.Instance.QuestMain);
    }
    #endregion

    #region ####################_GamePlay_#####################

    /// <summary>플레이어 데이터를 초기화 메서드. 다시 로비에 돌아올 때 실행됨</summary>
    public static void ResetPlayer()
    {
        // 리셋하면 게임 매니저 삭제
        if(GameManager.instance)
        {
            GameManager.instance.DestroyGameManager();
        }

        // DB에서 정보 불러오기 & 퀘스트 생성 & 업데이트 & 아이템 초기화
        Unit.UpdateDataFromDB();
        Unit.ResetInventory();


        UserDataManager.Instance.CurHP = UserDataManager.Instance.MaxHP;
        UserDataManager.Instance.drillLandingCount = SetDrillLandingCount();

        UserDataManager.Instance.effectCritDamage = 0;
        UserDataManager.Instance.effectCritProbability = 0;
        UserDataManager.Instance.effectDamage = 0;

        CraftingManager.Instance.Create();
    }
    public static bool ClearCheck()
    {

        return UserDataManager.Instance.isClear;
    }
    public static bool GameOverCheck()
    {

        return UserDataManager.Instance.isGameOver;
    }
    public static void GameOver()
    {
        MaterialItemCalculator();                            // 재료 아이템 정산
        Unit.SaveQuestDataToDB();
        UserDataManager.Instance.isGameOver = true;
    }
    // 클리어 던전
    public static void ClearDungeon()
    {
        MaterialItemCalculator();                            // 재료 아이템 정산
        Unit.SaveQuestDataToDB();
        UserDataManager.Instance.SaveClearData();
        UserDataManager.Instance.isClear = true;
    }
    #endregion


    /// <summary>유저 데이터를 요청하는 메서드. 데이터 요청 시, DB에서 데이터를 가져왔을 경우 Action을 실행시켜준다. </summary>
    /// <param name="action">데이터를 불러왔을 때 실행할 Action</param>
    public static void GetData(Action action)
    {
        UserDataManager.Instance.DBRequst(action);
    }
}
