using System;
using System.Text;
using System.Collections;
using UnityEngine;
using Js.Quest;

public static class GFunc
{
    /*************************************************
     *               Private Fields
     *************************************************/
    private static StringBuilder stringBuilder = new StringBuilder();


    /*************************************************
     *               Public Methods
     *************************************************/
    // Legacy:
    //public static string SumString(string inputA, string inputB, string inputC = "", string inputD = "")
    //{
    //    stringBuilder.Clear();
    //    stringBuilder.Append(inputA);
    //    stringBuilder.Append(inputB);
    //    stringBuilder.Append(inputC);
    //    stringBuilder.Append(inputD);

    //    return stringBuilder.ToString();
    //}

    /// <summary>
    /// 매개 변수로 받은 모든 string 인자를 더해서 반환한다.
    /// </summary>
    public static string SumString(params string[] inputs)
    {
        stringBuilder.Clear();
        for (int i = 0; i < inputs.Length; i++)
        {
            stringBuilder.Append(inputs[i]);
        }

        return stringBuilder.ToString();
    }

    // 언더바를 없애주는 String 확장 메서드
    public static string RemoveUnderbar(this string input)
    {
        return input.Replace("_", "");
    }

    // CSV 문제로 인한 string 변환
    public static string ReplaceString(string str)
    {
        str = str.Replace("#", ",");
        str = str.Replace("_", " ");

        return str;
    }
    /// <summary>
    /// 늘어져있는 대사의 Replace와 Split이후 반환해주는 함수
    /// </summary>    
    /// <param name="_placeString">GetData해온 string값</param>
    /// <returns></returns>
    public static string[] SplitConversation(string _placeString)
    {        
        _placeString = _placeString.Replace("\\n", "\n");
        _placeString = _placeString.Replace("#", ",");
        _placeString = _placeString.Replace("\\", "");
        _placeString = _placeString.Replace("_", "");

        string[] replaceStrings = _placeString.Split("\n");

        //tableIDs = tableIDs.Replace("\\n", "\n");
        //tableIDs = tableIDs.Replace("\\", "");        


        return replaceStrings;
    }       // SplitConveration()

    public static string CSVConversation(string _placeString)
    {
        _placeString = _placeString.Replace("\\n", "\n");
        _placeString = _placeString.Replace("#", ",");
        _placeString = _placeString.Replace("\\", "");
        _placeString = _placeString.Replace("_", "");

        //tableIDs = tableIDs.Replace("\\n", "\n");
        //tableIDs = tableIDs.Replace("\\", "");        


        return _placeString;
    }       // SplitConveration()
    /// <summary>
    /// GetData해온 string값을 Replace와 Split한후 int[]로 변환시켜줘서 반환해주는 함수
    /// </summary>
    /// <param name="_parsString">Id값이 들어있는 string</param>
    /// <returns>int[] 반환</returns>
    public static int[] SplitIds(string _parsString)
    {
        _parsString = _parsString.Replace("\\\\n", "\n");
        _parsString = _parsString.Replace("\\n", "\n");
        _parsString = _parsString.Replace("#", ",");
        _parsString = _parsString.Replace("\\", "");
        _parsString = _parsString.Replace("_", "");
        string[] splitParams = _parsString.Split("\n");

        int[] splitIds = new int[splitParams.Length];
        for (int i = 0; i < splitParams.Length; i++)
        {
            // 공백 제거 후 추가
            splitIds[i] = int.Parse(splitParams[i].Trim());
        }

        return splitIds;
    }       // SplitIds()

    /// <summary>
    /// GetData해온 string값을 Replace와 Split한후 float[]로 변환시켜줘서 반환해주는 함수
    /// </summary>
    /// <param name="_parsString">Id값이 들어있는 string</param>
    /// <returns>int[] 반환</returns>
    public static float[] SplitFloats(string _parsString)
    {
        _parsString = _parsString.Replace("\\n", "\n");
        _parsString = _parsString.Replace("#", ",");
        _parsString = _parsString.Replace("\\", "");
        _parsString = _parsString.Replace("_", "");
        string[] splitParams = _parsString.Split("\n");

        for (int i = 0; i < splitParams.Length; i++)
        {
            GFunc.Log($"splitParms 값 {splitParams[i]}");
        }
        float[] splitFloat = new float[splitParams.Length];
        for (int i = 0; i < splitParams.Length; i++)
        {
            splitFloat[i] = float.Parse(splitParams[i]);
        }

        return splitFloat;
    }       // SplitFloats()

