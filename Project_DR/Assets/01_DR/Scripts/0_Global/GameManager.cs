using BNG;
using Js.Quest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Layer
{
    Default = 0,
    TransparentFX = 1,
    IgnoreRaycast = 2,
    // 3 == null,
    Water = 4,
    UI = 5,
    Grabbable = 9,
    Drill = 11,
    Monster = 12,
    MonsterWall = 13,
    Player = 14,
    MapObject = 19,
    BattleRoomFloor = 20
}       // Layer

public enum GameRound
{
    Prototype,      // 프로토타입 상태
    FirstTime,      // 1회차 (클리어 하지 못함)
    FirstAfter      // 다회차 (1회차 클리어)
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance
    {
        get
        {
            // 만약 싱글톤 변수에 아직 오브젝트가 할당되지 않았다면
            if (m_instance == null || m_instance == default)
            {
                // 생성 후 할당
                GameObject obj = new GameObject("GameManager");
                m_instance = obj.AddComponent<GameManager>();
            }

            // 싱글톤 오브젝트를 반환
            return m_instance;
        }
    }
    private static GameManager m_instance; // 싱글톤이 할당될 static 변수    

    public GameRound round;
    [Header("Player Object")]
    public GameObject player;
    private ScreenFader fader;
    private InputBridge input;
    private ScreenText screenText;

    [Header("Game Over")]
    public string gameoverText;
    public string gameoverScene;

    private string _playerID; // SetPlayerID(string id) 메서드로 설정함
    public string PlayerID => _playerID;


    [Header("Dungeon")]
    // ----------------------------------------------- SG ------------------------------------------------

    public bool IsProtoType;
    #region 변수들
    /// <summary>
    /// // 현재 몇층인지 알려줄 변수
    /// </summary>
    public int nowFloor;
    /// <summary>
    /// 현재 플레이어의 회차의따라서 아래 const변수값이 대입될것임
    /// </summary>
    public int isPlayerMaxFloor;
    /// <summary>
    /// 기본 딜레이 5이며 딜레이를 줄때마다 ++이 되면서 천천히 생성이 되도록 하는것을 의도한 변수
    /// </summary>
    public int spawnDelayFrame = 5;

    // 던전 진행 Max층을 알려줄 const int 변수
    public const int PROTOTYPE = 1;
    public const int FIRSTTIME = 3;
    public const int FIRSTAFTER = 5;

    [SerializeField]
    public static List<RandomRoom> isClearRoomList;       // 모든 방의 클리어 여부를 관리해줄 List    

    private bool allRoomClear = false;              // 모든 방이 클리어 됬다면 true가 될 변수

    private bool isClear = false;                   // 방의 클리어 여부에 따라 변수값이 변하고 문을 관리해줄것임
    public bool isGameOver;

    [Header("Boss Battle")]
    public bool isBossBattle = false;            // 보스와 싸우는지 여부 (전투중 게임오버 체크 : 보스 전투 퀘스트 실패 조건)
    private Vector3 cutSceneRoomPosition;
    private Vector3 playerPos;                  // 컷씬룸 이동 전 플레이어 포지션

    public List<GameObject> ghostObjList;       // 유령을 담아둘 리스트
    public List<NullRoom> nullRoomList;         // 빈방을 담아두는 리스트 (모루생성에 필요)
    public bool isInItGhost = false;
    public bool isAnvilCreate = false;

    #endregion 변수들

    public bool IsClear
    {
        get { return isClear; }
        set
        {
            if (isClear != value)
            {
                isClear = value;
                DoorControl(IsClear);
                if (isClear == true)
                {
                    allRoomClear = CheckAllRoomClear();
                }
                if (allRoomClear == true)
                {
                    BossRoomDoorOnEvent?.Invoke();
                    allRoomClear = false;
                }
            }

        }
    }

    private bool isBossRoomClear = false;
    public bool IsBossRoomClear
    {
        get { return isBossRoomClear; }
        set
        {
            if (isBossRoomClear != value)
            {
                isBossRoomClear = value;
            }
            if (isBossRoomClear == true)
            {
                NextRoomDoorOnEvent?.Invoke();
            }
        }
    }



