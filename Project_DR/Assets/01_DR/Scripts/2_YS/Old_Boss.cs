using BNG;
using Js.Quest;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Old_Boss : MonoBehaviour
{
   
    public UnityEngine.UI.Slider bossHPSlider;
    private TMP_Text bossHPText;

    public GameObject bossState;

    public Rigidbody rigid;

    public Damageable damageable;

    public UnityEngine.UI.Image targetImage;

    GameObject instantLazer;
    GameObject instantLazerFire;

    public float turnSpeed = 15.0f;

    public float missileCount = 6.0f;

    [Header("분쇄")]
    public GameObject smash;
    public UnityEngine.UI.Image smashFilled;
    public TMP_Text smashCountNum;
    public float skillTime = 10.0f;
    public int countNum = 1;

    [Header("분쇄 데미지 퍼센트")]
    public float smashOne = 0.03f;
    public float smashSecond = 0.15f;
    public float smashThird = 0.6f;

    [Header("분쇄 발동 카운트")]
    public int smashCount = 0;      //깍아냄
    public int smashMaxCount = 3;  //깍아냄 횟수 충족


    [Header("보스 및 투사체 테이블 관련")]
    public int bossId;  //보스 테이블
    public int bossProjectileId;  //투사체 테이블
    public int bossProjectileID;
    public int bossProjectileBounceId;
    public int bossProjectileLazerId;

    public float power = 5.0f;

    [Header("넉백 관련")]
    private bool isMoving = false;
    private float moveDuration = 1.0f;
    private float moveTimer = 0.0f;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    [Header("레이저 관련")]
    public Transform lazerPort;
    public GameObject lazer;
    public GameObject lazerFire;
    public float waitLazer = default;

    [Header("원거리 공격 소형 투사체")]
    public float bulletCount = default;
    public GameObject smallBulletPrefab;
    public float delayTime = default;
    public float destoryTime = default;
    public float speed = default;
    public float waitBullet = default;

    [Header("애드벌룬 투사체 생성")]
    public GameObject bounce;
    public float bounceCount = default;
    public float destoryTimeBounce = default;
    public float speedBounce = default;
    public float waitBounce = default;

    [Header("타겟")]
    public Transform target;
    public GameObject player;
    public PlayerBackDash knockBack;


    [Header("테이블")]
    public float hp = default;
    public float maxHp = default;

    [Header("조건")]
    public bool isLazer = false;
    public bool isPattern = false;
    public bool isDie = false;
    public bool isPatternExecuting = false;
    public bool isShoot = false;
    public bool isPushPlayer = false;
    public bool isKnockBack = false;
    public bool isKnockBackSecond = false;
    public bool isKnockBackThird = false;

    public bool isStart = false;
    public bool isBounce = false;
    public bool isBrick = false;
    public bool isLazerCoroutineRunning = false;

    [Header("패턴 간격")]
    public float patternInterval = default;

    //[Header("포물선 터지는 오브젝트")]
    //public GameObject explosionPrefab;
    //public Transform explosionPort;
    //public Transform explosionPortLeft;
    //public Transform explosionPortRight;

    [Header("포물선 안 터지는 오브젝트")]
    public GameObject bigBrick;
    public float destroyTimeBrick = default;
    public float brickCount = default;
    public float waitBrick = default;

    [Header("Conversation")]
    public BossNPC npc;
    public UnityEvent bossMeet;
    public GameObject bossStartPosition;
    private int startDistance;
    private Vector3 bossStartPos;

    //[Header("유도 미사일 테스트")]
    //public Transform testPort;
    //public GameObject testBullet;
    private Vector3 initialTargetImagePosition;

    [Header("IEnumerator")]
    IEnumerator shootPlay;
    IEnumerator shootLazer;
    IEnumerator shootBounce;
    IEnumerator shootBrick;

    public bool IsUseFunctionalityOnly => _isUseFunctionalityOnly;
    [SerializeField] private Transform _attackTransform;
    [SerializeField] private bool _isUseFunctionalityOnly = false;      // 해당 스크립트의 기능만 사용하는지 여부

    public void Awake()
    {
        GetData(bossId, bossProjectileId, bossProjectileID, bossProjectileBounceId, bossProjectileLazerId);
    }

    public void Start()
    {
        // 기존 Old 보스일 경우
        if (! _isUseFunctionalityOnly)
        {
            InitializeBoss();
        }
    }
    public void InitializeBoss()
    {
        _attackTransform = transform;

        bossState = GameObject.FindWithTag("Boss");

        rigid = GetComponent<Rigidbody>();
        player = GameObject.FindWithTag("Player");
        target = player.GetComponent<PlayerPosition>().playerPos;
        knockBack = player.GetComponent<PlayerBackDash>();

        //particleSystem = GetComponent<ParticleSystem>();

        if (damageable != null)
        {
            damageable.Health = maxHp;

            SetMaxHealth(damageable.Health);  //maxhp
        }

        if (bossState.GetComponent<BossState>() != null)
        {
            if (bossState)
            {
                bossState.GetComponent<BossState>().CastSpell();
            }
        }

        npc = GetComponent<BossNPC>();

        //코루틴 
        shootPlay = PlayShoot();
        shootLazer = LazerCoroutine();
        shootBounce = BounceShoot();
        shootBrick = BigBrickShoot();

        bossStartPos = bossStartPosition.transform.localPosition;
        bossStartPos.z = startDistance - 17;
        bossStartPosition.transform.localPosition = bossStartPos;
    }

    // 공격의 주체를 Init
    public void InitializeTransform(Transform transform)
    {
        _attackTransform = transform;
        GFunc.Log(_attackTransform);
    }

    public void FixedUpdate()
    {
        // _isUseFunctionalityOnly가 False일 경우
        if (! _isUseFunctionalityOnly)
        {
            if (target != null)
            {
                if (!isLazerCoroutineRunning)
                {
                    // Look At Y 각도로만 기울어지게 하기
                    Vector3 targetPostition =
                        new Vector3(target.position.x, _attackTransform.position.y, target.position.z);
                    _attackTransform.LookAt(targetPostition);
                }
            }
            else
            {
                return;
            }
        }
    }

    public void GetData(int bossId, int bossProjectileId, int bossProjectileID, int bossProjectileBounceId, int bossProjectileLazerId)
    {
        //보스
        maxHp = (float)DataManager.Instance.GetData(bossId, "BossHP", typeof(float));
        patternInterval = (float)DataManager.Instance.GetData(bossId, "PatternChange", typeof(float)); //이건 하나만

        //소형 투사체 6910
        bulletCount = (float)DataManager.Instance.GetData(bossProjectileId, "Count", typeof(float));
        delayTime = (float)DataManager.Instance.GetData(bossProjectileId, "Delay", typeof(float));
        waitBullet = (float)DataManager.Instance.GetData(bossProjectileId, "WaitTime", typeof(float));
        speed = (float)DataManager.Instance.GetData(bossProjectileId, "Speed", typeof(float));

        //6914 거대벽돌
        destroyTimeBrick = (float)DataManager.Instance.GetData(bossProjectileID, "DesTime", typeof(float));
        brickCount = (float)DataManager.Instance.GetData(bossProjectileID, "Count", typeof(float));
        waitBrick = (float)DataManager.Instance.GetData(bossProjectileID, "WaitTime", typeof(float));

        //6912 애드벌룬
        //destoryTimeBounce = (float)DataManager.Instance.GetData(bossProjectileBounceId, "DesTime", typeof(float));
        speedBounce = (float)DataManager.Instance.GetData(bossProjectileBounceId, "Speed", typeof(float));
        bounceCount = (float)DataManager.Instance.GetData(bossProjectileBounceId, "Count", typeof(float));
        waitBounce = (float)DataManager.Instance.GetData(bossProjectileBounceId, "WaitTime", typeof(float));

        //6915
        waitLazer = (float)DataManager.Instance.GetData(bossProjectileLazerId, "WaitTime", typeof(float));
        startDistance = Data.GetInt(9106, "BossRoomHeight");
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetMaxHealth(float newHealth)
    {
        bossHPText = bossHPSlider.transform.GetChild(2).GetComponent<TMP_Text>();

        bossHPSlider.maxValue = newHealth;
        bossHPSlider.value = newHealth;
        bossHPText.text = newHealth.ToString();
    }
    public void SetHealth(float newHealth)
    {
        bossHPSlider.value = newHealth;
        bossHPText.text = newHealth.ToString();
    }

    public virtual IEnumerator ExecutePattern()
    {
        //GFunc.Log("코루틴이 한번만 실행이 되는지");

        while (!isDie)
        {
            yield return new WaitForSeconds(0.3f);

            if (!isPatternExecuting)
            {
                isPatternExecuting = true;

                //체력에 따라 랜덤으로 패턴 선택
                if (damageable.Health <= maxHp * 1.0f && damageable.Health > maxHp * 0.76f)
                {
                    //GFunc.Log("체력별 패턴 1 진입");

                    RandomPattern();
                    //GFunc.Log("3개 패턴 동시에 발동");
                    //GFunc.Log("랜덤 패턴1 발동");

                }
                else if (damageable.Health <= maxHp * 0.75f && damageable.Health > maxHp * 0.51f)
                {


                    RandomPattern();
                    //GFunc.Log("4개 패턴 동시에 발동");
                    //GFunc.Log("랜덤 패턴2 발동");

                    if (bossState && !isKnockBack)
                    {
                        PushPlayerBackward();
                        //GFunc.Log("넉백");

                        bossState.GetComponent<BossState>().CastSpell();
                        //GFunc.Log("넉백 애니메이션 작동");
                        isKnockBack = true;

                    }
                }
                else if (damageable.Health <= maxHp * 0.5 && damageable.Health > maxHp * 0.26f)
                {
                    //GFunc.Log("체력별 패턴 3 진입");

                    RandomPattern();
                    //GFunc.Log("랜덤 패턴3 발동");

                    if (bossState && !isKnockBackSecond)
                    {
                        PushPlayerBackward();
                        //GFunc.Log("넉백");

                        bossState.GetComponent<BossState>().CastSpell();
                        //GFunc.Log("넉백 애니메이션 작동");
                        isKnockBackSecond = true;

                    }
                }
                else if (0 < damageable.Health && damageable.Health <= maxHp * 0.25f)
                {
                    //GFunc.Log("체력별 패턴 4 진입");

                    RandomPatternSecond();
                    //GFunc.Log("랜덤 패턴4 발동");

                    if (bossState && !isKnockBackThird)
                    {
                        PushPlayerBackward();
                        //GFunc.Log("넉백");

                        bossState.GetComponent<BossState>().CastSpell();
                        //GFunc.Log("넉백 애니메이션 작동");
                        isKnockBackThird = true;

                    }
                }
                else if(damageable.Health <= 0)
                {
                    BossDie();
                }


                yield return new WaitForSeconds(patternInterval);
                isPatternExecuting = false;

            }
        }
    }

    public void RandomPattern()
    {
        int pattern = UnityEngine.Random.Range(0, 4);

        switch (pattern)
        {
            case 0:
                if(shootPlay != null)
                {
                    StartCoroutine(shootPlay);
                    shootPlay = null;
                }
                //GFunc.Log("패턴 1 선택");
                break;
            case 1:
                if(shootLazer != null)
                {
                   StopCoroutine(shootLazer);
                }
                StartCoroutine(shootLazer);
                //GFunc.Log("패턴 2 선택");
                break;
            case 2:
                if(shootBounce != null)
                {
                    StartCoroutine(shootBounce);
                    shootBounce = null;
                }
                //GFunc.Log("패턴 3 선택");
                break;
            case 3:
                if(shootBrick != null)
                {
                    StartCoroutine(shootBrick);
                    shootBrick = null;
                }
                //GFunc.Log("패턴 4 선택");
                break;
        }

        if (bossState)
        {
            bossState.GetComponent<BossState>().Attack();
            //GFunc.Log("보스 attack 애니메이션");
        }
    }
    public void RandomPatternSecond()
    {
        StartCoroutine(ExecuteRandomPatterns());
    }
    public IEnumerator ExecuteRandomPatterns()
    {
        int pattern1 = UnityEngine.Random.Range(0, 4);
        int pattern2 = UnityEngine.Random.Range(0, 4);

        // 랜덤으로 선택된 두 개의 패턴을 동시에 실행
        yield return StartCoroutine(ExecutePatternSecond(pattern1, "첫 번째 패턴"));
        yield return StartCoroutine(ExecutePatternSecond(pattern2, "두 번째 패턴"));

        if (bossState)
        {
            bossState.GetComponent<BossState>().Attack();
            //GFunc.Log("보스 attack 애니메이션");
        }
    }

    public IEnumerator ExecutePatternSecond(int pattern, string logMessage)
    {
        switch (pattern)
        {
            case 0:
                StartCoroutine(PlayShoot());
                //GFunc.Log(logMessage + ": 패턴 1 선택");
                break;
            case 1:
                StartCoroutine(LazerCoroutine());
                //GFunc.Log(logMessage + ": 패턴 2 선택");
                break;
            case 2:
                StartCoroutine(BounceShoot());
                //GFunc.Log(logMessage + ": 패턴 3 선택");
                break;
            case 3:
                StartCoroutine(BigBrickShoot());
                //GFunc.Log(logMessage + ": 패턴 4 선택");
                break;
        }

        // 코루틴이 완료될 때까지 대기
        yield return new WaitForSeconds(0);
    }

    public void RandomPatternThird()
    {
        StartCoroutine(ExecuteRandomPatternThird());
    }

    public IEnumerator ExecuteRandomPatternThird()
    {
        int pattern3 = UnityEngine.Random.Range(0, 4);
        int pattern4 = UnityEngine.Random.Range(0, 4);
        int pattern5 = UnityEngine.Random.Range(0, 4);

        // 랜덤으로 선택된 두 개의 패턴을 동시에 실행
        yield return StartCoroutine(ExecutePatternThird(pattern3, "첫 번째 패턴"));
        yield return StartCoroutine(ExecutePatternThird(pattern4, "두 번째 패턴"));
        yield return StartCoroutine(ExecutePatternThird(pattern5, "세 번째 패턴"));

        if (bossState)
        {
            bossState.GetComponent<BossState>().Attack();
            //GFunc.Log("보스 attack 애니메이션");
        }
    }

    public IEnumerator ExecutePatternThird(int pattern, string logMessage)
    {
        switch (pattern)
        {
            case 0:
                StartCoroutine(PlayShoot());
                //GFunc.Log(logMessage + ": 패턴 1 선택");
                break;
            case 1:
                StartCoroutine(LazerCoroutine());
                //GFunc.Log(logMessage + ": 패턴 2 선택");
                break;
            case 2:
                StartCoroutine(BounceShoot());
                //GFunc.Log(logMessage + ": 패턴 3 선택");
                break;
            case 3:
                StartCoroutine(BigBrickShoot());
                //GFunc.Log(logMessage + ": 패턴 4 선택");
                break;
        }

        // 코루틴이 완료될 때까지 대기
        yield return new WaitForSeconds(0);
    }

    public void RandomPatternForth()
    {
        StartCoroutine(ExecuteRandomPatternForth());
    }

    public IEnumerator ExecuteRandomPatternForth()
    {
        int pattern6 = UnityEngine.Random.Range(0, 4);
        int pattern7 = UnityEngine.Random.Range(0, 4);
        int pattern8 = UnityEngine.Random.Range(0, 4);
        int pattern9 = UnityEngine.Random.Range(0, 4);

        // 랜덤으로 선택된 두 개의 패턴을 동시에 실행
        yield return StartCoroutine(ExecutePatternForth(pattern6, "첫 번째 패턴"));
        yield return StartCoroutine(ExecutePatternForth(pattern7, "두 번째 패턴"));
        yield return StartCoroutine(ExecutePatternForth(pattern8, "세 번째 패턴"));
        yield return StartCoroutine(ExecutePatternForth(pattern9, "네 번째 패턴"));

        if (bossState)
        {
            bossState.GetComponent<BossState>().Attack();
            //GFunc.Log("보스 attack 애니메이션");
        }
    }

    public IEnumerator ExecutePatternForth(int pattern, string logMessage)
    {
        switch (pattern)
        {
            case 0:
                StartCoroutine(PlayShoot());
                //GFunc.Log(logMessage + ": 패턴 1 선택");
                break;
            case 1:
                StartCoroutine(LazerCoroutine());
                //GFunc.Log(logMessage + ": 패턴 2 선택");
                break;
            case 2:
                StartCoroutine(BounceShoot());
                //GFunc.Log(logMessage + ": 패턴 3 선택");
                break;
            case 3:
                StartCoroutine(BigBrickShoot());
                //GFunc.Log(logMessage + ": 패턴 4 선택");
                break;
        }

        // 코루틴이 완료될 때까지 대기
        yield return new WaitForSeconds(0);
    }

    public void PushPlayerBackward()
    {
        if (knockBack)
        {
            knockBack.OnBackDash(20);
        }
    }

    public IEnumerator PlayShoot()
    {

        if (!isShoot)
        {
            isShoot = true;

            List<GameObject> bullets = new List<GameObject>();

            // 총알 미리 생성
            for (int i = 0; i < bulletCount; i++)
            {
                Vector3 offset = Vector3.zero;

                offset = new Vector3(UnityEngine.Random.insideUnitCircle.x * 2.0f, 2.0f, UnityEngine.Random.insideUnitCircle.y * 2.0f);

                //기존 로직
                GameObject instantBullet = Instantiate(smallBulletPrefab, _attackTransform.position + offset, Quaternion.identity);
                //bullets.Add(instantBullet);
                //instantBullet.transform.LookAt(target.position);

                // 오브젝트 풀을 사용하여 총알을 가져옵니다.
                //GameObject instantBullet = ObjectPoolManager.GetObject(ObjectPoolManager.ProjectileType.CHAINBULLET);
                //GFunc.Log("오브젝트 풀 생성");
                //instantBullet.transform.position = transform.position + offset;
                //instantBullet.transform.rotation = Quaternion.identity;
                instantBullet.transform.LookAt(target.position);

                bullets.Add(instantBullet);

            }

            yield return new WaitForSeconds(waitBullet);

            //GFunc.Log($"리스트 크기 : {bullets.Count}");

            foreach (GameObject bullet in bullets)
            {
                if (bullet != null && bullet.activeSelf)
                {
                    Rigidbody rigidBullet = bullet.GetComponent<Rigidbody>();
                    rigidBullet.transform.LookAt(target.position);

                    yield return new WaitForSeconds(delayTime);

                    rigidBullet.velocity = (target.position - bullet.transform.position).normalized * speed;
                }
            }

            //기존 로직
            bullets.Clear();

            isShoot = false;
            //GFunc.Log($"불값 초기화가 언제 호출 되는지 : {isShoot}");

            //yield return new WaitForSeconds(destoryTime);
            //// 오브젝트 풀을 사용하여 총알을 반환합니다.
            //foreach (GameObject bullet in bullets)
            //{
            //    Destroy(bullet);
            //    //ObjectPoolManager.ReturnObjectToQueue(bullet, ObjectPoolManager.ProjectileType.CHAINBULLET);
            //    //GFunc.Log("반환 이상 없이 작동하는가?");
            //}


        }
        yield break;
    }


    public IEnumerator LazerCoroutine()
    {
        if (targetImage != null)
        {
            isLazerCoroutineRunning = true;

            // 처음 활성화될 때의 위치를 저장
            initialTargetImagePosition = target.position;

            // 처음 위치로 설정
            if (targetImage != null)
            {
                targetImage.gameObject.SetActive(true);
                targetImage.transform.position = initialTargetImagePosition; // Set position after activation
            }

            // 기다리고
            yield return new WaitForSeconds(waitLazer);

            // 발사될 때 처음 위치 사용
            ShootLazer(initialTargetImagePosition);

            // 레이저가 발사된 후 n초 대기 후 레이저 파이어 생성
            yield return new WaitForSeconds(0.1f);
            LazerFire(initialTargetImagePosition);

            isLazerCoroutineRunning = false;
        }
        yield break;
    }

    public void LazerFire(Vector3 firePosition)
    {
        instantLazerFire = Instantiate(lazerFire, firePosition, Quaternion.identity);
    }

    public void ShootLazer(Vector3 shootPosition)
    {
        // 레이저를 생성하고 shootPosition을 기준으로 방향을 설정합니다.
        instantLazer = Instantiate(lazer, lazerPort.position, lazerPort.rotation);
        instantLazer.transform.LookAt(shootPosition);
        lazerPort.transform.LookAt(shootPosition);

        // 1초 후에 레이저를 파괴하도록 예약합니다.
        Invoke("ReturnImage", 1.0f);
    }

    public void ReturnImage()
    {
        targetImage.gameObject.SetActive(false);
    }

    //public void DestroyLazer()
    //{
    //    if (instantLazer != null)
    //    {
    //        Destroy(instantLazer);
    //        GFunc.Log("레이저 파괴되는가");
    //    }

    //    // 타겟 이미지 비활성화
    //    targetImage.gameObject.SetActive(false);
    //}


    public IEnumerator BigBrickShoot()
    {
        if (!isBrick)
        {
            isBrick = true;

            List<GameObject> bigbrick = new List<GameObject>();

            for (int i = 0; i < brickCount; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.insideUnitCircle.x * 4.0f, 2.0f, UnityEngine.Random.insideUnitCircle.y * 3.0f);

                GameObject instantBrick = Instantiate(bigBrick, _attackTransform.position + offset, Quaternion.identity);
                //GameObject instantBrick = ObjectPoolManager.GetObject(ObjectPoolManager.ProjectileType.BIGBRICK);
                //instantBrick.transform.position = transform.position + offset;
                //instantBrick.transform.rotation = Quaternion.identity;

                bigbrick.Add(instantBrick);

                instantBrick.SetActive(true);
                SphereCollider brickCollider = instantBrick.GetComponent<SphereCollider>();
                brickCollider.isTrigger = true;

                Rigidbody brickRigidbody = instantBrick.GetComponent<Rigidbody>();
                brickRigidbody.useGravity = false;
            }

            yield return new WaitForSeconds(waitBrick);

            foreach (GameObject instantBrick in bigbrick)
            {
                SphereCollider brickCollider = instantBrick.GetComponent<SphereCollider>();
                brickCollider.isTrigger = false;

                Rigidbody brickRigidbody = instantBrick.GetComponent<Rigidbody>();
                brickRigidbody.useGravity = true;
                brickRigidbody.AddForce(parabola(), ForceMode.Impulse);

            }

            //yield return new WaitForSeconds(destroyTimeBrick);

            //foreach (GameObject instantBrick in bigbrick)
            //{
            //    Destroy(instantBrick);
            //    //ObjectPoolManager.ReturnObjectToQueue(instantBrick, ObjectPoolManager.ProjectileType.BIGBRICK);
            //}

            bigbrick.Clear();

            isBrick = false;
        }


        yield break;


    }

    public Vector3 parabola()
    {
        int randomPower = UnityEngine.Random.Range(3, 8);

        Vector3 pos = _attackTransform.forward;
        pos.y += 3.0f;

        return pos * randomPower;
    }

    public IEnumerator BounceShoot()
    {
        if (!isBounce)
        {
            isBounce = true;

            List<GameObject> bounceBall = new List<GameObject>();

            for (int i = 0; i < bounceCount; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.insideUnitCircle.x * 6.0f, 3.0f, UnityEngine.Random.insideUnitCircle.y * 5.0f);

                //기존 로직
                GameObject instantBounce = Instantiate(bounce, _attackTransform.position + offset, Quaternion.identity);
                //GameObject instantBounce = ObjectPoolManager.GetObject(ObjectPoolManager.ProjectileType.BOUNCEBALL);
                //GFunc.Log("오브젝트 풀 생성");
                //instantBounce.transform.position = transform.position + offset;
                //instantBounce.transform.rotation = Quaternion.identity;

                bounceBall.Add(instantBounce);

                SphereCollider bounceCollider = instantBounce.GetComponent<SphereCollider>();
                bounceCollider.isTrigger = true;
                //GFunc.Log("생성 후 트리거 true");


            }

            yield return new WaitForSeconds(waitBounce);
            //GFunc.Log("대기한다");

            foreach (GameObject instantBounce in bounceBall)
            {
                if (instantBounce != null && instantBounce.activeSelf)
                {
                    SphereCollider bounceCollider = instantBounce.GetComponent<SphereCollider>();
                    bounceCollider.isTrigger = false;
                    //GFunc.Log("발사전 트리거 false");

                    Rigidbody bounceRigidbody = instantBounce.GetComponent<Rigidbody>();
                    bounceRigidbody.velocity = -instantBounce.transform.forward.normalized * speedBounce;
                    //GFunc.Log("발사");
                }

            }

            //yield return new WaitForSeconds(destoryTimeBounce);

            //foreach (GameObject instantBounce in bounceBall)
            //{
            //    Destroy(instantBounce);
            //    //ObjectPoolManager.ReturnObjectToQueue(instantBounce, ObjectPoolManager.ProjectileType.BOUNCEBALL);
            //}

            bounceBall.Clear();

            isBounce = false;
        }

        yield break;

    }

    public void OnDeal(float damage)
    {
        if (damageable.Health <= 0)
        {
            BossDie();
        }

        if (!isDie)
        {
            if (damageable.Health >= 0)
            {
                SetHealth(damageable.Health);
                //GFunc.Log($"현재 HP: {damageable.Health}");
            }


            smashCount++;   // 분쇄 카운트 추가

            if (smashCount >= smashMaxCount)
            {
                smash.SetActive(true);
                //GFunc.Log("분쇄카운트 충족");

                //float smashTakeDamage = damageable.Health * smashOne;
                //SetHealth(damageable.Health - smashTakeDamage);
                //Debug.Log($"받는 데미지:{damageable.Health - smashTakeDamage}");

                smashCount = 0;
                //GFunc.Log($"분쇄 카운트:{smashCount}");

                smashFilled.fillAmount = 1;
                //GFunc.Log($"분쇄FillAmount:{smashFilled.fillAmount}");

                StartCoroutine(SmashTime());

                if (countNum <= 3)
                {
                    smashCountNum.text = countNum.ToString();
                    countNum++;
                    //Debug.Log($"숫자:{countNum}");
                }
                else if (countNum == 5)
                {

                }

                //GFunc.Log($"숫자:{countNum}");

                ApplyStackDamage(damage);
                //GFunc.Log("스택 별 데미지 진입");

                //GFunc.Log("중첩 숫자 증가");
            }
        }
    }

    public float OtherOnDeal(float damage)
    {
        smashCount++;   // 분쇄 카운트 추가

        if (smashCount >= smashMaxCount)
        {
            smash.SetActive(true);
            //GFunc.Log("분쇄카운트 충족");

            //float smashTakeDamage = damageable.Health * smashOne;
            //SetHealth(damageable.Health - smashTakeDamage);
            //Debug.Log($"받는 데미지:{damageable.Health - smashTakeDamage}");

            smashCount = 0;
            //GFunc.Log($"분쇄 카운트:{smashCount}");

            smashFilled.fillAmount = 1;
            //GFunc.Log($"분쇄FillAmount:{smashFilled.fillAmount}");

            StartCoroutine(SmashTime());

            if (countNum <= 3)
            {
                smashCountNum.text = countNum.ToString();
                countNum++;
                //Debug.Log($"숫자:{countNum}");
            }
            else if (countNum == 5)
            {

            }

            //GFunc.Log($"숫자:{countNum}");

            return ApplyStackDamage(damage);
            //GFunc.Log("스택 별 데미지 진입");

            //GFunc.Log("중첩 숫자 증가");
        }

        // 아닐 경우
        return damage;
    }

    public float ApplyStackDamage(float damage)
    {
        Debug.Log($"countNum = {countNum}");

        if (countNum == 2)
        {
            if (damageable != null)
            {
                damageable.Health -= SmashDamageCalculate(damage, 1);  //1단계
                // 갱신된 체력 값을 적용
                SetHealth(damageable.Health);

                // 남은 체력을 로그로 출력
                //Debug.Log($"추가 분쇄 데미지 1 : {SmashDamageCalculate(damage, 1)}, 남은체력:{damageable.Health}");
            }
            else
            {
                return SmashDamageCalculate(damage, 1);
            }
        }
        else if (countNum == 3)
        {
            if (damageable != null)
            {
                damageable.Health -= SmashDamageCalculate(damage, 2);
                SetHealth(damageable.Health);

                //Debug.Log($"추가 분쇄 데미지 2 : {SmashDamageCalculate(damage, 2)}, 남은체력:{damageable.Health}");
            }
            else
            {
                return SmashDamageCalculate(damage, 1);
            }

        }
        else if (countNum == 4)
        {
            if (damageable != null)
            {
                damageable.Health -= SmashDamageCalculate(damage, 3);
                SetHealth(damageable.Health);

                //Debug.Log($"남은체력:{damageable.Health}");

                //Debug.Log($"추가 분쇄 데미지 3 : {SmashDamageCalculate(damage, 3)}, 남은체력:{damageable.Health}");
            }
            else
            {
                return SmashDamageCalculate(damage, 1);
            }
        }

        return default;
    }
    /// <summary> 분쇄 데미지를 계산하는 메서드 </summary>
    /// <param name="damage">플레이어의 최종 데미지</param>
    /// <param name="index">분쇄 단계</param>
    /// <returns></returns>
    public float SmashDamageCalculate(float damage, int index)
    {
        float _debuff = UserData.GetSmashDamage(index);
        return Mathf.RoundToInt(damage * (1 + _debuff)) - damage;

        //return damage * (1 + _debuff);
    }

    public IEnumerator SmashTime()
    {
        while (smashFilled.fillAmount > 0)
        {
            //GFunc.Log($"남은 시간:{smashFilled.fillAmount * skillTime}");
            //GFunc.Log("분쇄 fill");
            smashFilled.fillAmount -= 1 * Time.smoothDeltaTime / skillTime;

            if (smashFilled.fillAmount <= 0)
            {
                smash.SetActive(false);
                smashCount = 0;
                countNum = 1;
                //GFunc.Log("사라지나");
            }
            yield return null;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        // 기본 보스일 경우
        if (! _isUseFunctionalityOnly)
        {
            if (other.CompareTag("Player") && !isStart)
            {
                GFunc.Log("Old_Boss.Trigger");
                isStart = true;

                //// 보스 조우 퀘스트 콜백
                //QuestCallback.OnBossMeetCallback(bossId);   // 상태값 갱신 및 자동 완료
                //Unit.ClearQuestByID(3111001);               // 완료 상태로 변경 & 보상 지급 & 선행퀘스트 조건이 있는 퀘스트들 조건 확인후 시작가능으로 업데이트
                //Unit.InProgressQuestByID(3122001);          // 다음 퀘스트 진행중 으로 변경

                npc.BossMeet();

                //isStart = true;
                //GFunc.Log("인식되냐");
                //StartCoroutine(ExecutePattern());
            }
        }
    }

    // 전투 시작
    public void StartAttack()
    {
        StartCoroutine(ExecutePattern());
    }

    //private void OnDrawGizmos()
    //{

    //    Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, angleRange / 2, radius);
    //    Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, -angleRange / 2, radius);

    //}

    void DebugDrawOverlapSphere()
    {
        //Vector3 dir = target.position - transform.position;
        //Debug.DrawRay(transform.position, dir.normalized * damageRadius, Color.blue);
    }

    // 보스 처치 후 퀘스트
    public void ClearBossKillQuest(int bossID = 0)
    {
        if (bossID.Equals(0)) { bossID = bossId; }
        // 보스 죽음 퀘스트
        QuestCallback.OnBossKillCallback(bossID);

        Quest curSubQuest = Unit.GetCanCompleteSubQuest();    // 현재 진행중인 서브 퀘스트 반환 (보스 처치 퀘스트)
        int clearID = curSubQuest.QuestData.ID;              // 진행중 서브 퀘스트 ID

        int[] clearEventIDs = Unit.ClearQuestByID(clearID);  // 완료 상태로 변경 & 보상 지급 & 선행퀘스트 조건이 있는 퀘스트들 조건 확인후 시작가능으로 업데이트
        if (clearEventIDs != null)
        {
            for(int i = 0; i < clearEventIDs.Length; i++)
            {
                if (clearEventIDs[i] == 0)
                    break;
                Unit.InProgressQuestByID(clearEventIDs[i]);        // 다음 퀘스트 진행중 으로 변경
            }
        }
        //Unit.SaveQuestDataToDB();
    }

    public void BossDie()
    {
        StopAllCoroutines();

        isDie = true;
        SetHealth(0);

        if (bossState)
        {
            bossState.GetComponent<BossState>().Die();
        }

        Vector3 newSize = new Vector3(0.00001f, 0.00001f, 0.00001f);
        this.gameObject.transform.localScale = newSize;

        GetComponent<BossMonsterDeadCheck>().BossDie();
        UserData.KillBoss(Data.GetInt(bossId, "GiveEXP"));

        ClearBossKillQuest();
    }


    //public IEnumerator LazerCoroutine()
    //{
    //    if (targetImage != null)
    //    {
    //        targetImage.transform.position = target.position;
    //        targetImage.gameObject.SetActive(true);

    //        yield return new WaitForSeconds(3.0f);

    //        // 3초 대기 후, targetImage의 위치에서 레이저 발사
    //        ShootLazer(targetImage.transform.position);

    //        // 추가: 레이저가 발사된 후 n초 대기 후 레이저 파이어 생성
    //        yield return new WaitForSeconds(0.1f);
    //        LazerFire(targetImage.transform.position);
    //    }
    //}

    //public void LazerFire(Vector3 firePosition)
    //{
    //    instantLazerFire = Instantiate(lazerFire, firePosition, Quaternion.identity);
    //    Invoke("LazerFireDestroy", 1.0f);
    //}

    //public void LazerFireDestroy()
    //{
    //    if (instantLazerFire != null)
    //    {
    //        Destroy(instantLazerFire);
    //        GFunc.Log("화염지대 파괴되는가");
    //    }
    //    else
    //    {
    //        CancelInvoke("LazerFireDestroy");
    //        GFunc.Log("cancelinvoke");
    //    }

    //    targetImage.gameObject.SetActive(false);
    //    //CancelInvoke("LazerFireDestroy");
    //}

    //public void ShootLazer(Vector3 shootPosition)
    //{
    //    // 레이저를 생성하고 shootPosition을 기준으로 방향을 설정합니다.
    //    instantLazer = Instantiate(lazer, lazerPort.position, lazerPort.rotation);
    //    instantLazer.transform.LookAt(shootPosition); // target.position 대신에 shootPosition을 사용합니다.
    //    lazerPort.transform.LookAt(shootPosition);

    //    // 1초 후에 레이저를 파괴하도록 예약합니다.
    //    Invoke("LazerDestroy", 1.0f);
    //}

    //public void LazerDestroy()
    //{
    //    if (instantLazer != null)
    //    {
    //        Destroy(instantLazer);
    //        GFunc.Log("레이저 파괴되는가");
    //    }

    //    CancelInvoke("LazerDestroy");

    //}



    //public Vector3 BigBrick(Vector3 portPosition)
    //{
    //    int randomBrick = UnityEngine.Random.Range(0, 10);
    //    // 해당 위치에 따라 초기 속도 설정
    //    Vector3 initialVelocity = transform.forward * 10.0f;  //10.0f;

    //    Vector3 target = transform.forward * 10.0f;   //5.0f;

    //    //int randomBrick = UnityEngine.Random.Range(0, 3);
    //    // 각 포트 위치에 따라 Y축 오프셋 값 조절
    //    if (portPosition == bigBrickPort.position)
    //    {
    //        target.y += randomBrick;
    //    }
    //    else if (portPosition == bigBrickPortLeft.position)
    //    {   
    //        target.y += randomBrick;
    //    }
    //    else if (portPosition == bigBrickPortRight.position)
    //    {
    //        target.y += randomBrick;
    //    }

    //    // 초기 속도와 목표 위치를 결합
    //    Vector3 combinedVector = initialVelocity + target;

    //    return combinedVector;
    //}

    //IEnumerator BrickWait(float waitTime)
    //{
    //    //transform.position = Vector3.zero;
    //    Debug.Log("정지");
    //    yield return new WaitForSeconds(waitTime);

    //}
    ////유도미사일 테스트
    //IEnumerator GuidedShoot()
    //{
    //    if (!isShoot)
    //    {
    //        isShoot = true;

    //        List<GameObject> missiles = new List<GameObject>();

    //        // 미사일 미리 생성
    //        for (int i = 0; i < missileCount; i++)
    //        {
    //            Vector3 offset = UnityEngine.Random.insideUnitSphere * 2.0f;

    //            GameObject instantMissile = Instantiate(testBullet, transform.position + offset, Quaternion.identity);
    //            missiles.Add(instantMissile);
    //        }

    //        yield return new WaitForSeconds(2.0f);

    //        GFunc.Log($"리스트 크기 : {missiles.Count}");

    //        foreach (var missile in missiles)
    //        {
    //            // 미사일이 이미 파괴되었는지 확인
    //            if (missile != null)
    //            {
    //                Rigidbody rigidMissile = missile.GetComponent<Rigidbody>();
    //                HomingMissile homingMissile = missile.GetComponent<HomingMissile>(); // Add HomingMissile script to your missile prefab

    //                if (homingMissile != null)
    //                {
    //                    // 플레이어를 타겟으로 설정
    //                    homingMissile.SetTarget(GameObject.FindWithTag("Player").GetComponent<PlayerPosition>().playerPos);
    //                }

    //                yield return new WaitForSeconds(0.4f);

    //                // 발사 방향 설정 및 속도 적용
    //                rigidMissile.velocity = rigidMissile.transform.forward * speed;

    //                Destroy(rigidMissile, 6.0f);
    //            }
    //        }

    //        missiles.Clear();

    //        isShoot = false;
    //        GFunc.Log($"불값 초기화가 언제 호출 되는지 : {isShoot}");
    //    }
    //}

    //IEnumerator BigBrickShoot()
    //{
    //    // 대기 시간 설정 (여기서는 2초로 설정)
    //    float waitTime = 2.0f;

    //    Vector3 offset = Vector3.zero;

    //    offset = new Vector3(UnityEngine.Random.insideUnitCircle.x * 3.0f, 2.0f, UnityEngine.Random.insideUnitCircle.y * 2.0f);


    //    // 가운데, 왼쪽, 오른쪽 위치에 대해 동시에 오브젝트를 생성하고 힘을 적용
    //    GameObject instantBrick = Instantiate(bigBrick, transform.position + offset, Quaternion.identity);

    //    instantBrick.SetActive(true);
    //    Rigidbody brickRigidbody = instantBrick.GetComponent<Rigidbody>();
    //    brickRigidbody.useGravity = false;
    //    GFunc.Log($"활성화:{instantBrick}");

    //    GameObject instantBrickLeft = Instantiate(bigBrick, transform.position + offset, Quaternion.identity);

    //    instantBrickLeft.SetActive(true);
    //    Rigidbody brickRigidbodyLeft = instantBrickLeft.GetComponent<Rigidbody>();
    //    brickRigidbodyLeft.useGravity = false;
    //    //GFunc.Log($"활성화:{instantBrickLeft}");

    //    GameObject instantBrickRight = Instantiate(bigBrick, transform.position + offset, Quaternion.identity);

    //    instantBrickRight.SetActive(true);
    //    Rigidbody brickRigidbodyRight = instantBrickRight.GetComponent<Rigidbody>();
    //    brickRigidbodyRight.useGravity = false;
    //    GFunc.Log($"활성화:{instantBrickRight}");

    //    //n초 동안 대기하면서 gravity 비활성화
    //   yield return StartCoroutine(BrickWait(waitTime));
    //    GFunc.Log("대기");


    //    // gravity 활성화 및 힘을 적용
    //    brickRigidbody.useGravity = true;
    //    brickRigidbody.AddForce(BigBrick(bigBrickPort.position), ForceMode.Impulse);
    //    //GFunc.Log("가운데 발사");

    //    brickRigidbodyLeft.useGravity = true;
    //    brickRigidbodyLeft.AddForce(BigBrick(bigBrickPortLeft.position), ForceMode.Impulse);
    //    //GFunc.Log("왼쪽 발사");

    //    brickRigidbodyRight.useGravity = true;
    //    brickRigidbodyRight.AddForce(BigBrick(bigBrickPortRight.position), ForceMode.Impulse);
    //    //GFunc.Log("오른쪽 발사");

    //    // 나머지 대기 시간 후에 오브젝트 파괴
    //    yield return StartCoroutine(BrickWait(waitTime));
    //    Destroy(instantBrick, destroy);
    //    Destroy(instantBrickLeft, destroy);
    //    Destroy(instantBrickRight, destroy);

    //}

    //public Vector3 ExplosionBox(Vector3 portPosition)
    //{
    //    int randomBox = UnityEngine.Random.Range(0, 10);
    //    // 해당 위치에 따라 초기 속도 설정
    //    Vector3 initialVelocity = transform.forward * randomBox;  //10.0f;

    //    Vector3 target = transform.forward * randomBox;   //5.0f;

    //    // 각 포트 위치에 따라 Y축 오프셋 값 조절
    //    if (portPosition == explosionPort.position)
    //    {
    //        target.y += randomBox;
    //    }
    //    else if (portPosition == explosionPortLeft.position)
    //    {
    //        target.y += randomBox;
    //    }
    //    else if (portPosition == explosionPortRight.position)
    //    {
    //        target.y += randomBox;
    //    }

    //    // 초기 속도와 목표 위치를 결합
    //    Vector3 combinedVector = initialVelocity + target;

    //    return combinedVector;
    //}


    //void ExplosionShoot()
    //{
    //    GameObject instantExplosion = Instantiate(explosionPrefab, explosionPort.position, Quaternion.identity);
    //    instantExplosion.GetComponent<Rigidbody>().AddForce(ExplosionBox(explosionPort.position), ForceMode.Impulse);

    //    GameObject instantExplosionLeft = Instantiate(explosionPrefab, explosionPortLeft.position, Quaternion.identity);
    //    instantExplosionLeft.GetComponent<Rigidbody>().AddForce(ExplosionBox(explosionPortLeft.position), ForceMode.Impulse);

    //    GameObject instantExplosionRight = Instantiate(explosionPrefab, explosionPortRight.position, Quaternion.identity);
    //    instantExplosionRight.GetComponent<Rigidbody>().AddForce(ExplosionBox(explosionPortRight.position), ForceMode.Impulse);
    //}

    //public void Shoot()
    //{
    //    AudioManager.instance.PlaySFX("FireBomb");

    //    GameObject ball = Instantiate(FireBombPrefab, firePosition.position, Quaternion.identity);
    //    ball.GetComponent<Rigidbody>().AddForce(calculateForce(), ForceMode.Impulse);
    //}
    //// 힘 계산하기
    //public Vector3 calculateForce()
    //{
    //    Vector3 targetPos = GameManager.instance.Golem.transform.position;
    //    targetPos.y += 25f;
    //    //firePosition.LookAt();  // 발사방향은 골렘을 향하게 변경
    //    firePosition.LookAt(targetPos);  // 발사방향은 골렘을 향하게 변경
    //    return firePosition.forward * power;
    //}
}