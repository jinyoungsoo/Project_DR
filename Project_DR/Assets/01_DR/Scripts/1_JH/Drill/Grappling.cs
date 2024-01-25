using BNG;
using Oculus.Interaction;
using Oculus.Platform.Models;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Windows;
using static UnityEngine.GraphicsBuffer;

public class Grappling : GrabbableEvents
{
    public enum State { Idle, Shooting, Grappling, Waiting };

    public State state = State.Idle;

    [Header("References")]
    public GameObject player;
    private CharacterController characterController;
    public Rigidbody playerRigid;
    private SmoothLocomotion smoothLocomotion;
    private PlayerTeleport teleport;
    private RaycastWeaponDrill weaponDrill;

    private PlayerGravity playerGravity;
    private PlayerClimbing playerClimbing;

    [Header("GrapplingGun")]
    public Transform gun;
    public Transform muzzleTransform;
    public LayerMask grappleableLayer;
    private LineRenderer line;
    public GameObject drill;
    public GameObject drillPrefab;
    private GameObject _drill;

    [Space]

    [Header("Grappling")]
    public float maxGrappleDistance;      // 최대사거리
    public float grappleDelayTime;        // 그래플링건 딜레이
    public float GrappleReelForce = 0.5f; // 그래플링건 힘
    public float currentGrappleDistance;  // 그래플링 거리

    private Vector3 grapplePoint;
    [Space]

    [Header("CoolDown")]
    public float grapplingCd;
    float lastGrapplingTime;
    private bool isGrappling;

    private bool isDamageCheck;
    private Damageable target;

    public TMP_Text debug;
    public TMP_Text debug2;

    private Vector3 smallScale;

    IEnumerator checkExcute;
    IEnumerator soundRoutine;
    WaitForSeconds waitGrappleDelay;

    public void OnEnable()
    {
        drill.SetActive(true);

    }


    private void Start()
    {
        smallScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
        player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            characterController = player.GetComponentInChildren<CharacterController>();
            smoothLocomotion = player.GetComponentInChildren<SmoothLocomotion>();
            playerGravity = player.GetComponentInChildren<PlayerGravity>();
            playerClimbing = player.GetComponentInChildren<PlayerClimbing>();
            playerRigid = player.GetComponent<Rigidbody>();
        }
        else
        {
            GFunc.Log("플레이어 오브젝트를 찾지 못함");
        }
        weaponDrill = GetComponent<RaycastWeaponDrill>();
        GetData();
        line = GetComponent<LineRenderer>();

        waitGrappleDelay = new WaitForSeconds(grappleDelayTime);

