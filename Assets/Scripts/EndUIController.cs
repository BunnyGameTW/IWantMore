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
    public GameObject gameObjectSecret, gameObjectLeaderboard, gameObjectName, gameObjectNoData, gameObjectAlert;
    public GameObject gameObjectLoading;
    public InputField inputFieldName;
    public GameObject scrollItem;

    Image[] scoreImages;
    Animation secretAni, leaderboardAni, alertAni;
    Stack<Transform> pool = new Stack<Transform>();
    LoopScrollRect scrollRect;
    LootLockerLeaderboardMember[] leaderboardDatas;
    bool canClick;
    Text leaderboardTitle;
    Text[] secretTexts;
    const string POP_IN_ANIMATION_NAME = "PopInOut";
    const string SCALE_IN_ANIMATION_NAME = "ScaleInOut";
    const int MAX_RANKING_NUMBER = 100;
    string[] randomNames =
   {
        "ILoveAsa",
        "火神的受保人",
        "晚薩好",
        "早薩好",        
        "薩氣の子",
        "想不到要取什麼",
        "Im颯颯子",
        "HelloWorld",
        "等等要吃啥",
        "生快",
        "OAO",
        "OvO",
        "Owo",        
    };
    const string RULE_TEXT_ROOT_NAME = "GameObjectTextRoot";
    string[] SECRET_TEXTS =
    {
        "恭喜你！", "    我們很榮幸的通知你\n由於你精采的表現，你已經獲得稱號：\n\"<b><color=#EF6361>蛋糕</color>之<color=#FE9B0B>王</color></b>\"\n回首頁確認看看吧\n\n感謝遊玩!\\(oxo", "Ⓒ蛋糕株式會社",
        "Congratulations!", "        We are delighted to inform you\ndue to your amazing work\nyou have gain the title\n\"<b>THE <color=#FE9B0B>KING</color> OF <color=#EF6361>CAKE</color></b>\"\ngive it a look at home page!\n\nThank you for playing this game \\(oxo", "ⒸCake.Inc",
    };
    string[] LEADERBOARD_TITLE_TEXT =
   {
        "排行榜",
        "Leaderboard"
    };

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
        alertAni = gameObjectAlert.GetComponent<Animation>();
        gameObjectSecret.SetActive(false);
        gameObjectLeaderboard.SetActive(false);
        gameObjectName.SetActive(false);
        leaderboardTitle = gameObjectLeaderboard.GetComponentInChildren<Text>();
        
    }

    void InitSecretTexts()
    {
        Transform t = gameObjectSecret.transform.Find("Image/" + RULE_TEXT_ROOT_NAME);
        secretTexts = new Text[t.childCount];
        for (int i = 0; i < t.childCount; i++)
        {
            secretTexts[i] = t.GetChild(i).GetComponent<Text>();
        }
    }

    void UpdateSecret(ELanguage e)
    {
        int index = e == ELanguage.CN ? 0 : secretTexts.Length;
        for (int i = 0; i < secretTexts.Length; i++)
        {
            secretTexts[i].text = SECRET_TEXTS[index];
            index++;
        }
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
        InitSecretTexts();
        UpdateSecret(GameManager.Instance.GetLanguage());
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

   

    string GetRandomName()
    {
        int v = Random.Range(0, randomNames.Length);
        return randomNames[v];
    }

    public void ButtonEvent(string name)
    {
        if (!canClick)
            return;

        AudioManager.Instance.PlaySound(EAudioClipKind.BUTTON);

        switch (name)
        {
            case "Retry":
                SetCanClick(false);
                GameManager.Instance.ChangeState(EGameState.READY);
                break;
            case "Home":
                SetCanClick(false);
                GameManager.Instance.ChangeState(EGameState.TITLE);
                break;
            case "Upload":
                string _name = GameManager.Instance.GetPlayerName();
                if (_name == "")
                {
                    gameObjectName.SetActive(true);
                    if (GameManager.Instance.GetLanguage() == ELanguage.CN)
                    {
                        gameObjectName.transform.Find("TextTitle").GetComponent<Text>().text = "設定名字";
                        inputFieldName.placeholder.GetComponent<Text>().text = "輸入名字\n(最多十個字，之後不能再更改)";
                    }
                    if (GameManager.Instance.CheckIfMobile())
                    {
                        inputFieldName.text = GetRandomName();
                    }
                }
                else
                {
                    SetCanClick(false);
                    gameObjectLoading.SetActive(true);
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
            case "Name":
                if (inputFieldName.text != "")
                {
                    GameManager.Instance.SetPlayerName(inputFieldName.text);
                    gameObjectName.SetActive(false);
                    gameObjectLoading.SetActive(true);
                    SetCanClick(false);
                    GameManager.Instance.SubmitScore((response) =>
                    {
                        if (response.rank > MAX_RANKING_NUMBER)
                            response.rank = MAX_RANKING_NUMBER;
                        ShowLeaderBoard(response.rank);
                    });
                }
                else
                {
                    gameObjectAlert.SetActive(true);
                    alertAni.Play();
                }
                break;
        }
    }

    public void ShowLeaderBoard(int scrollIndex = 0)
    {
        bool needShow = !gameObjectLeaderboard.activeSelf;
        if (needShow)
        {
            leaderboardTitle.text = LEADERBOARD_TITLE_TEXT[(int)GameManager.Instance.GetLanguage()];
            gameObjectLeaderboard.SetActive(true);
        }

        
        GameManager.Instance.GetLeaderBoardDatas((LootLockerLeaderboardMember[] datas) => {
            gameObjectLoading.SetActive(false);
            SetCanClick(true);
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
