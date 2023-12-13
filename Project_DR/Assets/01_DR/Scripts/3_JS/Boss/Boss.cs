using BNG;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BossMonster
{
    public class Boss : MonoBehaviour
    {
        /*************************************************
         *                 Public Fields
         *************************************************/
        public BossData BossData => _bossData;
        public GameObject BossStone => _bossStone;
        public BossSummoningStone BossSummoningStone => _bossSummoningStone;
        public BossAnimationHandler BossAnimationHandler => _bossAnimationHandler;

        public IState CurrentState => _currentState;
        public IState IdleState => _idleState;
        public IState DieState => _dieState;
        public IState[] AttackStates => _attackStates;

        public Rigidbody Rigidbody => _bossData.Rigidbody;                          // 리지드 바디
        public Damageable Damageable => _bossData.Damageable;                       // 데미지 관련 처리
        public Transform Target => _bossData.Target;                                // 공격 대상
        public Animator Animator => _bossData.Animator;                             // 애니메이터


        /*************************************************
         *                 Private Fields
         *************************************************/
        // 보스 관련
        [SerializeField] private BossData _bossData;                                // 보스 데이터
        [SerializeField] private GameObject _bossStone;                             // 보스 소환석 게임 오브젝트
        [SerializeField] private BossSummoningStone _bossSummoningStone;            // 보스 소환석 스크립트
        [SerializeField] private string _bossStoneName = "BossSummoningStone";      // 가져올 소환석 프리팹 이름
        [SerializeField] private BossAnimationHandler _bossAnimationHandler;        // 보스 애니메이션 핸들러

        // 패턴에 따라 정의되는 상태
        private IState _currentState;                                               // 현재 상태
        private IState _idleState;                                                  // 대기 상태
        private IState _dieState;                                                   // 죽음 상태
        private IState[] _attackStates = new IState[10];                            // 공격 상태 패턴(0 ~ 9)[10]
        private List<int> _availableAttackStatesList = new List<int>();             // 사용 가능한 공격 패턴(0 ~ 9)[10]


        /*************************************************
         *                 Public Methods
         *************************************************/
        // Init
        public void Initialize(int id)
        {
            // 보스 관련 데이터 할당
            _bossData = new BossData(id);                               // 보스 데이터 생성
            _bossData.SetRigidbody(GetComponent<Rigidbody>());          // 리지드 바디 할당 
            _bossData.SetTarget(FindTarget("Player"));                  // 플레이어를 타겟으로 설정
            _bossData.SetAnimator(GetComponent<Animator>());            // 애니메이터 할당
            _bossAnimationHandler = new BossAnimationHandler(this);     // 보스 애니메이션 핸들러 생성

            // 상태 초기화 및 할당
            _idleState = new IdleState();
            _dieState = new DieState();
            SetAttackStates(id);

            // 초기 상태 변경
            _currentState = _idleState;
            // 대기 상태 진입
            _currentState.EnterState(this);
            // 상태 업데이트
            _currentState.UpdateState(this);

            // 보스 소환석 생성 및 Init
            CreateSummoningStone();

            // 데미지 관련 처리 할당
            _bossData.SetDamageable(_bossSummoningStone.Damageable);

            // 공격 패턴 랜덤 설정
            ChooseRandomPattern();
        }

        // 데미지 처리
        public void OnDamage(float damage)
        {
            // 소환석에 데미지 처리
            _bossSummoningStone.OnDamage(damage);
        }

        // 보스 오브젝트 삭제
        public void Dead()
        {
            // 죽음 애니메이션 재생
            _bossAnimationHandler.DieAnimation();

            // 3초 후 오브젝트 삭제
            Destroy(gameObject, 3.0f);
        }


        /*************************************************
         *                 Unity Methods
         *************************************************/
        private void FixedUpdate()
        {
            // 공격 대상으로 LookAt
            LookAtTarget(Target);
        }


        /*************************************************
         *                 State Methods
         *************************************************/
        // 현재 상태 변경
        public void ChangeState(IState state)
        { 
            if (_currentState != null)
            {
                // 상태 나가기
                _currentState.ExitState(this);
            }

            // 상태 변경
            _currentState = state;

            // 상태 진입
            _currentState.EnterState(this);

            // 상태 업데이트
            _currentState.UpdateState(this);
        }

        // 랜덤으로 사용 가능한 공격 패턴을 결정
        public void ChooseRandomPattern()
        {
            int patternCount = _bossData.PatternCount;
            int i = 0;
            while(i < patternCount)
            {
                int randomNumber = UnityEngine.Random.Range(0, _attackStates.Length);

                // 패턴 중복 체크
                if (! _availableAttackStatesList.Contains(randomNumber))
                {
                    // 중복이 아닐 경우 추가
                    _availableAttackStatesList.Add(randomNumber);
                }

                i++;
            }
        }


        /*************************************************
         *                 Private Methods
         *************************************************/
        // 공격 상태를 할당
        private void SetAttackStates(int id)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < _attackStates.Length; i++)
            {
                // 타입을 찾을 때 네임스페이스명 + 찾을 타입명으로 검색해야 함
                // 연산을 최소화 하기 위해 string 대신 StringBuilder 사용
                stringBuilder.Clear();
                stringBuilder.Append("BossMonster.AttackState_");
                stringBuilder.Append(i);
                //string type = "BossMonster.AttackState_" + i;     //Legacy:
                // 타입 검색
                Type attackStateType = Type.GetType(stringBuilder.ToString());
                // 타입이 있을 경우
                if (attackStateType != null)
                {
                    // _인스턴스를 생성하여 _attackStates 배열에
                    // 할당 & 생성자 호출
                    _attackStates[i] = (IState)Activator.CreateInstance(attackStateType, id, this);
                }

                // 없을 경우
                else
                {
                    Debug.LogWarning($"BossMonster.Boss.Initialize(): {stringBuilder} 타입을 찾을 수 없습니다.");
                }
            }
        }

        // 보스 소환석 생성
        private void CreateSummoningStone()
        {
            // 프리팹에 등록된 보스 소환석 생성
            GameObject bossStonePrefab = Resources.Load<GameObject>(_bossStoneName);
            GameObject bossStone = Instantiate(bossStonePrefab, transform);
                // 디버그용
                Vector3 position = new Vector3(0f, 1.013f, 4.42f);
                bossStone.transform.position = position;
                // 디버그용
            bossStone.name = _bossStoneName;
            _bossStone = bossStone;

            // 보스 소환석 Init
            _bossSummoningStone = bossStone.AddComponent<BossSummoningStone>();
            _bossSummoningStone.Initialize(this);
        }

        // 공격 대상 검색
        private Transform FindTarget(string targetName)
        {
            GameObject target = GameObject.FindWithTag(targetName);
            if (target != null)
            {
                // 타겟을 찾았을 경우
                Transform targetTransform = target.GetComponent<PlayerPosition>().playerPos;
                return targetTransform;
            }
            else
            {
                // 타겟을 못 찾았을 경우
                return default;
            }
        }

        // 공격 대상으로 LookAt
        public void LookAtTarget(Transform target)
        {
            if (target != null)
            {
                // Look At Y 각도로만 기울어지게 하기
                Vector3 targetPosition =
                    new Vector3(target.position.x, transform.position.y, target.position.z);
                transform.LookAt(targetPosition);
            }
        }
    }
}