        AudioManager.Instance.AddSFX("SFX_Drill_HookShoot_Fire");
        AudioManager.Instance.AddSFX("SFX_Drill_HookShoot_Stick_01");
        AudioManager.Instance.AddSFX("SFX_Drill_HookShoot_Stick_02");
    }

    private void FixedUpdate()
    {
        if (state != State.Grappling)
            return;

        GrapplingMove(); // 그래플링일 경우에만 실행

    }


    private void LateUpdate()
    {
        

        // 그래플링 중이면
        if (state != State.Idle)
        {
            // 라인 만들고
            line.SetPosition(0, muzzleTransform.position);
            // 거리 계산
        }
        updateGrappleDistance();
        DamageCheck(); // 데미지 체크
        GrapleDisable();
    }

    // 그래플링 쏘기와 당기기를 한 버튼으로 할 경우
    public void OnGrapple()
    {
        // 쿨타임이 안돌고 기본 상태가 아니면
        if (Time.time - lastGrapplingTime < grapplingCd)
        {
            return;
        }

        if (state == State.Idle)
        {
            StartGrapple();
        }
        else if (state == State.Shooting)
        {
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        return;
    }
    public void CheckExcute()
    {
        if(checkExcute != null)
        {
            StopCoroutine(checkExcute);
            checkExcute = null;
        }
        checkExcute = CheckingButton1();
        StartCoroutine(checkExcute);
    }

    private WaitForFixedUpdate fixedUpdate = new WaitForFixedUpdate();
    IEnumerator CheckingButton1()
    {
        while (true)
        {
            if (Time.time - lastGrapplingTime < grapplingCd)
                yield return fixedUpdate;

            if(!input.AButton && state == State.Shooting) 
            {
                Invoke(nameof(ExecuteGrapple), grappleDelayTime);
                break;
            }

            yield return fixedUpdate;
        }
        yield break;
    }


    // 그래플링 시작
    public void StartGrapple()
    {
        if (state != State.Idle || Time.time - lastGrapplingTime < grapplingCd)
            return;
        lastGrapplingTime = Time.time;


        state = State.Shooting;                         // 그래플링을 발사한 상태
        //smoothLocomotion.freeze = true;                 // 플레이어 이동 못하는 상태로 전환
        input.VibrateController(0.1f, 0.2f, 0.1f, thisGrabber.HandSide);
        RaycastHit hit;
        // 레이 발사
        if (Physics.Raycast(muzzleTransform.position, gun.forward, out hit, maxGrappleDistance, grappleableLayer))
        {
            grapplePoint = hit.point;                         // 충돌한 곳이 있으면 그래플링 포인트로
            smoothLocomotion.freeze = false;                  // 플레이어 움직일 수 있도록 전환
            // 만약  damageable 오브젝트와 충돌 시
            if (hit.collider.GetComponent<Damageable>())
            {
                isDamageCheck = true;                             // 데미지 체크 켜고
                target = hit.collider.GetComponent<Damageable>(); // 타겟 세팅
                StartCoroutine(PlaySFXRoutine("SFX_Drill_HookShoot_Stick_01"));
            }
            else if (hit.collider.GetComponent<DamageablePart>())
            {
                isDamageCheck = true;
                target = hit.collider.GetComponent<DamageablePart>().parent;
                StartCoroutine(PlaySFXRoutine("SFX_Drill_HookShoot_Stick_01"));
            }
            else
                StartCoroutine(PlaySFXRoutine("SFX_Drill_HookShoot_Stick_02"));


        }

        else
        {
            grapplePoint = gun.position + gun.forward * maxGrappleDistance; // 충돌한곳이 없다면 최대사거리로 쏘고 
            state = State.Waiting;
            Invoke(nameof(StopGrapple), grappleDelayTime);                  // 그래플링 정지
        }

        line.enabled = true;                                  // 라인 켜주기
        line.SetPosition(1, grapplePoint);                    // 그래플링 포인트까지
        //drill.SetActive(false);                               // 달려있는 드릴 잠깐 꺼주고
        drill.transform.localScale = smallScale;
        ShootDrill();                                         // 그래플링용 드릴 발사


#if PC_Build
        // 에디터일 경우 체크 따로 하기 : 버튼 UP이 작동하지 않는 이유를 찾지 못함
        CheckExcute();
#endif
    }


    // 드릴 발사
    private void ShootDrill()
    {
        AudioManager.Instance.PlaySFX("SFX_Drill_HookShoot_Fire");

        _drill = Instantiate(drillPrefab, drill.transform.position, drill.transform.rotation); // 드릴 인스턴스
        //_drill.transform.localScale = drill.transform.localScale;
        _drill.transform.localScale = weaponDrill.GetDrillSize();                              // 현재 드릴 크기로 맞추기
        _drill.transform.LookAt(grapplePoint);                                                 // 타겟 바라보고
        _drill.GetComponent<DrillHead>().targetPos = grapplePoint;                             // 그래플링 포인트 세팅
        _drill.GetComponent<DrillHead>().grappling = this;                                     // 드릴이 만난 오브젝트의 체력이 0이하일 경우를 체크하기 위함

        _drill.GetComponent<DrillHead>().DrillSide(weaponDrill.isLeft);

        Destroy(_drill,grappleDelayTime);
    }
    

    // 그래플링 실행
    public void ExecuteGrapple()
    {
        if (Time.time - lastGrapplingTime < grapplingCd)
        {
            Invoke(nameof(Excute), grappleDelayTime);                  // 그래플링 실행
            //GFunc.Log("if");
        }

        //else
        //    GFunc.Log("else");
        Excute();
    }
    public void Excute()
    {
        if (state != State.Shooting)
        { return; }
        state = State.Grappling;
        changeGravity(false);
        //smoothLocomotion.freeze = true;
        smoothLocomotion.grappleCount++;
    }

    // 그래플링 이동 관련 : state가 그래플링일 경우 실행
    private void GrapplingMove()
    {
        if(playerRigid == null)
        {
            SetRigid();
        }

        if (currentGrappleDistance < 0.3f)
            return;
        // 플레이어 이동
        Vector3 moveDirection = (grapplePoint - muzzleTransform.position) * GrappleReelForce;
        //smoothLocomotion.MoveCharacter(moveDirection * Time.deltaTime);

        playerRigid.velocity = moveDirection;
        //playerRigid.MovePosition(grapplePoint);
    }

    // 그래플링 멈춤
    public void StopGrapple()
    {
        if (state == State.Idle)
            return;
        if (smoothLocomotion.grappleCount == 1)
        {
            changeGravity(true);
            smoothLocomotion.freeze = false;
            playerRigid.velocity = Vector3.zero;
        }
        if (state == State.Grappling)
        {
            smoothLocomotion.grappleCount--;
        }
        
        state = State.Idle;

        lastGrapplingTime = Time.time;

        //drill.SetActive(true);
        drill.transform.localScale = weaponDrill.GetDrillSize(); // 현재 드릴 크기로 되돌리기
        line.enabled = false;
        
        isDamageCheck = false;
        target = null;      
    }
    public void GrapleDisable()
    {
        if (currentGrappleDistance < 1.3f && state == State.Grappling)
        {
            StopGrapple();

            SkillManager.instance.CheckLandingHeight(); // 그래플링을 사용하고 스킬을 사용할 수 있는지 체크
        }
    }

    // 데미지를 체크하는 메서드
    public void DamageCheck()
    {
        if (!isDamageCheck)
            return;
        if (target.Health <= 0)
        {
            StopGrapple(); // 맞은 상대가 체력이 0이하이면, 그래플링 멈추기
        }
    }

    // 그래플링 포인트와 총구의 거리를 계산하는 메서드
    void updateGrappleDistance()
    {
        // Update Distance
        if (state != State.Idle)
        {
            currentGrappleDistance = Vector3.Distance(muzzleTransform.position, grapplePoint);
        }
        else
        {
            currentGrappleDistance = 0;
        }
    }

    // 중력 변경
    void changeGravity(bool gravityOn)
    {
        if (playerGravity)
        {
            playerGravity.ToggleGravity(gravityOn);
        }
        playerRigid.useGravity = gravityOn;
    }
    // 데이터 가져오기
    void GetData()
    {
        maxGrappleDistance = (float)DataManager.Instance.GetData(1100, "MaxDistance", typeof(float));
        grapplingCd = (float)DataManager.Instance.GetData(1100, "GrappleDelay", typeof(float)) ;
    }

    public void SetRigid()
    {
        // 리지드 바디 호출 후 없으면 생성하는 확장 메서드 호출
        playerRigid = player.GetOrAddRigidbody();
        //if (player.GetComponent<Rigidbody>())
        //{
        //    GFunc.Log("리지드바디 생성");
        //    playerRigid = player.GetComponent<Rigidbody>();
        //}
    }

    IEnumerator PlaySFXRoutine(string name)
    {
        yield return waitGrappleDelay;
        PlaySFX(name);
        yield break;
    }

    private void PlaySFX(string _name)
    {
        AudioManager.Instance.PlaySFXPoint(_name, grapplePoint);
    }
}
