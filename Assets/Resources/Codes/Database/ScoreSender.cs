using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ScoreSender : MonoBehaviour
{
    public IEnumerator SendScore(string player, float time)
    {
        string json = JsonUtility.ToJson(new Score(player, time));

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:5000/scoreboard", json, "application/json"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("Erro: " + www.error);
            else
                Debug.Log("Enviado com sucesso!");
        }
    }
}

[System.Serializable]
public class Score
{
    public string PlayerName;
    public float Time;

    public Score(string playerName, float time)
    {
        PlayerName = playerName;
        Time = time;
    }
}
