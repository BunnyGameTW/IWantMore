using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class EndUIController : MonoBehaviour
{
    public Sprite[] numberSprites;
    public GameObject gameObjectNewRecord;
    public GameObject scoreRoot;
    public GameObject gameObjectSecret;
    Image[] scoreImages;
    Animation secretAni;
    const string POP_IN_ANIMATION_NAME = "PopInOut";

    static EndUIController instance;
    public static EndUIController Instance
    {
        get
        {
            //if (instance == null)
            return instance;
        }
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
        secretAni = gameObjectSecret.GetComponent<Animation>();
        gameObjectSecret.SetActive(false);
    }

    void AnimationEvent(string name)
    {
        if ((name == "start") && (secretAni[POP_IN_ANIMATION_NAME].speed == -1))
        {
            gameObjectSecret.SetActive(false);
        }
    }

    public void SetUnlockSecret()
    {
        gameObjectSecret.SetActive(true);
        secretAni[POP_IN_ANIMATION_NAME].speed = 1;
        secretAni[POP_IN_ANIMATION_NAME].time = 0;
        secretAni.Play(POP_IN_ANIMATION_NAME);
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
                //TODO show input field & upload to firebase & show leaderboard
                break;
            case "End":
                secretAni[POP_IN_ANIMATION_NAME].speed = -1;
                secretAni[POP_IN_ANIMATION_NAME].time = secretAni[POP_IN_ANIMATION_NAME].length;
                secretAni.Play(POP_IN_ANIMATION_NAME);

                break;
        }
    }
}
