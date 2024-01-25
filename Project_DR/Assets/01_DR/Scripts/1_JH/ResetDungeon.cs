using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BNG;

public class ResetDungeon : MonoBehaviour
{
    public Button button;


   public void ResetDungeonBtn()
    {
        FindObjectOfType<GameManager>().DoFade();
        Invoke("ResetDungeonFunc", 1f);
    }
    public void ResetDungeonFunc()
    {
        FindObjectOfType<DungeonCreator>().CreateDungeon();
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
