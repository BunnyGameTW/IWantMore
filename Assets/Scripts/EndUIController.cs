using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;

public class EndUIController : MonoBehaviour, LoopScrollDataSource, LoopScrollPrefabSource
{
    public Sprite[] numberSprites;
    public GameObject gameObjectNewRecord;
    public GameObject scoreRoot;
    public GameObject gameObjectSecret, gameObjectLeaderboard, gameObjectName, gameObjectNoData;
    public InputField inputFieldName;
    public GameObject scrollItem;

    Image[] scoreImages;
    Animation secretAni, leaderboardAni;
    Stack<Transform> pool = new Stack<Transform>();
    LoopScrollRect scrollRect;
    LootLockerLeaderboardMember[] leaderboardDatas;
    bool canClick;

    const string POP_IN_ANIMATION_NAME = "PopInOut";
    const string SCALE_IN_ANIMATION_NAME = "ScaleInOut";
    const int MAX_RANKING_NUMBER = 100;

    static EndUIController instance;
    public static EndUIController Instance
    {
        get
        {
            //if (instance == null)
            return instance;
        }
    }
    void Start()
    {
        scrollRect = gameObjectLeaderboard.GetComponentInChildren<LoopScrollRect>();
        scrollRect.dataSource = this;
        scrollRect.prefabSource = this;
    }

    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("end ui controller");
        instance = this;
        scoreImages = new Image[scoreRoot.transform.childCount];
        int j = 0;
        for (int i = scoreRoot.transform.childCount; i > 0; i--)
        {
            scoreImages[j] = scoreRoot.transform.GetChild(i - 1).GetComponent<Image>();
            j++;
        }
        gameObjectSecret.GetComponent<AnimationEventListener>().sender += AnimationEvent;
        gameObjectLeaderboard.GetComponent<AnimationEventListener>().sender += AnimationEvent;
        secretAni = gameObjectSecret.GetComponent<Animation>();
        leaderboardAni = gameObjectLeaderboard.GetComponent<Animation>();
        gameObjectSecret.SetActive(false);
        gameObjectLeaderboard.SetActive(false);
        gameObjectName.SetActive(false);
    }

    void AnimationEvent(string name)
    {
        if ((name == "start") && (secretAni[POP_IN_ANIMATION_NAME].speed == -1))
        {
            gameObjectSecret.SetActive(false);
        }
        if ((name == "startScale"))
        {
            if (leaderboardAni[SCALE_IN_ANIMATION_NAME].speed == -1 && gameObjectLeaderboard.activeSelf)
                gameObjectLeaderboard.SetActive(false);
        }
    }

    public void SetUnlockSecret()
    {
        gameObjectSecret.SetActive(true);
        secretAni[POP_IN_ANIMATION_NAME].speed = 1;
        secretAni[POP_IN_ANIMATION_NAME].time = 0;
        secretAni.Play(POP_IN_ANIMATION_NAME);
    }
    
    public void SetCanClick(bool _bool)
    {
        canClick = _bool;
    }
    public void SetScore(int score, bool showNewRecord = false)
    {
        for (int i = 0; i < scoreImages.Length; i++)
        {
            scoreImages[i].sprite = numberSprites[score % 10];
            score = Mathf.FloorToInt((float)score / 10.0f);
        }

        if (gameObjectNewRecord.activeSelf != showNewRecord)
            gameObjectNewRecord.SetActive(showNewRecord);
    }
    public void ButtonEvent(string name)
    {
        if (!canClick)
            return;

        AudioManager.Instance.PlaySound(EAudioClipKind.BUTTON);

        switch (name)
        {
            case "Retry":
                GameManager.Instance.ChangeState(EGameState.READY);
                break;
            case "Home":
                GameManager.Instance.ChangeState(EGameState.TITLE);
                break;
            case "Upload":
                string _name = GameManager.Instance.GetPlayerName();
                if (_name == "")
                {
                    gameObjectName.SetActive(true);
                }
                else
                {
                    GameManager.Instance.SubmitScore((response) => {
                        ShowLeaderBoard(response.rank);
                    });
                }
                break;
            case "End":
                secretAni[POP_IN_ANIMATION_NAME].speed = -1;
                secretAni[POP_IN_ANIMATION_NAME].time = secretAni[POP_IN_ANIMATION_NAME].length;
                secretAni.Play(POP_IN_ANIMATION_NAME);
                break;
            case "CloseLeaderboard":
                leaderboardAni[SCALE_IN_ANIMATION_NAME].speed = -1;
                leaderboardAni[SCALE_IN_ANIMATION_NAME].time = leaderboardAni[SCALE_IN_ANIMATION_NAME].length;
                leaderboardAni.Play(SCALE_IN_ANIMATION_NAME);
                break;
            case "Name"://TODO check faul character?
                Debug.Log("inputFieldName.text->" + inputFieldName.text);
                GameManager.Instance.SetPlayerName(inputFieldName.text);
                gameObjectName.SetActive(false);
                GameManager.Instance.SubmitScore((response) => {
                    if (response.rank > MAX_RANKING_NUMBER)
                        response.rank = MAX_RANKING_NUMBER;
                    ShowLeaderBoard(response.rank);
                });
                break;
        }
    }

    public void ShowLeaderBoard(int scrollIndex = 0)
    {
        bool needShow = !gameObjectLeaderboard.activeSelf;
        if (needShow)
            gameObjectLeaderboard.SetActive(true);

        GameManager.Instance.GetLeaderBoardDatas((LootLockerLeaderboardMember[] datas) => {
            if (needShow)
            {
                leaderboardAni[SCALE_IN_ANIMATION_NAME].speed = 1;
                leaderboardAni[SCALE_IN_ANIMATION_NAME].time = 0;
                leaderboardAni.Play(SCALE_IN_ANIMATION_NAME);
            }
            gameObjectNoData.SetActive(datas.Length == 0);

            leaderboardDatas = datas;
            scrollRect.totalCount = datas.Length;
            scrollRect.RefillCells();
            if (scrollIndex != 0)
                scrollRect.ScrollToCellWithinTime(scrollIndex - 1, 0.5f);
        });
    }
    public GameObject GetObject(int index)
    {
        if (pool.Count == 0)
        {
            return Instantiate(scrollItem);
        }
        Transform candidate = pool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        // Use `DestroyImmediate` here if you don't need Pool
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        pool.Push(trans);
    }

    public void ProvideData(Transform transform, int idx)
    {
        transform.SendMessage("ScrollCellIndex", leaderboardDatas[idx]);
    }
}