    public event System.Action DoorOnEvent;
    public event System.Action DoorOffEvent;
    public event System.Action BossRoomDoorOnEvent;
    public event System.Action NextRoomDoorOnEvent;



    // ----------------------------------------------- SG ------------------------------------------------


    [Header("BossRoom")]
    public List<GameObject> bossRoomPillars = new List<GameObject>();


    private void Awake()
    {
        // 싱글톤 인스턴스 초기화
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        AwakeInIt();
    }

    void Start()
    {
        // 데이터 가져오기
        GetData();
        StartInIt();
        SetGameSetting();
    }       // Start()    


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            GameOver();
        }
        if (Input.GetKeyDown(KeyCode.F11))
        {
            TeleportToBoss();
        }
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ClearDungeon();
        }


    }

    private void OnLevelWasLoaded()
    {
        // GFunc.Log("객체의 첫 생성일때에도 이게 호출이 되나?");
        // 데이터 가져오기
        GetData();
        SetGameSetting();
    }


    /// <summary>
    /// Awake사이클에서 초기화해야하는것 초기화하는 함수
    /// </summary>
    private void AwakeInIt()
    {
        if (isClearRoomList == null || isClearRoomList == default)
        {
            isClearRoomList = new List<RandomRoom>();
        }

    }       // AwakeInIt()

    private void StartInIt()
    {
        // TODO : 프로토타입 이후 수정 예정
        if (round == GameRound.Prototype)
        {
            isPlayerMaxFloor = PROTOTYPE;
            return;
        }
        if (round == GameRound.FirstTime)
        {
            isPlayerMaxFloor = FIRSTTIME;
        }
        else
        {
            isPlayerMaxFloor = FIRSTAFTER;
        }
    }       // StartInIt()

    private void SetGameSetting()
    {

        // 플레이어 찾아오기
        player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            input = player.transform.parent.GetComponent<InputBridge>();
        }
        else
        {
            GFunc.Log("플레이어를 찾지 못했습니다.");
        }
        // 스크린 페이더 가져오기
        if (Camera.main)
        {
            fader = Camera.main.transform.GetComponent<ScreenFader>();
        }




    }


    /*************************************************
     *            Game Over & Game Clear
    *************************************************/
    #region GameManager
    // 게임오버
    public void GameOver()
    {
        // 보스전일 경우 보스 처치 실패
        if (isBossBattle)
        {
            BossKillFail();
        }

        fader.DoFadeIn();
        screenText = player.GetComponent<ScreenText>();
        screenText.OnScreenText(gameoverText);
        input.enabled = false;

        isGameOver = true;
        UserData.GameOver();

        SceneLoad(gameoverScene); // 게임오버 씬 전환
    }

    // 현재 씬 리셋
    public void ResetScene()
    {
        isGameOver = true;
        UserData.GameOver();

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneLoad(currentSceneName);
    }

    /// <summary>  던전 클리어후 로비로 보내줄 함수 </summary>
    public void ClearDungeon()
    {
        isBossBattle = false;

        // 출구 층이면 로비로 보내주기
        if (nowFloor == isPlayerMaxFloor)
        {
            GFunc.Log("클리어 성공, 현재 층 : " + nowFloor);

            isGameOver = true;

            UserData.ClearDungeon();

            string lobbySceneName = "3_LobbyScene";
            SceneLoad(lobbySceneName);
        }
        // 출구 층이 아니라면, 다시 던전씬 돌리기
        else if (nowFloor < isPlayerMaxFloor)
        {
            GFunc.Log("출구층 아님, 현재 층 : " + nowFloor);

            // 층 높이고
            nowFloor++;
            string dungeonSceneName = "6_DungeonReadyScene";
            SceneLoad(dungeonSceneName);
        }
        else
            GFunc.Log("클리어 실패, 현재 층 : " + nowFloor);

    }       // ClearDungeon()

    // 보스 처치 실패
    public void BossKillFail()
    {
        isBossBattle = false;

        Quest curSubQuest = Unit.GetInProgressSubQuest();    // 현재 진행중인 서브 퀘스트 반환 (보스 처치 퀘스트)
        int clearID = curSubQuest.QuestData.ID;              // 진행중 서브 퀘스트 ID
        int[] clearEventIDs = Unit.ClearQuestByID(clearID);  // 완료 상태로 변경 & 보상 지급 & 선행퀘스트 조건이 있는 퀘스트들 조건 확인후 시작가능으로 업데이트
        if (clearEventIDs != null)
        {
            for (int i = 0; i < clearEventIDs.Length; i++)
            {
                if (clearEventIDs[i] == 0)
                    break;
                Quest quest = Unit.GetQuestByID(clearEventIDs[i]);
                quest.ChangeState(1);
            }
        }
    }

    #endregion

    public void DestroyGameManager()
    {
        Destroy(gameObject);
    }


    /*************************************************
     *                Scene Manager
    *************************************************/
    #region Scene Manager
    // 씬 전환 함수
    public void SceneLoad(string _sceneName)
    {
        if (string.IsNullOrEmpty(_sceneName))
        {
            GFunc.Log("전환할 씬을 찾지 못했습니다.");
            return;
        }
        StartCoroutine(SceneChangeDelay(_sceneName));
    }

    // 플레이어의 페이드를 포함한 씬 전환 딜레이
    IEnumerator SceneChangeDelay(string _sceneName)
    {
        if (fader)
        {
            fader.DoFadeIn();
        }

        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene(_sceneName);

        if (isGameOver)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    /*************************************************
    *                 Dungeon Object
    *************************************************/
    #region Dungeon
    /// <summary>
    /// 문을 열고 닫는 함수를 하나로 묶은것
    /// </summary>
    /// <param name="_isDoorOn">문을 열지 닫을지 bool값</param>
    private void DoorControl(bool _isDoorOn)
    {
        if (_isDoorOn == true)
        {
            //GFunc.Log($"문열기 호출");
            DoorOn();
        }
        else if (_isDoorOn == false)
        {
            //GFunc.Log($"문닫기 호출");
            DoorOff();
        }
    }       // DoorControll()

    private void DoorOn()
    {
        DoorOnEvent?.Invoke();
    }       // DoorOn()

    private void DoorOff()
    {
        DoorOffEvent?.Invoke();
    }       // DoorOff()

    /// <summary>
    /// 모든 방이 클리어 됬는지 체크해줄 함수
    /// </summary>
    public bool CheckAllRoomClear()
    {
        if (isClearRoomList.Count != 0)
        {
            foreach (RandomRoom temp in isClearRoomList)
            {
                //GFunc.Log($"{temp.gameObject.name}의 클리어 여부 : {temp.isClearRoom}");

                if (temp.isClearRoom == false)
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }       // CheckAllRoomClear()

    /// <summary>
    /// 리스트에 NullRoom이라는 객체를 Add해주는 함수
    /// </summary>
    /// <param name="_nullRoom"></param>
    public void AddNullRoom(NullRoom _nullRoom)
    {
        if (nullRoomList == null || nullRoomList == default)
        {       // 참조되는순간에 New할당
            nullRoomList = new List<NullRoom>();
        }
        else { /*PASS*/ }

        nullRoomList.Add(_nullRoom);

        StartCoroutine(CreateAnvil());

        //if (nullRoomList.Count == 2 && nowFloor % 2 == 0)
        //{   // 빈방이 2개일때와 현재층이 짝수 일때에 들어오는 조건문
        //    int randListIdx = UnityEngine.Random.Range(0, nullRoomList.Count);

        //    nullRoomList[randListIdx].CreateAnvilObj();
        //    GFunc.Log($"모루 생성 -> 위치 {nullRoomList[randListIdx].gameObject.name}");
        //}

        //GFunc.Log($"NullRomListCount : {nullRoomList.Count}\nNowFloor : {nowFloor}");


    }       // AddNullRoom()

    IEnumerator CreateAnvil()
    {
        yield return new WaitForSeconds(nullRoomList.Count);

        if (isAnvilCreate == false)
        {
            if (nullRoomList.Count >= 2 && nowFloor % 2 == 0)
            {   // 빈방이 2개일때와 현재층이 짝수 일때에 들어오는 조건문
                int randListIdx = UnityEngine.Random.Range(0, nullRoomList.Count);

                nullRoomList[randListIdx].CreateAnvilObj();
                //GFunc.Log($"모루 생성 -> 위치 {nullRoomList[randListIdx].gameObject.name}");
                isAnvilCreate = true;
            }
        }
        else { /*PASS*/ }

    }


    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void BossCutScene()
    {
        playerPos = player.transform.position;  // 플레이어 기존 위치 캐싱

        player.GetComponent<SmoothLocomotion>().freeze = true;

        // 컷씬룸 구성
        cutSceneRoomPosition = playerPos;
        cutSceneRoomPosition.y -= 50;
        GameObject cutSceneRoomPrefab = Resources.Load<GameObject>("Prefabs/CutSceneRoom");

        GameObject cutSceneRoom = Instantiate(cutSceneRoomPrefab, cutSceneRoomPosition, Quaternion.identity);

        StartCoroutine(FadeRoutine(cutSceneRoom.transform.GetChild(0).transform.position));
    }

    public void EndBossCutScene()
    {
        player.GetComponent<SmoothLocomotion>().freeze = false;

        StartCoroutine(FadeRoutine(playerPos));
    }

    IEnumerator FadeRoutine(Vector3 position)
    {
        FadeIn();
        yield return new WaitForSeconds(1);
        player.transform.position = position;
        FadeOut();
        yield return new WaitForSeconds(1);
        yield break;
    }
    IEnumerator Fade()
    {
        FadeIn();
        yield return new WaitForSeconds(3);
        FadeOut();
        yield return new WaitForSeconds(1);
        yield break;
    }
    public void DoFade()
    {
        StartCoroutine(Fade());
    }

    public void FadeIn()
    {
        if (fader)
        {
            fader.DoFadeIn();
        }
    }
    public void FadeOut()
    {
        if (fader)
        {
            fader.DoFadeOut();
        }
    }





    /// <summary>
    /// 유령 오브젝트를 List에 Add해주는 함수
    /// </summary>
    public void AllocatedGhostObj()
    {
        if (UserDataManager.Instance.ClearCount < 100)
        {
            if (isInItGhost == false)
            {       // 문제 X
                isInItGhost = true;
                ghostObjList = new List<GameObject>();
                GameObject ghostObj;
                ghostObj = Resources.Load<GameObject>("NPC_12_Ghost_FT");
                ghostObjList.Add(ghostObj);
                ghostObj = Resources.Load<GameObject>("NPC_12_Ghost_IE");
                ghostObjList.Add(ghostObj);
                ghostObj = Resources.Load<GameObject>("NPC_12_Ghost_IEFT");
                ghostObjList.Add(ghostObj);
                ghostObj = Resources.Load<GameObject>("NPC_12_Ghost_NS");
                ghostObjList.Add(ghostObj);
                ghostObj = Resources.Load<GameObject>("NPC_12_Ghost_PJ");
                ghostObjList.Add(ghostObj);

            }
            else { /*PASS*/ }
        }
        else { /*PASS*/ }



    }       // AllocatedGhostObj()
    #endregion

    /*************************************************
    *                        Data
    *************************************************/
    #region Data
    // 데이터 가져오기
    public void GetData()
    {
        SetPlayState();
        gameoverText = (string)DataManager.Instance.GetData(1001, "GameOverText", typeof(string));
    }

    // 아이디 가져오기
    public void SetPlayerID(string id)
    {
        _playerID = id;
    }

    // 플레이 상태 세팅
    private void SetPlayState()
    {
        if (UserDataManager.Instance.ClearCount == 0)
        {
            round = GameRound.FirstTime;
        }
        else
        {
            round = GameRound.FirstAfter;
        }

        if (IsProtoType)
        {
            round = GameRound.Prototype;
        }
    }
    #endregion



    private void TeleportToBoss()
    {
        Damage.instance.critProbability = 100;
        Damage.instance.critIncrease = 100;
        player.transform.position = GameObject.FindGameObjectWithTag("Finish").transform.position;

    }
}       // ClassEnd
