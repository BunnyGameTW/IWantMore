using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
public enum EEnemyKind
{
    NORMAL = 1,
    EXPLODE = 2,
    SPAWN = 3
}
public enum EEnemyState
{
    NONE = 0,
    NORMAL = 1,    
    DIE = 2,
    COLD_DOWN = 3,
    IDLE = 4
}
public class Enemy : MonoBehaviour
{
    public EEnemyKind kind;
    public int score;
    public float speed;
    EEnemyState state = EEnemyState.NONE;
    Vector3 direction;    
    float timer, spawnTimer;
    float spawnDirection;
    int bulletNumber;
    Animator animator;
    float shootAnimationTime;
    List<MMF_Feedback> feedbacks;
    MMF_Player feedbackPlayer;
    const int BULLET_NUMBER_MIN = 3; 
    const int BULLET_NUMBER_MAX = 8;
    const float SPAWN_TIME = 3;
    const float COLD_DOWN_TIME = 0.2f;
    const int MAX_DIRECTION = 4;
    const float SPAWN_ANGLE = 90.0f;
    const string ANIMATOR_SHOOT_TRIGGER = "shoot";
    const string ANIMATION_SHOOT_NAME = "EnemyShoot";
    
    // Start is called before the first frame update
    void Awake()
    {
        if (kind == EEnemyKind.SPAWN)
        {
            animator = GetComponent<Animator>();
            RuntimeAnimatorController ac = animator.runtimeAnimatorController;
            for (int i = 0; i < ac.animationClips.Length; i++)                
            {
                if (ac.animationClips[i].name == ANIMATION_SHOOT_NAME)
                {
                    shootAnimationTime = ac.animationClips[i].length;
                }
            }
        }
        feedbackPlayer = GetComponent<MMF_Player>();
        feedbacks = feedbackPlayer.FeedbacksList;
        for (int i = 0; i < feedbacks.Count; i++)
        {
            feedbacks[i].Initialization(feedbackPlayer);
        }
        Reset();      
    }

    void Reset()
    {
        timer = 0;
        spawnTimer = 0;
        spawnDirection = Random.Range(1, MAX_DIRECTION + 1);
        bulletNumber = BULLET_NUMBER_MIN;
    }

    #region public
    public void SetSpeed(float _speed)
    {
        speed = _speed;
    }
    
    public void SetTarget(Vector3 pos)
    {
        direction = pos - transform.position;
        direction.z = 0;
        direction = direction.normalized;        
    }
    public void SetState(EEnemyState _state)
    {
        state = _state;

        if (state == EEnemyState.DIE)
        {
            if (kind == EEnemyKind.EXPLODE)
            {
                float value = (360 / bulletNumber);
                for (int i = 0; i < bulletNumber; i++)
                {
                    if (!GameManager.Instance.CheckCanSpawnEnemy())
                        continue;

                    Enemy e = GameManager.Instance.GetEnemy(EEnemyKind.NORMAL);
                    float angle = value * (i + 1);
                    Vector3 v = GetRotation(angle);
                    Vector3 pos = transform.position;

                    e.SetPosition(pos);
                    e.SetTarget(pos + v);
                    e.SetState(EEnemyState.COLD_DOWN);
                }
            }
            feedbacks[1].Play(Vector3.zero);
            GameManager.Instance.PlayDieEffect(transform.position);
            GameManager.Instance.ReturnToPool(this);
        }
        else if (state == EEnemyState.NONE)
        {
            Reset();
            feedbacks[0].Play(Vector3.zero);
        }
        else if (state == EEnemyState.NORMAL)
        {
            bulletNumber = Mathf.Min(BULLET_NUMBER_MAX, BULLET_NUMBER_MIN + GameManager.Instance.difficulty);
        }
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    public void AddDifficulty()
    {
        if (bulletNumber < BULLET_NUMBER_MAX)
            bulletNumber++;
    }
    #endregion

    // Update is called once per frame
    void Update()
    {        
        if (state == EEnemyState.DIE || state == EEnemyState.NONE || state == EEnemyState.IDLE)
            return;
        
        //move to target
        transform.position += direction * speed * Time.deltaTime;

        //out of view return to pool
        if (Vector2.Distance(transform.position, Vector2.zero) > GameManager.Instance.maxDistance)
        {
            SetState(EEnemyState.IDLE);
            GameManager.Instance.ReturnToPool(this);
            return;
        }
            
        //check spawn
        if (kind == EEnemyKind.SPAWN)
        {
            spawnTimer += Time.deltaTime;            
            if (spawnTimer >= (SPAWN_TIME - shootAnimationTime) && !animator.GetBool(ANIMATOR_SHOOT_TRIGGER))
            {
                //check spawn
                if (!GameManager.Instance.CheckCanSpawnEnemy() || !IsInView(transform.localPosition))
                    spawnTimer = 0;
                else
                    animator.SetBool(ANIMATOR_SHOOT_TRIGGER, true);
            }
            if (spawnTimer >= SPAWN_TIME)
            {
                spawnTimer = 0;
                animator.SetBool(ANIMATOR_SHOOT_TRIGGER, false);

                //direction
                Vector3 v = GetRotation(spawnDirection * SPAWN_ANGLE);
                spawnDirection += 1;
                if (spawnDirection > MAX_DIRECTION)
                    spawnDirection = 1;
                Vector3 pos = transform.position;
                Enemy e = GameManager.Instance.GetEnemy(EEnemyKind.EXPLODE);
                e.SetPosition(pos);
                e.SetTarget(pos + v);
                e.SetState(EEnemyState.NORMAL);
                if (IsInView(pos))
                    AudioManager.Instance.PlaySound(EAudioClipKind.SHOOT, 0.2f);

            }
        }

        if (state == EEnemyState.COLD_DOWN)
        {
            timer += Time.deltaTime;
            if (timer > COLD_DOWN_TIME)
                state = EEnemyState.NORMAL;
        }
        
    }
    bool IsInView(Vector3 pos)
    {
        Vector3 viewPos = Camera.main.WorldToViewportPoint(pos);
        return (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1);
    }

    Vector3 GetRotation(float angle)
    {
        float value = Mathf.Tan(Mathf.Deg2Rad * angle);
        Vector3 v = new Vector3(1, value);
        if (angle > 270)
            v = new Vector3(1, value);
        else if (angle == 270)
            v = new Vector3(0, -1);
        else if (angle == 90)
            v = new Vector3(0, 1);
        else if (angle > 90)
            v = new Vector3(-1, value);
        v = v.normalized;
        return v;
    }

   
    void OnTriggerEnter2D(Collider2D col)
    {
        //Debug.Log("enemy collided with " + col.tag);
        if (state != EEnemyState.NORMAL)
            return;

        if (col.tag == Player.COLLIDER_TAG_PLAYER)
        {
            SetState(EEnemyState.DIE);
            GameManager.Instance.PlayerHurt();
        }
        else if (col.tag == Player.COLLIDER_TAG_HAND)
        {
            GameManager.Instance.OnEnemyDie(score);
            SetState(EEnemyState.DIE);
        }
    }

    void OnTriggerStay2D(Collider2D col)
    {
        if (state != EEnemyState.NORMAL)
            return;

        if (col.tag == Player.COLLIDER_TAG_HAND)
        {
            GameManager.Instance.OnEnemyDie(score);
            SetState(EEnemyState.DIE);
        }
    }
}
