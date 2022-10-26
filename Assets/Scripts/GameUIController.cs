using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
public class GameUIController : MonoBehaviour
{
    const string COMBO_TEXT_IMAGE_NAME = "combo";
    const string COMBO_IMAGE_ROOT_NAME = "comboRoot";
    const string COMBO_IMAGE_ROOT_BG_NAME = "comboRootBg";
    const string FEVER_TEXT_IMAGE_NAME = "fever";
    const float COMBO_TEXT_WIDTH_RATIO = 0.55f;
    const float COMBO_DIGIT_RATIO = 0.15f;
   
    public GameObject hpRoot, scoreRoot, comboRoot, feverRoot;
    public Sprite hpSprite;
    public Sprite[] numberSprites;
    public Image scoreBarImage, delayBarImage;
    public Image countDownImage;
    public Sprite [] countDownNumberSprites;

    public Canvas canvas;
    public MMF_Player feedbackFever;
    public MMF_Player feedbackHit;
    public MMF_Player feedbackDie;

    GameObject[] gameObjectHps;
    MMF_Feedback[] feedbackHps, feedbackDies;
    MMF_Player feedbackCountDown;
    Image[] scoreImages, comboImages, comboBgImages;
    Image comboTextImage, feverImage;    
    
    int maxScore, prevMaxScore, offsetScore;
    float comboTime, maxComboTime;
    float feverTime, maxFeverTime;
    float[] HEARTBEAT_RATE = {
        0.2f, 0.5f, 1.0f
    };
    float hearbeatTimer;
    int tempHp, index;

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
        feedbackHps = new MMF_Feedback[hp];
        feedbackDies = new MMF_Feedback[hp];
        GameObject go = hpRoot.transform.GetChild(0).gameObject;
        go.GetComponent<Image>().sprite = hpSprite;
        
        gameObjectHps[0] = go;
        List<MMF_Feedback> list = go.GetComponent<MMF_Player>().FeedbacksList;
        feedbackHps[0] = list[0];
        feedbackDies[0] = list[1];

        if (hpRoot.transform.childCount < hp)
        {
            for (int i = 0; i < hp - 1; i++)
            {
                GameObject go2 = Instantiate(go, hpRoot.transform);
                gameObjectHps[i + 1] = go2;
                list = go2.GetComponent<MMF_Player>().FeedbacksList;

                feedbackHps[i + 1] = list[0];
                feedbackDies[i + 1] = list[1];
            }
        }
    }
    public void ResetHp()
    {
        tempHp = 0;
    }

    public void SetHp(int hp)
    {
        int offset = tempHp - hp;
        if (offset > 0)
        {
            feedbackDies[hp].Play(Vector3.zero);
        }
        else
        {
            for (int i = 0; i < hp - 1; i++)
            {
                gameObjectHps[i].transform.localScale = new Vector3(1, 1);
            }
        }

        tempHp = hp;
    }

    
    public void SetDifficultyScore(int _prevMaxScore, int _maxScore)
    {
        prevMaxScore = _prevMaxScore;
        offsetScore = _maxScore - _prevMaxScore;//1 10 100
        //maxScore = _maxScore;        
        scoreBarImage.fillAmount = 0;
        delayBarImage.fillAmount = 0;
        isStartAddBar = false;
        fAddAmount = 0;
    }

    public void SetScore(int score)
    {

        if (offsetScore != 0)
        {//TODO
            //scoreBarImage.fillAmount = (float)((float)(score - prevMaxScore) / (float)offsetScore);
            delayBarImage.fillAmount = (float)((float)(score - prevMaxScore) / (float)offsetScore);
            StartCoroutine(AddScoreBar(delayBarImage.fillAmount));
            //scoreProgressBar.AddPercent(0.1f);
        }

        for (int i = 0; i < scoreImages.Length; i++)
        {
            scoreImages[i].sprite = numberSprites[score % 10];
            score = Mathf.FloorToInt((float)score / 10.0f);
        }
    }

    bool isStartAddBar = false;
    float fAddAmount = 0; 
    IEnumerator AddScoreBar(float fillAmount)
    {
        yield return new WaitForSeconds(0.3f);
        fAddAmount = fillAmount;
        isStartAddBar = true;
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
                int v = combo % 10;
                comboImages[i].sprite = numberSprites[v];
                comboBgImages[i].sprite = numberSprites[v];
                combo = Mathf.FloorToInt((float)combo / 10.0f);

                comboImages[i].fillAmount = 1;
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
            feedbackFever.PlayFeedbacks();
        else
            feedbackFever.StopFeedbacks();

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
        {
            countDownImage.sprite = countDownNumberSprites[time - 1];
            feedbackCountDown.PlayFeedbacks();
        }
    }
    public void Reset()
    {
        index = 0;
        hearbeatTimer = 0;
        //SetDifficultyScore(0, 0);
        SetScore(0);
        SetCombo(0);
        SetFever(false);        
    }

    void Awake()
    {
        Debug.Log("game ui controller");
        instance = this;

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

        trans = comboRoot.transform.Find(COMBO_IMAGE_ROOT_BG_NAME);
        comboBgImages = new Image[trans.transform.childCount];
        j = 0;
        for (int i = trans.childCount; i > 0; i--)
        {
            comboBgImages[j] = trans.GetChild(i - 1).GetComponent<Image>();
            j++;
        }

        feverImage = feverRoot.transform.Find(FEVER_TEXT_IMAGE_NAME).GetComponent<Image>();
        feedbackCountDown = countDownImage.GetComponent<MMF_Player>();
        feedbackCountDown.Initialization();
        scoreProgressBar = scoreBarImage.GetComponent<MMProgressBar>();
    }
    // Update is called once per frame
    MMProgressBar scoreProgressBar;
    void Update()
    {
        if (comboTime > 0)
        {
            comboTime -= Time.deltaTime;
            float r = comboTime / maxComboTime;
            if (r < COMBO_TEXT_WIDTH_RATIO)
                comboTextImage.fillAmount = r / COMBO_TEXT_WIDTH_RATIO;
            for (int i = 0; i < comboImages.Length; i++)
            {
                float v = 1.0f - (COMBO_DIGIT_RATIO * i);
                float v2 = 1.0f - (COMBO_DIGIT_RATIO * (i + 1));
                if (r < v && r > v2)//0.7 + 0.1 * (i + 1)))
                    comboImages[i].fillAmount = (r - v2) / COMBO_DIGIT_RATIO;
            }
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

        if (tempHp > 0)
        {
            hearbeatTimer += Time.deltaTime;
            
            if (hearbeatTimer >= HEARTBEAT_RATE[tempHp - 1])
            {
                if (index <= tempHp - 1)
                    feedbackHps[index].Play(Vector3.zero);
                index++;
                if (index >= feedbackHps.Length)
                    index = 0;
                hearbeatTimer = 0;
            }
        }

        if (isStartAddBar)
        {
            scoreBarImage.fillAmount += Time.deltaTime * fAddAmount;//1
            if (scoreBarImage.fillAmount >= fAddAmount)
            {
                scoreBarImage.fillAmount = fAddAmount;
                isStartAddBar = false;
                fAddAmount = 0;

                //TODO tysm!
                if (scoreBarImage.fillAmount == 1)
                {
                    isStartAddBar = true;
                    fAddAmount = delayBarImage.fillAmount;
                    scoreBarImage.fillAmount = 0;
                }
            }
        }
    }
  
}
