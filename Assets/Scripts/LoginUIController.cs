using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
public class LoginUIController : MonoBehaviour
{
    const string LOGIN_ANIMATION_NAME = "Login";
    const string NUMBER_ROOT_NAME = "numbers";
    const string POP_IN_ANIMATION_NAME = "PopInOut";
    const string SCALE_IN_ANIMATION_NAME = "ScaleInOut";
    public Sprite fatAsa, plate, kingAsa;
    public GameObject gameObjectHighScore, gameObjectCream, gameObjectSecret, buttonSecret;
    public GameObject gameObjectHow, gameObjectCredit, gameObjectNext, gameObjectPrev;
    public SpriteRenderer spriteRendererCake, spriteRendererAsa;
    public Sprite[] numberSprites;
    public Transform platePosition;
    public Sprite[] howToSprites;
    public Image howToImage;

    Animation ani, secretAni, howAni, creditAni;
    Image[] numberImages;
    int page;
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
        AudioManager.Instance.PlaySound(EAudioClipKind.BUTTON);

        switch (name)
        {
            case "Start":
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
                creditAni[SCALE_IN_ANIMATION_NAME].time = howAni[SCALE_IN_ANIMATION_NAME].length;
                creditAni.Play(SCALE_IN_ANIMATION_NAME);
                break;
            case "Next":
                ChangePage(1);
                break;
            case "Prev":
                ChangePage(-1);
                break;
            case "Rank":
                //TODO scrollrect
                LootLockerLeaderboardMember [] datas = GameManager.Instance.GetLeaderBoardDatas();
                for (int i = 0; i < datas.Length; i++)
                {

                }
                break;
        }
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

        gameObjectSecret.GetComponent<AnimationEventListener>().sender += AnimationEvent;
        gameObjectCredit.GetComponent<AnimationEventListener>().sender += AnimationEvent;
        gameObjectHow.GetComponent<AnimationEventListener>().sender += AnimationEvent;

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
