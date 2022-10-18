using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class LoginUIController : MonoBehaviour
{
    const string LOGIN_ANIMATION_NAME = "Login";
    const string NUMBER_ROOT_NAME = "numbers";

    public Sprite fatAsa, plate;
    public GameObject gameObjectHighScore, gameObjectCream;
    public SpriteRenderer spriteRendererCake, spriteRendererAsa;
    public Sprite[] numberSprites;
    public Transform platePosition;

    Animation ani;
    bool isFirstOpen = true;
    Image[] numberImages;

    public void ButtonEvent(string name)
    {
        switch (name)
        {
            case "Start":
                GameManager.Instance.ChangeState(EGameState.READY);
                break;
        }
    }

    public void SetHighScore(int score)
    {
        bool isShow = score != 0;
        if (gameObjectHighScore.activeSelf != isShow)
            gameObjectHighScore.SetActive(isShow);
        //TODO set score
        //
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
        gameObjectCream.SetActive(true);
    }

    public void SetFull()
    {
        spriteRendererAsa.sprite = fatAsa;
        spriteRendererCake.sprite = plate;
        spriteRendererCake.transform.localPosition = platePosition.localPosition;
    }

  
    // Start is called before the first frame update
    void Start()
    {
        ani = GetComponent<Animation>();
        if (isFirstOpen)
        {
            isFirstOpen = false;
            ani.Play(LOGIN_ANIMATION_NAME);//TODO ¼½§¹¤~¯à«ö?
        }

        Transform trans = gameObjectHighScore.transform.Find(NUMBER_ROOT_NAME);
        numberImages = new Image[trans.childCount];
        int j = 0;
        for (int i = trans.childCount; i > 0; i--)
        {
            numberImages[j] = trans.GetChild(i - 1).GetComponent<Image>();
            j++;
        }
        gameObjectCream.SetActive(false);

        //TODO check high score
        SetHighScore(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
