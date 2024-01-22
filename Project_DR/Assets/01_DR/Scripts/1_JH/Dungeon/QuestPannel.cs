using Js.Quest;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestPannelItem : MonoBehaviour
{
    public TMP_Text category;       // 퀘스트 카테고리
    public TMP_Text questName;      // 퀘스트 명
    public TMP_Text achievement;    // 퀘스트 달성도

    // 퀘스트 초기화
    public void QuestInit()
    {
        category = transform.GetChild(0).GetComponent<TMP_Text>();
        questName = transform.GetChild(1).GetComponent<TMP_Text>();
        achievement = transform.GetChild(2).GetComponent<TMP_Text>();
    }
    
    // 카테고리 UI 업데이트
    public void SetCategory(string text)
    {
        category.text = text;
    }
    // 퀘스트명 UI 업데이트
    public void SetQuestName(string text)
    {
        questName.text = text;
    }
    // 퀘스트 진행도 업데이트
    public void SetAchievement(string text)
    {
        achievement.text = text;
    }

}

public class QuestPannel : MonoBehaviour
{
    private GameObject questItem;
    private Transform contentParent;
    public Dictionary<Quest, GameObject> QuestList = new Dictionary<Quest, GameObject>();
    private Vector3 offSize = new Vector3(0.000001f, 0.000001f, 0.000001f);


    private void OnDisable()
    {
        QuestCallback.SubspecialQuestProgressCallback -= AddQuest;
        QuestCallback.SubspecialQuestValueChangedCallback -= UpdateQuest;
        QuestCallback.SubspecialQuestCompletedCallback -= RemoveQuest;
    }

    private void Start()
    {
        QuestCallback.SubspecialQuestProgressCallback += AddQuest;
        QuestCallback.SubspecialQuestValueChangedCallback += UpdateQuest;
        QuestCallback.SubspecialQuestCompletedCallback += RemoveQuest;

        transform.parent.localScale = offSize;
        // 퀘스트 아이템 및 부모 지정
        questItem = transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).gameObject;
        contentParent = transform.GetChild(1).GetChild(0).GetChild(0).transform;

        // 퀘스트 아이템 컴포넌트 추가 및 초기화
        questItem.AddComponent<QuestPannelItem>();
        questItem.GetComponent<QuestPannelItem>().QuestInit();
        questItem.SetActive(false);

        //퀘스트 리스트 초기화
        QuestList.Clear();

        // 만약 진행중 퀘스트 정보가 있다면 초기화
        SetProgressQuest();
    }

    /// <summary> 퀘스트를 패널에 추가하는 메서드  </summary>
    public void AddQuest(Quest quest)
    {
        transform.parent.localScale = Vector3.one;

        // 패널 내의 새 퀘스트 오브젝트 생성
        GameObject newQuestObj = Instantiate(questItem, contentParent);
        newQuestObj.SetActive(true);
        
        // 오브젝트에 퀘스트 정보 세팅
        QuestPannelItem item = newQuestObj.GetComponent<QuestPannelItem>();
        int id = quest.QuestData.ID;
        int typeNum = Data.GetInt(id, "Type");
        string type = default;
        if (typeNum == 2)
        {
            type = "서브";
        }
        else if (typeNum == 3)
        {
            type = "특수";
        }
        else
            type = "ERROR";

        item.SetCategory(type);
        item.SetQuestName(Data.GetString(id, "Desc"));
        item.SetAchievement(quest.QuestData.CurrentValue.ToString());

        // 딕셔너리에 퀘스트 추가
        QuestList.Add(quest, newQuestObj);
    }
    /// <summary> 퀘스트를 업데이트하는 메서드  </summary>
    public void UpdateQuest(Quest quest)
    {
        GFunc.Log($"UpdateQuest(): {quest.QuestData.CurrentValue}");
        // 딕셔너리의 퀘스트 내용 업데이트
        QuestList[quest].GetComponent<QuestPannelItem>().SetAchievement(quest.QuestData.CurrentValue.ToString());
    }
    /// <summary> 퀘스트를 삭제하는 메서드  </summary>
    public void RemoveQuest(Quest quest) 
    {
        // 패널 내의 퀘스트 오브젝트 삭제 후 딕셔너리 제거
        Destroy(QuestList[quest]);
        QuestList.Remove(quest);

        if(QuestList.Count == 0)
        {
            transform.parent.localScale = offSize;
        }
    }

    /// <summary> 진행중인 퀘스트가 있을 경우 업데이트  </summary>
    public void SetProgressQuest()
    {
        // 서브 퀘스트 탐색
        List<Quest> subQuest = Unit.GetInProgressSubQuestForList();
        foreach (Quest sub in subQuest) 
        {
            AddQuest(sub);
        }

        // 특수 퀘스트 탐색
        List<Quest> specialQuest = Unit.GetInProgressSpecialQuestForList();
        foreach (Quest special in specialQuest)
        {
            AddQuest(special);
        }
    }
}
