using BNG;
using Oculus.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class HumanTypeNPC : NPC, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{       // 인간NPC의 BassClass

    private GameObject ConversationBox;

    protected override void Awake()
    {
        base.Awake();
        PointerSetting();
    }       // Awake()

    // 포인터가 생기도록 초기화
    private void PointerSetting()
    {
        PointerEvents pointer = gameObject.AddComponent<PointerEvents>();
        pointer.MaxDistance = 1;                                             // 최대거리 설정
        int PhysicsPointerLayer = 16;                                        // 레이어 설정
        this.gameObject.layer = PhysicsPointerLayer;

        GameObject npcConversationBox = Resources.Load<GameObject>("Prefabs/NPCConversationBox");
        Vector3 boxPosition = transform.position;
        boxPosition.y += 0.8f;
        boxPosition.x += 0.5f;
        ConversationBox = Instantiate(npcConversationBox, boxPosition, transform.rotation, transform);
        ConversationBox.SetActive(false);
    }

    // 대화 종료 후 레이어 변경
    private void ResetLayer()
    {
        int DefaultLayer = 1;
        this.gameObject.layer = DefaultLayer;
        Destroy(ConversationBox);
    }



    #region 포인터 이벤트

    // 포인터 클릭했을 때
    public void OnPointerClick(PointerEventData eventData)
    {
        ResetLayer();
        InvokeStartConverationEvent();
    }
    // 포인터 엔터했을 떄
    public void OnPointerEnter(PointerEventData eventData)
    {
        ConversationBox?.SetActive(true);
    }
    // 포인터 빠져나왔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        ConversationBox?.SetActive(false);
    }
    #endregion

}
