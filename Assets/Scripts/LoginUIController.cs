using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
public class LoginUIController : MonoBehaviour, LoopScrollDataSource, LoopScrollPrefabSource
{
    const string LOGIN_ANIMATION_NAME = "Login";
    const string NUMBER_ROOT_NAME = "numbers";
    const string POP_IN_ANIMATION_NAME = "PopInOut";
    const string SCALE_IN_ANIMATION_NAME = "ScaleInOut";
    public Sprite fatAsa, plate, kingAsa;
    public GameObject gameObjectHighScore, gameObjectCream, gameObjectSecret, buttonSecret;
    public GameObject gameObjectHow, gameObjectCredit, gameObjectNext, gameObjectPrev, gameObjectLeaderboard;
    public GameObject gameObjectNoData;
    public SpriteRenderer spriteRendererCake, spriteRendererAsa;
    public Sprite[] numberSprites;
    public Transform platePosition;
    public Sprite[] howToSprites;
    public Image howToImage;
    public GameObject scrollItem;

    Animation ani, secretAni, howAni, creditAni, leaderboardAni;
    Image[] numberImages;
    int page;
    Stack<Transform> pool = new Stack<Transform>();
    LoopScrollRect scrollRect;
    LootLockerLeaderboardMember[] leaderboardDatas;
    bool canClick;
    static LoginUIController instance;

    public static LoginUIController Instance
    {
        get
        {
            //if (instance == null)
            return instance;
        }
    }
    
    public void ButtonEvent(string name)
    {
        if (!canClick)
            return;

        AudioManager.Instance.PlaySound(EAudioClipKind.BUTTON);

        switch (name)
        {
            case "Start":
                SetCanClick(false);
                GameManager.Instance.ChangeState(EGameState.READY);
                break;
            case "Secret":
                gameObjectSecret.SetActive(true);
                secretAni[POP_IN_ANIMATION_NAME].speed = 1;
                secretAni[POP_IN_ANIMATION_NAME].time = 0;
                secretAni.Play(POP_IN_ANIMATION_NAME);
                break;
            case "How":
                gameObjectHow.SetActive(true);
                page = 0;
                ChangePage(0);
                howAni[SCALE_IN_ANIMATION_NAME].speed = 1;
                howAni[SCALE_IN_ANIMATION_NAME].time = 0;
                howAni.Play(SCALE_IN_ANIMATION_NAME);
                break;
            case "Credit":
                gameObjectCredit.SetActive(true);
                creditAni[SCALE_IN_ANIMATION_NAME].speed = 1;
                creditAni[SCALE_IN_ANIMATION_NAME].time = 0;
                creditAni.Play(SCALE_IN_ANIMATION_NAME);
                break;
            case "Close":                
                secretAni[POP_IN_ANIMATION_NAME].speed = -1;
                secretAni[POP_IN_ANIMATION_NAME].time = secretAni[POP_IN_ANIMATION_NAME].length;
                secretAni.Play(POP_IN_ANIMATION_NAME);
                break;
            case "CloseHowTo":
                howAni[SCALE_IN_ANIMATION_NAME].speed = -1;
                howAni[SCALE_IN_ANIMATION_NAME].time = howAni[SCALE_IN_ANIMATION_NAME].length;
                howAni.Play(SCALE_IN_ANIMATION_NAME);
                break;
            case "CloseCredit":
                creditAni[SCALE_IN_ANIMATION_NAME].speed = -1;
                creditAni[SCALE_IN_ANIMATION_NAME].time = creditAni[SCALE_IN_ANIMATION_NAME].length;
                creditAni.Play(SCALE_IN_ANIMATION_NAME);
                break;
            case "CloseLeaderboard":
                leaderboardAni[SCALE_IN_ANIMATION_NAME].speed = -1;
                leaderboardAni[SCALE_IN_ANIMATION_NAME].time = leaderboardAni[SCALE_IN_ANIMATION_NAME].length;
                leaderboardAni.Play(SCALE_IN_ANIMATION_NAME);
                break;
            case "Next":
                ChangePage(1);
                break;
            case "Prev":
                ChangePage(-1);
                break;
            case "Rank":
                ShowLeaderBoard();
                break;
        }
    }

