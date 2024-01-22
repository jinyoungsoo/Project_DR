using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Js.Quest;
using System;
using Rito.InventorySystem;
using Js.Crafting;

public class TEST : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            // PlayerPrefs에 저장된 모든 데이터 삭제
            PlayerPrefs.DeleteAll();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            // 데이터 저장
            UserDataManager.Instance.SaveLocalData();
        }
    }
    public void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }


}
