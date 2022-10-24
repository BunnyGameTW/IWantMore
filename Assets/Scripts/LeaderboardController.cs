using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
public class LeaderboardController : MonoBehaviour
{
    const int leaderboardID = 8078;
    const int MAX_RANKING_NUMBER = 100;

    int memberId = 0;
    string playerName;
    LootLockerLeaderboardMember[] scores;
    // Start is called before the first frame update
    void Start()
    {
        playerName = PlayerPrefs.GetString("PlayerName");
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
        PlayerPrefs.SetString("PlayerName", name);
    }
    
    public void SubmitScore(int score, System.Action<LootLockerSubmitScoreResponse> callback)
    {
        if (memberId == 0) 
        {
            StartCoroutine(StartSession(() => {
                LootLockerSDKManager.SubmitScore(playerName, score, leaderboardID, callback);
            }));
        }
        else
        {
            LootLockerSDKManager.SubmitScore(playerName, score, leaderboardID, callback);
        }
    }

    public void ShowScores(System.Action<LootLockerLeaderboardMember[]> callback)
    {
        if (memberId == 0)
            StartCoroutine(StartSession(() => {
                if (memberId == 0)
                {
                    Debug.LogError("network error?");
                    return;
                }
                RequestGetScore(callback);
            }));
        else
            RequestGetScore(callback);
    }
    
   
    void RequestGetScore(System.Action<LootLockerLeaderboardMember[]> callback)
    {
        LootLockerSDKManager.GetScoreList(leaderboardID, MAX_RANKING_NUMBER, (response) =>
        {
            if (response.success)
            {
                scores = response.items;
                Debug.Log("ScoreList: " + response.items.Length);
            }
            else
            {
                Debug.Log("failed: " + response.Error);
            }
            callback(scores);
        });
    }

    IEnumerator StartSession(System.Action callback)
    {
        bool done = false;
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (!response.success)
            {
                Debug.Log("error starting LootLocker session");
            }
            else
            {
                Debug.Log("successfully started LootLocker session->" + response.player_id);
                memberId = response.player_id;
            }
            done = true;
        });
        yield return new WaitUntil(() => done == true);
        callback();
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
