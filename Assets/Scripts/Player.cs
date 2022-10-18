using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EPlayerState
{
    NORMAL = 1,
    STOP = 2
}

public class Player : MonoBehaviour
{
    public GameObject hand;
    public Transform posR, posL;
    public int hp;

    SpriteRenderer sp, bodySp;
    Collider2D col, bodyCol;

    bool prevFaceR;
    float angle;
    float currentVelocity;
    bool isMoving;
    float nowScale;
    int nowHp;
    float hitColdDownTime;
    float feverTimer;
    float speed;
    float maxTurnSpeed;
    float smoothTime;
    float tempScale;

    EPlayerState state;
    Animator animator;

    public const string COLLIDER_TAG_HAND = "hand";
    public const string COLLIDER_TAG_PLAYER = "Player";

    const float HAND_RATIO = 5;
    const float SPEED = 5;

    const float INITIAL_SCALE = 1.0f;
    const float ADD_SCALE = 0.1f;
    const float DONT_CHANGE_DIRECTION_OFFSET = 0.6f;
    const float MIN_DISTANCE = 0.1f;
    const float COLD_DOWN_TIME = 1.0f;

    const float FEVER_TIME = 5.0f;
    const float SPEED_UP_RATIO = 2.0f;
    const float TURN_SPEED = 1000;
    const float SMOOTH_TIME = 0.01f;
    const float SMOOTH_RATIO = 10;

    const string ANIMATOR_IS_MOVING_NAME = "isMoving";
    const string ANIMATOR_IS_FEVER_NAME = "isFever";
    const string ANIMATOR_IS_HIT_NAME = "isHit";
    const float FEVER_SCALE = 4.0f;
    void Awake()
    {        
        sp = hand.GetComponentInChildren<SpriteRenderer>();
        bodySp = GetComponentInChildren<SpriteRenderer>();
        bodyCol = GetComponentInChildren<CircleCollider2D>();
        col = hand.GetComponentInChildren<CircleCollider2D>();
        Reset();
        GameUIController.Instance.InitHp(nowHp);
        animator = GetComponentInChildren<Animator>();
    }

    public int GetHp()
    {
        return nowHp;
    }

    public void Reset()
    {
        state = EPlayerState.STOP;
        nowHp = hp;
        nowScale = INITIAL_SCALE;
        isMoving = false;
        prevFaceR = true;
        feverTimer = hitColdDownTime = 0;
        speed = SPEED;
        maxTurnSpeed = TURN_SPEED;
        smoothTime = SMOOTH_TIME;
        transform.position = Vector2.zero;
        transform.localScale = new Vector2(nowScale, nowScale);        
    }

    Vector3 GetMouseDirection(Vector3 mousePosition)
    {
               
        return mousePosition - hand.transform.position;
    }

    void UpdateView(Vector3 direction, Vector3 mousePosition)
    {
        float targetAngle = Vector2.SignedAngle(Vector2.right, direction);
        angle = Mathf.SmoothDampAngle(angle, targetAngle, ref currentVelocity, smoothTime, maxTurnSpeed);
        hand.transform.eulerAngles = new Vector3(0, 0, angle);

        float distance = Vector2.Distance(mousePosition, hand.transform.position);
        float length = distance * HAND_RATIO / nowScale;
        sp.size = new Vector2(length, 1);
        col.offset = new Vector2(length, 0);
    }
    
    void UpdateFaceTo(Vector3 direction)
    {        
        if (Mathf.Abs(direction.x) < (DONT_CHANGE_DIRECTION_OFFSET * nowScale))
        {
            return;           
        }

        bool faceToR = (direction.x > 0);
        if (prevFaceR != faceToR)
        {
            bodySp.flipX = !faceToR;
            hand.transform.localPosition = faceToR ? posR.localPosition : posL.localPosition;
            bodyCol.offset = new Vector2(-bodyCol.offset.x, bodyCol.offset.y);
        }
        prevFaceR = direction.x > 0;
    }

    void SetIsMoving(bool _isMoving)
    {
        isMoving = _isMoving;
        animator.SetBool(ANIMATOR_IS_MOVING_NAME, _isMoving);
    }

    void CheckControl()
    {
        if (state == EPlayerState.STOP)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            SetIsMoving(true);            
        }
        else if (Input.GetMouseButtonUp(0))
            SetIsMoving(false);
            
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = GetMouseDirection(mousePosition);
        UpdateView(direction, mousePosition);//hand rotation & length
        
        CheckControl();

        if (isMoving)
        {            
            Vector3 direction2 = mousePosition - hand.transform.position;
            UpdateFaceTo(direction2);
            direction.z = 0;
            transform.position += direction.normalized * speed * Time.deltaTime;
            
            if (Vector2.Distance(mousePosition, hand.transform.position) <= MIN_DISTANCE)
            {
                SetIsMoving(false);
            }
        }


        if (hitColdDownTime > 0)
        {
            hitColdDownTime -= Time.deltaTime;
            if (hitColdDownTime <= 0)
            {
                bodyCol.enabled = true;
                animator.SetBool(ANIMATOR_IS_HIT_NAME, false);
            }
                
        }
        else if (feverTimer > 0)
        {
            feverTimer -= Time.deltaTime;
            if (feverTimer <= 0)
            {
                Debug.Log("fever end");
                feverTimer = 0;
                bodyCol.tag = COLLIDER_TAG_PLAYER;
                GameUIController.Instance.SetFever(false);
                animator.SetBool(ANIMATOR_IS_FEVER_NAME, false);
                Debug.Log("tempScale->" + tempScale);
                SetScale(tempScale);
                speed = SPEED;
                maxTurnSpeed = TURN_SPEED;
                smoothTime = SMOOTH_TIME;
            }
        }
    }

    void SetScale(float _scale)
    {
        nowScale = _scale;
        transform.localScale = new Vector2(_scale, _scale);

    }
    public void SetState(EPlayerState _state)
    {
        state = _state;

    }

    public void SetEnd()
    {
        SetState(EPlayerState.STOP);
        isMoving = false;
        //TODO
        animator.SetBool(ANIMATOR_IS_FEVER_NAME, true);
    }   
    
    public void SetHit()
    {
        nowHp--;
        Debug.Log("ouch! now hp->" + nowHp);
        GameUIController.Instance.SetHp(nowHp);
        animator.SetBool(ANIMATOR_IS_HIT_NAME, true);
        if (nowHp <= 0)//TODO pause a second or sth
        {
            GameManager.Instance.OnPlayerDie();
        }
        else
        {
            hitColdDownTime = COLD_DOWN_TIME;
            bodyCol.enabled = false;
        }
    }

    public void SetFever()
    {
        if (feverTimer <= 0)
            tempScale = nowScale;
        feverTimer += FEVER_TIME;
        bodyCol.tag = COLLIDER_TAG_HAND;        
        speed = SPEED * SPEED_UP_RATIO;
        maxTurnSpeed = TURN_SPEED * SPEED_UP_RATIO;
        smoothTime = SMOOTH_TIME / SMOOTH_RATIO;
        animator.SetBool(ANIMATOR_IS_FEVER_NAME, true);
        GameUIController.Instance.SetFever(true, feverTimer);
        SetScale(FEVER_SCALE);
    }

    public void BecomeFatter()
    {
        if (feverTimer > 0)
            tempScale += ADD_SCALE;
        else
            SetScale(nowScale + ADD_SCALE);
    }
}
