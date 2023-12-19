using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rito.InventorySystem;

namespace Js.Quest
{
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

        public Item[] InventoryItems => UserDataManager.items;              // 보유 인벤토리 아이템
        public List<Quest> QuestList => UserDataManager.quests;             // 보유 퀘스트 리스트


        /*************************************************
         *                 Private Fields
         *************************************************/
        private const int QUEST_FIRST_ID = 1_000_000_1;
        [SerializeField] private List<Quest> _debugQuestList;               // 디버그용 퀘스트 리스트 

        /*************************************************
         *                  Unity Events
         *************************************************/
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
        }

        void Start()
        {
            // QuestCallback에 메서드 등록
            AddQuestCallbacks();

            // 데이터 테이블에 있는 퀘스트를 가져와서 생성
            CreateQuestFromDataTable();
        }


        /*************************************************
         *                Public Methods
         *************************************************/
        // 생성용 트리거
        public void Trigger()
        {
            GFunc.Log("Create Quest Manager");

            // 디버그
            _debugQuestList = QuestList;
        }

        // 데이터 테이블에 있는 퀘스트를 가져와서 생성
        public void CreateQuestFromDataTable()
        {
            int count = Data.GetCount(QUEST_FIRST_ID);
            for (int i = 0; i < count; i++)
            {
                // 퀘스트 생성 및 저장
                Quest quest = new Quest(QUEST_FIRST_ID + i);
                UserDataManager.quests.Add(quest);

                GFunc.Log($"퀘스트 [{QUEST_FIRST_ID + i}] 생성 완료");
            }

            // 디버그
            _debugQuestList = QuestList;
        }

        // 퀘스트를 생성한다
        public void CreateQuest(int id)
        {
            Quest quest = new Quest(id);
            UserDataManager.quests.Add(quest);
        }

        // 퀘스트를 삭제한다
        public void RemoveQuest(int index)
        {
            UserDataManager.RemoveQuest(index);
        }


        /*************************************************
         *                Private Methods
         *************************************************/
        // QuestCallback에 메서드 등록
        private void AddQuestCallbacks()
        {
            ////TODO: 해당하는 클래스들의 메서드에서 온콜백 호출되게 해야함
            QuestCallback.QuestDataCallback += ChangeQuestStates;   // DB에서 퀘스트 정보를 가져왔을 때 or 퀘스트가 완료되었을 때
            QuestCallback.BossMeetCallback += UpdateQuests;         // 보스 조우
            QuestCallback.BossKillCallback += UpdateQuests;         // 보스 킬
            QuestCallback.UseItemCallback += UpdateQuests;          // 아이템 사용
            QuestCallback.MonsterKillCallback += UpdateQuests;      // 몬스터 처치
            QuestCallback.CraftingCallback += UpdateQuests;         // 크래프팅
            QuestCallback.ObjectCallback += UpdateQuests;           // 오브젝트
            QuestCallback.InventoryCallback += UpdateQuests;        // 인벤토리(증정): 디버깅 완료
            QuestCallback.DialogueCallback += UpdateQuests;         // NPC와 대화
        ////TODO: 해당하는 클래스들의 메서드에서 온콜백 호출되게 해야함
        }

        // 퀘스트 상태 변경[시작불가] -> [시작가능]
        // 단 선행퀘스트 조건을 충족해야 변경된다.
        private void ChangeQuestStates()
        {
            GFunc.Log("OnQuestDataCallback()");
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

        // 퀘스트 업데이트
        private void UpdateQuests(int id, int condition)
        {
            // 조건에 해당하는 퀘스트를 보유했는지 검사
            // 해당하는 퀘스트가 없을 경우
            if (IsQuestConditionFulfilled(condition).Equals(false)) { GFunc.Log($"해당하는 퀘스트{condition}가 없습니다."); return; }

            int itemCount = default;
            // condition이 [7] 증정일 경우
            if (condition.Equals(7))
{
                // 일치하는 아이템의 갯수를 가져옴
                itemCount = GetItemCountByID(id);
            }

            // 보유한 퀘스트 리스트를 순회해서 값 변경
            foreach (var item in QuestList)
            {
                // 퀘스트의 condition(조건)이 일치할 경우
                // {[1]=보스조우, [2]=보스처치, [3]=소비, [4]=처치, [5]=크래프팅, [6]=오브젝트, [7]=증정, [8]대화}
                // && 해당하는 퀘스트의 상태가 '진행중'일 경우
                if (item.QuestData.Condition.Equals(condition)
                    && item.QuestState.State.Equals(QuestState.StateQuest.IN_PROGRESS))
                {
                    // id와 퀘스트 키ID가 일치할 경우
                    if (item.QuestData.KeyID.Equals(id))
                    {
                        // condition이 [7] 증정일 경우
                        if (item.QuestData.Condition.Equals(7))
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
                    }
                }
            }
        }

        // 조건에 해당하는 퀘스트를 보유했는지 검사
        private bool IsQuestConditionFulfilled(int condition)
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
    }
}
