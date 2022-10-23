using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
public class LeaderboardController : MonoBehaviour
{
    const int leaderboardID = 8078;
    const int MAX_RANKING_NUMBER = 100;

    int memberId = 0;
    public string playerName { get; set; }
    LootLockerLeaderboardMember[] scores;
    // Start is called before the first frame update
    void Start()
    {
        playerName = PlayerPrefs.GetString("PlayerName");

        ShowScores((LootLockerLeaderboardMember[] _scores) => {
            for (int i = 0; i < _scores.Length; i++)
            {
                Debug.Log(_scores[i].rank + ", " + _scores[i].member_id + ", " + _scores[i].score);//", " + scores[i].player.name +
            }
        });
        //CheckSetName();
        //SubmitScore("BunnyGame", 100);
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
