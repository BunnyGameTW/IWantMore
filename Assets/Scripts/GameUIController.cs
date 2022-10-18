using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class GameUIController : MonoBehaviour
{
    const string COMBO_TEXT_IMAGE_NAME = "combo";
    const string COMBO_IMAGE_ROOT_NAME = "comboRoot";
    const string FEVER_TEXT_IMAGE_NAME = "fever";
    const string APPEAR_NAME = "GameUIInOut";

    public GameObject hpRoot, scoreRoot, comboRoot, feverRoot;
    public Sprite hpSprite;
    public Sprite[] numberSprites;
    public Image scoreBarImage;
    public Image countDownImage;
    public Sprite [] countDownNumberSprites;

    public Button retryButton;
    public Button homeButton;
    public Canvas canvas;

    GameObject[] gameObjectHps;
    Animation anim;
    Image[] scoreImages, comboImages;
    Image comboTextImage, feverImage;

    //TODO leaderboard
    
    int maxScore, prevMaxScore, offsetScore;
    float comboTime, maxComboTime;
    float feverTime, maxFeverTime;
    static GameUIController instance;
    public static GameUIController Instance
    {
        get
        {
            //if (instance == null)
            return instance;
        }
    }

    public void InitHp(int hp)
    {
        gameObjectHps = new GameObject[hp];

        GameObject go = hpRoot.transform.GetChild(0).gameObject;
        go.GetComponent<Image>().sprite = hpSprite;
        gameObjectHps[0] = go;

        if (hpRoot.transform.childCount < hp)
        {
            for (int i = 0; i < hp - 1; i++)
            {
                GameObject go2 = Instantiate(go, hpRoot.transform);
                gameObjectHps[i + 1] = go2;
            }
        }

    }
    public void SetHp(int hp)
    {
        for (int i = 0; i < gameObjectHps.Length; i++)
        {
            if (i <= hp - 1)
            {
                if (!gameObjectHps[i].activeSelf)
                    gameObjectHps[i].SetActive(true);
            }
            else
            {
                if (gameObjectHps[i].activeSelf)
                    gameObjectHps[i].SetActive(false);
            }

        }
    }

    
    public void SetDifficultyScore(int _prevMaxScore, int _maxScore)
    {
        prevMaxScore = _prevMaxScore;
        offsetScore = _maxScore - _prevMaxScore;//1 10 100
        maxScore = _maxScore;        
        scoreBarImage.fillAmount = 0;
    }

    public void SetScore(int score)
    {

        if (offsetScore != 0)
            scoreBarImage.fillAmount = (float)((float)(score - prevMaxScore) / (float)offsetScore);

        for (int i = 0; i < scoreImages.Length; i++)
        {
            scoreImages[i].sprite = numberSprites[score % 10];
            score = Mathf.FloorToInt((float)score / 10.0f);
        }
    }

    public void SetCombo(int combo, float _comboTime = 0)
    {
        if (combo == 0)
        {
            if (comboRoot.gameObject.activeSelf)
            {
                comboRoot.gameObject.SetActive(false);            
            }
        }
        else
        {            
            for (int i = 0; i < comboImages.Length; i++)
            {
                comboImages[i].sprite = numberSprites[combo % 10];
                combo = Mathf.FloorToInt((float)combo / 10.0f);
            }

            comboTextImage.fillAmount = 1;
            comboTime = _comboTime;
            maxComboTime = _comboTime;

            if (!comboRoot.gameObject.activeSelf)
            {
                comboRoot.gameObject.SetActive(true);                
            }
        }
    }

    
    public void SetFever(bool isFever, float _feverTime = 0)
    {
        feverRoot.SetActive(isFever);       
        if (isFever)
        {
            feverImage.fillAmount = 1;
            feverTime = _feverTime;
            maxFeverTime = _feverTime;
        }
            
    }
    
    public void SetCountDown(int time)
    {
        bool isShow = time != 0;
        if (isShow != countDownImage.gameObject.activeSelf)
            countDownImage.gameObject.SetActive(isShow);
        if (isShow)
            countDownImage.sprite = countDownNumberSprites[time - 1];
    }
    public void Reset()
    {        
        SetDifficultyScore(0, 0);
        SetScore(0);
        SetCombo(0);
        SetFever(false);        
    }

    //TODO use animation replace active
    //TODO prevState?
    public void SetState(EGameState state)
    {
        switch (state)
        {
            case EGameState.TITLE:
                retryButton.gameObject.SetActive(false);
                homeButton.gameObject.SetActive(false);
                anim[APPEAR_NAME].speed = 1;
                anim[APPEAR_NAME].time = 0;
                anim.Play(APPEAR_NAME);
                break;
            case EGameState.READY:
                retryButton.gameObject.SetActive(false);
                homeButton.gameObject.SetActive(false);
                anim[APPEAR_NAME].speed = -1;
                anim[APPEAR_NAME].time = anim[APPEAR_NAME].length;
                anim.Play(APPEAR_NAME);
                break;
            case EGameState.GAME:
                break;
            case EGameState.END:
                //TODO show end and play zoom in animation
                retryButton.gameObject.SetActive(true);
                homeButton.gameObject.SetActive(true);                
                break;
        }
    }
 
    void Awake()
    {
        Debug.Log("game ui controller");
        instance = this;
        anim = canvas.GetComponent<Animation>();


        scoreImages = new Image[scoreRoot.transform.childCount];
        int j = 0;
        for (int i = scoreRoot.transform.childCount; i > 0; i--)
        {
            scoreImages[j] = scoreRoot.transform.GetChild(i - 1).GetComponent<Image>();
            j++;
        }

        comboTextImage = comboRoot.transform.Find(COMBO_TEXT_IMAGE_NAME).GetComponent<Image>();

        Transform trans = comboRoot.transform.Find(COMBO_IMAGE_ROOT_NAME);
        comboImages = new Image[trans.transform.childCount];
        j = 0;
        for (int i = trans.childCount; i > 0; i--)
        {
            comboImages[j] = trans.GetChild(i - 1).GetComponent<Image>();
            j++;
        }
        feverImage = feverRoot.transform.Find(FEVER_TEXT_IMAGE_NAME).GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (comboTime > 0)
        {
            comboTime -= Time.deltaTime;
            comboTextImage.fillAmount = comboTime / maxComboTime;            
            if (comboTime <= 0 && comboRoot.activeSelf)
            {
                comboRoot.SetActive(false);
            }
        }
        if (feverTime > 0)
        {
            feverTime -= Time.deltaTime;
            feverImage.fillAmount = feverTime / maxFeverTime;
            if (feverTime <= 0 && feverRoot.activeSelf)
            {
                feverRoot.SetActive(false);
            }
        }

    }
    public void ButtonEvent(string name)
    {
        switch (name)
        {
            case "Retry":
                GameManager.Instance.ChangeState(EGameState.READY);
                break;
            case "Home":
                GameManager.Instance.ChangeState(EGameState.TITLE);
                break;
        }       
    }
}
