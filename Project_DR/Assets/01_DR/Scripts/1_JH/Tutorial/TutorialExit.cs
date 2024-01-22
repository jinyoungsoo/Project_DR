using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialExit;

public class TutorialExit : MonoBehaviour
{
    public VRSceneLoder sceneLoader;
    public enum Level { Easy, Hard}
    public Level level;


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // 데이터 저장 방식이 서버일 경우
            if (PlayerDataManager.IsLocal.Equals(false))
            {
                PlayerDataManager.Save("tutorial", 1);
            }

            // 로컬일 경우
            else
            {
                PlayerDataManager.SaveTutorial(1);
            }

            switch (level)
            {
                case Level.Easy:
                    UserDataManager.Instance.Gold = 5000;
                    UserDataManager.Instance.Exp = 30000;
                    UserDataManager.Instance.SaveGoldandExp();
                    break;
                case Level.Hard:
                    UserDataManager.Instance.Gold = 500;
                    UserDataManager.Instance.Exp = 5000;
                    UserDataManager.Instance.SaveGoldandExp();
                    break;
            }
            sceneLoader.LoadScene();            
        }
    }

}