    public void ShowLeaderBoard(int scrollIndex = 0)
    {
        bool needShow = !gameObjectLeaderboard.activeSelf;
        if (needShow)
            gameObjectLeaderboard.SetActive(true);
        SetCanClick(false);
        GameManager.Instance.GetLeaderBoardDatas((LootLockerLeaderboardMember[] datas) => {
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
    // Implement your own Cache Pool here. The following is just for example.
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

    public void SetHighScore(int score)
    {
        bool isShow = score != 0;
        if (gameObjectHighScore.activeSelf != isShow)
            gameObjectHighScore.SetActive(isShow);
        if (isShow)
        {
            for (int i = 0; i < numberImages.Length; i++)
            {
                numberImages[i].sprite = numberSprites[score % 10];
                score = Mathf.FloorToInt((float)score / 10.0f);
            }
        }
    }

    public void SetPlayed()
    {        
        if (!gameObjectCream.activeSelf)
            gameObjectCream.SetActive(true);
    }
    public void SetUnlockSecret()
    {
        spriteRendererAsa.sprite = kingAsa;
        if (!buttonSecret.activeSelf)
            buttonSecret.SetActive(true);
    }

    public void SetFull()
    {
        spriteRendererAsa.sprite = fatAsa;
        spriteRendererCake.sprite = plate;
        spriteRendererCake.transform.localPosition = platePosition.localPosition;
    }
    public void PlayOpenAnimation()
    {
        ani.Play(LOGIN_ANIMATION_NAME);
    }
   
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("login uicontroller");
        instance = this;
        ani = GetComponent<Animation>();
        secretAni = gameObjectSecret.GetComponent<Animation>();
        creditAni = gameObjectCredit.GetComponent<Animation>();
        howAni = gameObjectHow.GetComponent<Animation>();
        leaderboardAni = gameObjectLeaderboard.GetComponent<Animation>();

        gameObjectSecret.GetComponent<AnimationEventListener>().sender += AnimationEvent;
        gameObjectCredit.GetComponent<AnimationEventListener>().sender += AnimationEvent;
        gameObjectHow.GetComponent<AnimationEventListener>().sender += AnimationEvent;
        gameObjectLeaderboard.GetComponent<AnimationEventListener>().sender += AnimationEvent;

        Transform trans = gameObjectHighScore.transform.Find(NUMBER_ROOT_NAME);
        numberImages = new Image[trans.childCount];
        int j = 0;
        for (int i = trans.childCount; i > 0; i--)
        {
            numberImages[j] = trans.GetChild(i - 1).GetComponent<Image>();
            j++;
        }

        gameObjectCream.SetActive(false);
        gameObjectSecret.SetActive(false);
        buttonSecret.SetActive(false);
        gameObjectHow.SetActive(false);
        gameObjectCredit.SetActive(false);
        gameObjectLeaderboard.SetActive(false);
        SetCanClick(true);
    }
    
    public void SetCanClick(bool _bool)
    {
        canClick = _bool;
    }

    void Start()
    {
        scrollRect = gameObjectLeaderboard.GetComponentInChildren<LoopScrollRect>();
        scrollRect.dataSource = this;
        scrollRect.prefabSource = this;
    }
    void AnimationEvent(string name)
    {
        if ((name == "start") && (secretAni[POP_IN_ANIMATION_NAME].speed == -1))
        {
            gameObjectSecret.SetActive(false);
        }
        if ((name == "startScale"))
        {
            if (howAni[SCALE_IN_ANIMATION_NAME].speed == -1 && gameObjectHow.activeSelf)
                gameObjectHow.SetActive(false);
            else if(creditAni[SCALE_IN_ANIMATION_NAME].speed == -1 && gameObjectCredit.activeSelf)
                gameObjectCredit.SetActive(false);
            else if (leaderboardAni[SCALE_IN_ANIMATION_NAME].speed == -1 && gameObjectLeaderboard.activeSelf)
                gameObjectLeaderboard.SetActive(false);
        }
    }
    void ChangePage(int addPage)
    {
        page += addPage;
        bool isShow = page != howToSprites.Length - 1;
        if (isShow != gameObjectNext.activeSelf)
            gameObjectNext.SetActive(isShow);

        isShow = page != 0;
        if (isShow != gameObjectPrev.activeSelf)
            gameObjectPrev.SetActive(isShow);
        howToImage.sprite = howToSprites[page];
    }

}