    /*************************************************
     *                    Debug
     *************************************************/
    #region Debug
    // 디버그 로그를 찍어주는 메서드
    public static void Log(string _input)
    {
#if UNITY_EDITOR
        Debug.Log(_input);
#endif
    }

    // 디버그 로그를 찍어주는 메서드
    public static void Log(object _input)
    {
#if UNITY_EDITOR
        Debug.Log(_input);
#endif
    }

    // 디버그 로그를 찍어주는 메서드
    public static void LogWarning(string _input)
    {
#if UNITY_EDITOR

        Debug.LogWarning(_input);
#endif
    }

    // 디버그 로그를 찍어주는 메서드
    public static void LogError(string _input)
    {
#if UNITY_EDITOR
        Debug.LogError(_input);
#endif
    }
    // 디버그 로그를 찍어주는 메서드
    public static void LogErrorFormat(string _input)
    {
#if UNITY_EDITOR
        Debug.LogErrorFormat(_input);
#endif
    }

    // 디버그 로그를 찍어주는 메서드
    public static void LogException(System.Exception _input)
    {
#if UNITY_EDITOR
        Debug.LogException(_input);
#endif
    }
    #endregion

    // 강제로 진행중으로 변경하는 메서드
    public static void DebugQuestInProgress(int id)
    {
        Quest quest = UserDataManager.QuestDictionary[id];
        quest.ChangeState(3);
    }

    // NPC 대화의 이벤트를 실행시켜주는 함수
    public static void ChoiceEvent(int targetID)
    {
        string eventID = Data.GetString(targetID, "Choice1Event");
        int[] ids = SplitIds(eventID);

        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == 0)
            { break; }
            Unit.InProgressQuestByID(ids[i]);
        }
    }

    public static void MeshTangentsCalculator(Mesh theMesh)
    {

        int vertexCount = theMesh.vertexCount;
        Vector3[] vertices = theMesh.vertices;
        Vector3[] normals = theMesh.normals;
        Vector2[] texcoords = theMesh.uv;
        int[] triangles = theMesh.triangles;
        int triangleCount = triangles.Length / 3;

        Vector4[] tangents = new Vector4[vertexCount];
        Vector3[] tan1 = new Vector3[vertexCount];
        Vector3[] tan2 = new Vector3[vertexCount];

        int tri = 0;

        for (int i = 0; i < (triangleCount); i++)
        {

            int i1 = triangles[tri];
            int i2 = triangles[tri + 1];
            int i3 = triangles[tri + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            Vector2 w1 = texcoords[i1];
            Vector2 w2 = texcoords[i2];
            Vector2 w3 = texcoords[i3];

            float x1 = v2.x - v1.x;
            float x2 = v3.x - v1.x;
            float y1 = v2.y - v1.y;
            float y2 = v3.y - v1.y;
            float z1 = v2.z - v1.z;
            float z2 = v3.z - v1.z;

            float s1 = w2.x - w1.x;
            float s2 = w3.x - w1.x;
            float t1 = w2.y - w1.y;
            float t2 = w3.y - w1.y;

            float r = 1.0f / (s1 * t2 - s2 * t1);
            Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan1[i1] += sdir;
            tan1[i2] += sdir;
            tan1[i3] += sdir;

            tan2[i1] += tdir;
            tan2[i2] += tdir;
            tan2[i3] += tdir;

            tri += 3;

        }



        for (int i = 0; i < (vertexCount); i++)
        {
            Vector3 n = normals[i];
            Vector3 t = tan2[i];

            // Gram-Schmidt orthogonalize
            Vector3.OrthoNormalize(ref n, ref t);

            tangents[i].x = -t.x;
            tangents[i].y = t.y;
            tangents[i].z = t.z;

            // Calculate handedness
            tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
        }

        theMesh.tangents = tangents;

    }

    public static float DBToLinear(float dB)
    {
        float linear = Mathf.Log10(dB) * 20;
        return linear;
    }
}   // ClassEnd
