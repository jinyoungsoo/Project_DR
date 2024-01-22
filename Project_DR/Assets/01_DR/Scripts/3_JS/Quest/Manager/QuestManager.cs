using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rito.InventorySystem;
using System.Linq;

namespace Js.Quest
{
    /*************************************************
     *                    Classes
     *************************************************/
    [System.Serializable]
    public class QuestSaveData
    {
        public int id;              // 퀘스트 ID
        public int type;            // 퀘스트 Type
        public int currentValue;    // 현재 퀘스트 달성 값
        public int currentState;    // 현재 퀘스트 상태{[시작불가] -> [시작가능] -> [진행중] -> [완료가능] -> [완료] | [실패]}
    }

    [System.Serializable]
    public class QuestSaveDatas
    {
        public List<QuestSaveData> list = new List<QuestSaveData>();
    }

    public class QuestManager : MonoBehaviour
    {
        /*************************************************
         *                 Public Fields
         *************************************************/
        #region 싱글톤 패턴
        private static QuestManager m_Instance = null; // 싱글톤이 할당될 static 변수    

        public static QuestManager Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = FindObjectOfType<QuestManager>();
                if (m_Instance == null)
                {
                    GameObject obj = new GameObject("QuestManager");
                    m_Instance = obj.AddComponent<QuestManager>();
                    DontDestroyOnLoad(obj);
                }
                return m_Instance;
            }
        }
        #endregion
        public Item[] InventoryItems => UserDataManager.items;                               // 보유 인벤토리 아이템
        public List<Quest> QuestList => UserDataManager.QuestList;                           // 보유 퀘스트 리스트
        public Dictionary<int, Quest> QuestDictionary => UserDataManager.QuestDictionary;    // 보유 퀘스트 딕셔너리

        public Dictionary<int, List<Quest>> KeyIDQuestDictionary => UserDataManager.KeyIDQuestDictionary;    // NPC ID가 키 값인 딕셔너리

        public const int QUEST_FIRST_ID = 31_1_1_001;                                        // 퀘스트 테이블 시작 ID


        /*************************************************
         *                 Private Fields
         *************************************************/
        [SerializeField] private List<Quest> _debugQuestList;                                // 디버그용 퀘스트 리스트 
        [SerializeField] private QuestSaveDatas _mainQuestSaveDatas = new QuestSaveDatas();  // 저장용 퀘스트 데이터 리스트
                                                                                             // 클래스 통째로 직렬화 해야된다.

        /*************************************************
         *                  Unity Events
         *************************************************/
        void Start()
        {
            // QuestCallback에 메서드 등록
            AddQuestCallbacks();
        }

        // 디버그
        //private void Update()
        //{
        //    // DB 저장 테스트
        //    if (Input.GetKey(KeyCode.Q))
        //    {
        //        SaveQuestDataToDB();
        //    }

        //    // DB 호출 테스트
        //    if (Input.GetKey(KeyCode.W))
        //    {
        //        LoadUserQuestDataFromDB();
        //    }
        //}
        // 디버그


        /*************************************************
         *             Init Public Methods
         *************************************************/
        // 데이터 테이블에 있는 퀘스트를 가져와서 생성
        public void CreateQuestFromDataTable()
        {
            //GFunc.Log("CreateQuestFromDataTable()");

            // 퀘스트 리스트를 초기화
            UserDataManager.ResetQuestList();

            List<int> idTable = DataManager.Instance.GetDataTableIDs(QUEST_FIRST_ID);
            for (int i = 0; i < idTable.Count; i++)
            {
                // 퀘스트 생성 및 저장
                Quest quest = new Quest(idTable[i]);
                UserDataManager.AddQuestToQuestList(quest);

                //GFunc.Log($"퀘스트 [{idTable[i]}] 생성 완료");
            }

            // 디버그
            _debugQuestList = QuestList;

            // UserDataManager.questDictionary 할당
            UserDataManager.AddQuestDictionary();

            // UserDataManager.keyIDQuestDictionary 할당
            UserDataManager.AddKeyIDQuestDictionary();

            // 디버그 퀘스트 리스트 생성
            CreateQuestListDebug();
        }


        /*************************************************
         *             Public Quest Methods
         *************************************************/
        // 퀘스트를 생성한다
        // 같은 ID의 퀘스트 중복 조심
        public void CreateQuest(int id)
        {
            Quest quest = new Quest(id);
            UserDataManager.AddQuestToQuestList(quest);
        }

        // ID로 퀘스트를 찾아 초기 상태로 리셋한다.
        // 현재 진행 값도 초기화 된다.
        public void ResetQuest(int id)
        {
            GetQuestByID(id).ResetQuest();
        }

        // 퀘스트 상태 변경[시작불가] -> [시작가능]
        // 단 선행퀘스트 조건을 충족해야 변경된다.
        public void UpdateQuestStatesToCanStartable()
        {
            GFunc.Log("UpdateQuestStatesToCanStartable()");
            foreach (var item in QuestList)
            {
                // 상태가 [시작불가]일 경우
                if (item.QuestState.State.Equals(QuestState.StateQuest.NOT_STARTABLE))
                {
                    // [시작가능]으로 상태 변경 시도
                    item.ChangeToNextState();
                }
            }
        }

        // 퀘스트 상태 변경[시작가능] -> [시작불가]
        // 선행 퀘스트 초기화로 [시작불가]로 변경해야 할 경우 변경한다.
        public void UpdateQuestStatesToNotStartable()
        {
            foreach (var item in QuestList)
            {
                // 상태가 [시작가능]일 경우
                // && [시작가능] 조건을 충족하지 못할 경우
                if (item.QuestState.State.Equals(QuestState.StateQuest.CAN_STARTABLE)
                    && item.QuestHandler.CheckStateForCanStartable().Equals(false))
                {
                    // 상태를 [시작불가]로 변경
                    item.ChangeState(0);
                }
            }
        }

        // 퀘스트 ID로 딕셔너리에서 퀘스트를 검색하고 반환
        public Quest GetQuestByID(int id)
        {
            // 딕셔너리가 비어있을 경우 / 예외 처리
            if (QuestDictionary.Count.Equals(0))
            {
                GFunc.LogWarning("QuestManager.GetQuestByID(): 퀘스트 ID 검색 및 호출 실패! / " +
                    "퀘스트가 생성되기 전에 딕셔너리를 호출한 것 같습니다.");
                return default; 
            }

            Quest quest = QuestDictionary[id];
            return quest;
        }

        // 퀘스트 Key ID로 딕셔너리에서 퀘스트 리스트를 검색하고 반환
        public List<Quest> GetQuestListByKeyID(int id)
        {
            // 딕셔너리가 비어있을 경우 / 예외 처리
            if (KeyIDQuestDictionary[id].Count.Equals(0))
            {
                GFunc.LogWarning("QuestManager.GetQuestByKeyID(): 퀘스트 Key ID 검색 및 호출 실패! / " +
                    "퀘스트가 생성되기 전에 딕셔너리를 호출한 것 같습니다.");
                return default;
            }

            List<Quest> quest = KeyIDQuestDictionary[id];
            return quest;
        }

        // 인덱스로 퀘스트를 검색하고 반환
        public Quest GetQuestByIndex(int index)
        {
            Quest quest = QuestList[index];
            return quest;
        }

        // 특정 타입의 퀘스트를 List<Quest>로 반환한다 
        public List<Quest> GetQuestsOfType(int type)
        {
            List<Quest> questList = new List<Quest>();

            // QuestList 순회
            QuestData.QuestType questType = (QuestData.QuestType)type;
            foreach (var item in QuestList)
            {
                // type이 일치할 경우
                if (item.QuestData.Type.Equals(questType))
                {
                    // questList에 퀘스트 추가
                    questList.Add(item);
                }
            }

            return questList;
        }

        // 특정한 상태에 해당하는 모든 퀘스트를 리스트로 가져온다
        public List<Quest> GetQuestsByStatus(int state)
        {
            List<Quest> questList = new List<Quest>();

            // QuestList 순회
            QuestState.StateQuest questState = (QuestState.StateQuest)state;
            foreach (var item in QuestList)
            {
                // 상태가 일치할 경우
                if (item.QuestState.State.Equals(questState))
                {
                    // questList에 퀘스트 추가
                    questList.Add(item);
                }
            }

            return questList;
        }

        // 받은 Quest List에서 특정한 상태를 가진 퀘스트만 따로 추출한다
        public List<Quest> GetQuestsByStatusFromQuestList(List<Quest> questList, int state)
        {
            List<Quest> tempQuestList = new List<Quest>();

            // questList 순회
            QuestState.StateQuest questState = (QuestState.StateQuest)state;
            foreach (var item in questList)
            {
                // 상태가 일치할 경우
                if (item.QuestState.State.Equals(questState))
                {
                    // tempQuestList에 퀘스트 추가
                    tempQuestList.Add(item);

                    //GFunc.LogError($"{questState} / {tempQuestList[0].QuestData.ID}");
                }
            }

            return tempQuestList;
        }

        // 특정 타입 퀘스트의 전체 Count를 반환한다
        public int GetQuestCountOfType(int type)
        {
            int count = GetQuestsOfType(type).Count;
            return count;
        }


        /*************************************************
         *               Public DB Methods
         *************************************************/
        // 퀘스트 데이터를 DB에 저장한다
        public void SaveQuestDataToDB()
        {
            // 보유한 퀘스트의 데이터를 _mainQuestSaveDatas에 추가
            SerializeQuestDatas();

            // _mainQuestSaveDatas에 있는 데이터를 직렬화
            // && 직렬화된 데이터를 DB에 저장
            UserDataManager.Instance.SaveQuestDatasToDB(SerializeQuestSaveDataList());
        }

        // PlayerDataManager에 있는 정보로 퀘스트 목록을 업데이트 한다.
        public void UpdateUserQuestData(bool isDecoding = true)
        {
            // json으로 변환된 string은 .NET Framework 디코딩이 필요
            string json = isDecoding.Equals(true) ? 
                System.Web.HttpUtility.UrlDecode(PlayerDataManager.QuestMain) : PlayerDataManager.QuestMain;

            //GFunc.Log($"디코딩된 데이터: {json}");

            // 데이터가 비어있을 경우 예외처리
            // 퀘스트 전체 상태만 업데이트 [시작불가] -> [시작가능]
            if (! json.Equals(""))
            {
                QuestSaveDatas questSaveDatas = JsonUtility.FromJson<QuestSaveDatas>(json);

                // QuestSaveDatas에 있는 데이터를 UserDataManager의 퀘스트 목록으로 전달
                // 보유한 퀘스트의 상태 와 진행 값을 변경한다.
                UpdateUserDataFromQuestSaveDatas(questSaveDatas);
            }

            // 퀘스트의 상태를 업데이트 한다
            // 조건 충족시 [시작불가] -> [시작가능]으로 변경
            UpdateQuestStatesToCanStartable();
        }

        // DB에서 퀘스트 정보를 가져와서 UserDataManager와
        // 퀘스트 목록을 업데이트 한다.
        public IEnumerator LoadUserQuestDataCoroutine(float t)
        {
            yield return new WaitForSeconds(t);
            PlayerDataManager.Update();
            UpdateUserQuestData();
        }


        /*************************************************
         *                Private Methods
         *************************************************/
        // QuestCallback에 메서드 등록
        private void AddQuestCallbacks()
        {
            ////TODO: 해당하는 클래스들의 메서드에서 온콜백 호출되게 해야함
            QuestCallback.QuestDataCallback += UpdateQuestStatesToCanStartable;     // DB에서 퀘스트 정보를 가져왔을 때 or 퀘스트가 완료되었을 때
            QuestCallback.BossMeetCallback += UpdateQuests;                         // 보스 조우
            QuestCallback.BossKillCallback += UpdateQuests;                         // 보스 킬
            QuestCallback.UseItemCallback += UpdateQuests;                          // 아이템 사용
            QuestCallback.MonsterKillCallback += UpdateQuests;                      // 몬스터 처치
            QuestCallback.CraftingCallback += UpdateQuests;                         // 크래프팅
            QuestCallback.ObjectCallback += UpdateQuests;                           // 오브젝트
            QuestCallback.InventoryCallback += UpdateQuests;                        // 인벤토리(증정): 디버깅 완료
            QuestCallback.DialogueCallback += UpdateQuests;                         // NPC와 대화    
        ////TODO: 해당하는 클래스들의 메서드에서 온콜백 호출되게 해야함
        }

        // 보유한 퀘스트의 데이터를 QuestSaveData에 저장하고
        // _questSaveDatas에 추가한다.
        private void SerializeQuestDatas()
        {
            foreach (var item in QuestList)
            {
                // 퀘스트 타입이 [메인퀘스트]일 경우
                // && '실패' 상태가 아닐 경우
                if (item.QuestData.Type.Equals(QuestData.QuestType.MAIN)
                    && (! item.QuestState.State.Equals(QuestState.StateQuest.FAILED)))
                {
                    // QuestSaveData 생성 및 _mainQuestSaveDatas에 추가
                    QuestSaveData questSaveData = new QuestSaveData();
                    questSaveData.id = item.QuestData.ID;
                    questSaveData.type = (int)item.QuestData.Type;
                    questSaveData.currentValue = item.QuestData.CurrentValue;
                    questSaveData.currentState = (int)item.QuestState.State;
                    _mainQuestSaveDatas.list.Add(questSaveData);
                }
            }
        }

        // 퀘스트 데이터가 업데이트 되었을 때 퀘스트 직렬화



        // _questSaveDatas에 있는 데이터를 직렬화
        private string SerializeQuestSaveDataList()
        {
            string json = JsonUtility.ToJson(_mainQuestSaveDatas);
            GFunc.Log($"직렬화된 _mainQuestSaveDatas: {json}");

            return json;
        }

        // QuestSaveDatas에 있는 데이터를 UserDataManager로 전달
        // 보유한 퀘스트의 상태 와 진행 값을 변경한다.
        private void UpdateUserDataFromQuestSaveDatas(QuestSaveDatas questSaveDatas)
        {
            // Dictionary를 사용하여 ID를 Key로 하는 퀘스트 맵을 생성
            Dictionary<int, Quest> questMap = QuestList.ToDictionary(
                quest => quest.QuestData.ID, quest => quest);

            // QuestSaveData 리스트 순회
            foreach (var item in questSaveDatas.list)
            {
                // questSaveDatas에 있는 ID와 questMap(QuestList)의 ID가 일치할 경우
                if (questMap.TryGetValue(item.id, out var quest))
                {
                    // 상태 및 현재 진행 값 변경
                    quest.QuestState.ChangeState((QuestState.StateQuest)item.currentState);
                    quest.ChangeCurrentValue(item.currentValue);
                }
            }
        }

        // 퀘스트 업데이트
        private void UpdateQuests(int id, int condition)
        {
            QuestData.ConditionType conditionType = (QuestData.ConditionType)condition;
            // 조건에 해당하는 퀘스트를 보유했는지 검사
            // 해당하는 퀘스트가 없을 경우
            if (IsQuestConditionFulfilled(conditionType).Equals(false)) 
            { GFunc.Log($"조건[{conditionType}]에 해당하는 진행중인 퀘스트가 없습니다."); return; }

            int itemCount = default;
            // conditionType이 [7] 증정일 경우
            if (conditionType.Equals(QuestData.ConditionType.GIVE_ITEM))
{
                // 일치하는 아이템의 갯수를 가져옴
                itemCount = GetItemCountByID(id);
            }

            // 보유한 퀘스트 리스트를 순회해서 값 변경
            foreach (var item in QuestList)
            {
                // 퀘스트의 conditionType(조건)이 일치할 경우
                // {[1]=보스조우, [2]=보스처치, [3]=소비, [4]=처치, [5]=크래프팅, [6]=오브젝트, [7]=증정, [8]대화}
                // && 해당하는 퀘스트의 상태가 '진행중'일 경우
                if (item.QuestData.Condition.Equals(conditionType)
                    && item.QuestState.State.Equals(QuestState.StateQuest.IN_PROGRESS))
                {
                    // id와 퀘스트 키ID가 일치할 경우
                    if (item.QuestData.KeyID.Equals(id))
                    {
                        // condition이 [7] 증정일 경우
                        if (item.QuestData.Condition.Equals(QuestData.ConditionType.GIVE_ITEM))
                        {
                            // 보유한 아이템의 갯수로 값 변경
                            item.ChangeCurrentValue(itemCount);
                        }

                        // condition이 [7]이 아니라면
                        else
                        {
                            // 값 증가 += 1
                            item.AddCurrentValue();
                        }

                        // 조건 충족시 [3][완료 가능]으로 상태 변경
                        item.ChangeToNextState();
                    }
                }

                // [7] 증정 예외처리
                // [완료가능] 상태에서 보유 갯수가 줄어들어 현재 값이 목표 값 보다
                // 낮아질 경우 [진행중]으로 변경한다.
                if (itemCount.Equals(default).Equals(false)
                    && item.QuestState.State.Equals(QuestState.StateQuest.CAN_COMPLETE)
                    && item.QuestData.CurrentValue < item.QuestData.ClearValue)
                {
                    GFunc.Log("item");
                    // 현재 값과 상태를 [2][진행중]으로 변경
                    item.ChangeCurrentValue(itemCount);
                    item.ChangeState(2);
                }
            }
        }

        // 조건에 해당하는 퀘스트를 보유했는지 검사
        private bool IsQuestConditionFulfilled(QuestData.ConditionType condition)
        {
            // 퀘스트 
            foreach (var item in QuestList)
            {
                // 퀘스트의 condition(조건)에 해당하는 퀘스트가 있을 경우
                // && 해당 퀘스트의 상태가 '진행중'일 경우
                if (item.QuestData.Condition.Equals(condition)
                    && item.QuestState.State.Equals(QuestState.StateQuest.IN_PROGRESS))
                {
                    return true;
                }
            }

            // 없을 경우
            return false;
        }

        // 인벤토리에 있는 id에 일치하는 아이템의 갯수를 검색한다
        private int GetItemCountByID(int id)
        {
            int itemCount = default;
            // 최대 아이템 갯수를 보유할 경우 return을 안하고 추가로 순회
            for (int i = 0; i < InventoryItems.Length; i++)
            {
                // 아이템의 ID가 일치할 경우 / null 예외처리
                // && 아이템이 카운트 가능한 아이템일 경우
                if ((InventoryItems[i]?.Data.ID.Equals(id)).Equals(true)
                    && InventoryItems[i] is CountableItem ci)
                {
                    // itemCount에 보유 갯수 저장
                    itemCount += ci.Amount;
                    // 해당하는 아이템의 갯수가 최대 갯수가 아닐 경우
                    if (IsItemAtMaximumCount(ci).Equals(false))
                    {
                        GFunc.Log("IsItemAtMaximumCount(ci).Equals(false)");

                        // 해당하는 아이템의 갯수를 반환
                        return itemCount;
                    }
                }
            }

            // 찾지 못하거나 그 외의 경우
            return itemCount;
        }

        // 보유한 아이템이 최대 갯수인지 확인한다
        private bool IsItemAtMaximumCount(CountableItem item)
        {
            // 아이템이 최대 갯수일 경우
            if (item.Amount.Equals(item.MaxAmount))
            {
                return true;
            }

            // 아닐 경우
            return false;
        }

        // 디버그용 퀘스트 리스트 생성
        // type은 표시할 퀘스트 타입이다.
        private void CreateQuestListDebug()
        {
            // 이미 생성되 있을 경우 삭제 후 재생성한다.
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Destroy(child.gameObject);
            }
            GameObject obj = new GameObject("QuestListDebug");
            obj.transform.SetParent(transform);
            obj.AddComponent<QuestListDebug>().Initialize();
        }
    }
}
