using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LootLocker.Requests;
using MoreMountains.Feedbacks;
public enum EGameState
{
    TITLE = 1,
    READY = 2,
    GAME = 3,
    END = 4,
    SCORE = 5,
}
public enum ELanguage
{
    CN = 0,
    EN = 1
}
public class GameManager : MonoBehaviour
{
    static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            //if (instance == null)
            return instance;
        }
    }
    
    const float GAP = 1;
    const float COMBO_TIME = 1.0f;
    const float COMBO_BONUS = 2.0f;
    const int MAX_COMBO = 999;
    const int MAX_SCORE = 9999999;
    const int MAX_ENEMY_COUNT = 300;//TODO
    const int UNLOCK_SECRET_SCORE = 1000;
    const int INITIAL_ENEMY_COUNT = 50;
    const int EXPAND_POOL_COUNT = 10;
    const int INITIAL_PARTICLE_COUNT = 30;
    const int GAME_START_ENEMY_COUNT = 5;
    const int COUNT_DOWN_TIME = 4;

    const float SPAWN_TIME = 3.0f;
    const float SPAWN_TIME_DECREASE_RATIO = 0.8f;

    const string SPAWN_POSITION_NAME = "spawnPos";
    const string SPAWN_ROOT_NAME = "enemyRoot";

    const string TRANISITION_NAME = "GameTransition";

    const float SLOW_MOTION_TIME = 2.0f;
    const float SLOW_MOTION_TIME_SCALE = 0.1f;
    const string SAVE_NAME = "save";//NOTE 只用來存分數
    const string RULE_SAVE_NAME = "rule";
    const string LANGUAGE_SAVE_NAME = "language";
    Vector2 POOL_IDLE_POSITION = new Vector2(15, 0);
    int[] fatScoreArray = {
        150, 500, 1000, 3000, 5000, 
        10000, 20000, 50000, 100000, 150000,
    };
    int[,] enemyKindArray = {
       {60, 10, 30},
       {50, 10, 40},
       {50, 20, 30},
       {40, 30, 30},
       {35, 30, 35},

       {30, 40, 30},
       {20, 40, 40},
       {20, 45, 35},
       {20, 40, 40},
       {30, 30, 40},
       {30, 30, 40},
    };

    public Enemy[] enemies;
    public Animation transitionAni;
    public GameObject dieParticle;
    public GameObject[] roots;
    public int difficulty { get; set; }
    public float maxDistance { get; set; }

    EGameState state, nextState;
    public Player player;
    float comboTimer, spawnTimer, spawnTime, endTimer;
    int score, comboCounter, highScore;    
    Dictionary<EEnemyKind, List<Enemy>> enemyPool, enemyInUsePool;
    List<Enemy> enemyList;
    Vector3[] spawnPositions;
    int countDownCounter;
    Transform spawnRoot;
    bool isFirstOpen = true;
    bool hasUnlockSecret;
    Dictionary<EGameState, GameObject> gameObjectRoots;
    List<GameObject> particlePool, particleInUsePool;
    List<Vector3> enemyPositionList;
    LeaderboardController leaderboardController;
    bool hasWatchRule;
    ELanguage language;
    
    public bool isMovingStateForMobile { get; set; }

#if !UNITY_EDITOR && UNITY_WEBGL
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern bool IsMobile();
#endif

    #region life cycle
    void Awake()
    {
        Debug.Log("game manager");
        instance = this;        

        string data = PlayerPrefs.GetString(SAVE_NAME);
        if (data != "")
            highScore = int.Parse(data);
        else
            highScore = 0;
        hasUnlockSecret = highScore > UNLOCK_SECRET_SCORE;

        data = PlayerPrefs.GetString(RULE_SAVE_NAME);
        hasWatchRule = data != "";

        data = PlayerPrefs.GetString(LANGUAGE_SAVE_NAME);
        if (data != "")
            language = data == (ELanguage.CN.ToString()) ? ELanguage.CN : ELanguage.EN;
        else
            language = ELanguage.CN;

        AnimationEventListener[] events = FindObjectsOfType<AnimationEventListener>();//TODO
        for (int i = 0; i < events.Length; i++)
        {
            events[i].sender += AnimationEvent;
        }        

        GameObject go = GameObject.Find(SPAWN_POSITION_NAME);        
        spawnPositions = new Vector3[go.transform.childCount];
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            spawnPositions[i] = go.GetComponentsInChildren<Transform>()[i + 1].position;
            float dis = Vector2.Distance(spawnPositions[i], Vector2.zero);
            if (dis > maxDistance)
                maxDistance = dis;
        }
        maxDistance += GAP;

        enemyPositionList = new List<Vector3>(spawnPositions);
        ResetEnemyPositionList();

        spawnRoot = GameObject.Find(SPAWN_ROOT_NAME).transform;
        InitPool();
        gameObjectRoots = new Dictionary<EGameState, GameObject>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        leaderboardController = GetComponent<LeaderboardController>();

#if !UNITY_EDITOR && UNITY_WEBGL
        if (IsMobile())
        {            
            isMovingStateForMobile = false;
            GameUIController.Instance.ShowGameObjectChangeState();
        }
#endif
    }
    public bool CheckIfMobile()
    {
        bool isMobile = false;
#if !UNITY_EDITOR && UNITY_WEBGL
        isMobile = IsMobile();
#endif
        return isMobile;
    }
   
   
    public ELanguage GetLanguage()
    {
        return language;
    }

    public void SetLanguage(ELanguage _language)
    {
        if (_language != language)
        {
            language = _language;
            PlayerPrefs.SetString(LANGUAGE_SAVE_NAME, language.ToString());
        }
    }
    void MyDebug()
    {
        if (Input.GetKeyDown(KeyCode.S))
            SpawnEnemy(1);
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayerPrefs.SetString(SAVE_NAME, "");
            leaderboardController.SetPlayerName("");
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            SetLanguage(ELanguage.CN);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            SetLanguage(ELanguage.EN);
        }
    }

    void Update()
    {
        MyDebug();

        //spawn enemy
        if (state == EGameState.GAME)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnTime)
            {
                spawnTimer = 0;
                SpawnEnemy(GetRandomEnemyCount(difficulty));
            }

            if (comboTimer > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0)
                {
                    comboCounter = 0;
                    GameUIController.Instance.SetCombo(0);
                }
            }
        }
        else if (state == EGameState.END)
        {
            if (endTimer > 0)
            {
                endTimer -= Time.unscaledDeltaTime;
                if (endTimer <= 0)
                {
                    ChangeState(EGameState.SCORE);
                    endTimer = 0;
                    Time.timeScale = 1.0f;
                }
            }

        }

    }
#endregion
   
#region public
    public void GetLeaderBoardDatas(System.Action<LootLockerLeaderboardMember[]> callback)
    {
        leaderboardController.ShowScores((LootLockerLeaderboardMember[] members) => {
            callback(members);
        });
    }
    public string GetPlayerName()
    {
        return leaderboardController.GetPlayerName();
    }
    public void SetPlayerName(string name)
    {
        leaderboardController.SetPlayerName(name);
    }

    public void SubmitScore(System.Action<LootLockerSubmitScoreResponse> callback)
    {
        leaderboardController.SubmitScore(score, (LootLockerSubmitScoreResponse response) =>
        {
            callback(response);
        });
    }

    public void PlayDieEffect(Vector3 _pos)
    {
        GameObject go;
        if (particlePool.Count > 0)
        {
            go = particlePool[0];
            particlePool.Remove(go);
            particleInUsePool.Add(go);
        }
        else//expand pool
        {

            particlePool.Capacity += EXPAND_POOL_COUNT;
            particleInUsePool.Capacity += EXPAND_POOL_COUNT;

            go = Instantiate(dieParticle, POOL_IDLE_POSITION, Quaternion.identity, spawnRoot);//TODO how to improve
            go.GetComponent<AnimationEventListener>().senderGameObject += AnimationEvent;//TODO
            particleInUsePool.Add(go);

            for (int i = 1; i < EXPAND_POOL_COUNT; i++)
            {
                GameObject e1 = Instantiate(dieParticle, POOL_IDLE_POSITION, Quaternion.identity, spawnRoot);
                e1.GetComponent<AnimationEventListener>().senderGameObject += AnimationEvent;//TODO
                particlePool.Add(e1);
            }
        }

        go.transform.position = _pos;
        go.GetComponent<Animator>().Play("Die", 0, 0f);
    }

    public void ReturnToPool(Enemy e)
    {
        
        enemyInUsePool[e.kind].Remove(e);
        enemyPool[e.kind].Add(e);
    }

    public Enemy GetEnemy(EEnemyKind kind)
    {
        if (enemyPool[kind].Count > 0)
        {

            Enemy e = enemyPool[kind][0];
            enemyPool[kind].Remove(e);
            enemyInUsePool[kind].Add(e);
            e.SetState(EEnemyState.NONE);
            return e;
        }
        else//expand pool
        {            
            enemyPool[kind].Capacity += EXPAND_POOL_COUNT;
            enemyInUsePool[kind].Capacity += EXPAND_POOL_COUNT;

            Enemy e = Instantiate(enemies[(int)kind - 1], POOL_IDLE_POSITION, Quaternion.identity, spawnRoot);//TODO how to improve
            enemyInUsePool[kind].Add(e);            

            for (int i = 1; i < EXPAND_POOL_COUNT; i++)
            {
                Enemy e1 = Instantiate(enemies[(int)kind - 1], POOL_IDLE_POSITION, Quaternion.identity, spawnRoot);
                enemyPool[kind].Add(e1);
            }
            return e;
        }
            
    }


    public void ChangeMoveState()
    {
        isMovingStateForMobile = !isMovingStateForMobile;
        GameUIController.Instance.SetMoveState(isMovingStateForMobile);
    }

    public void ChangeState(EGameState _state)
    {
        if (state == _state)
        {
            Debug.LogError("this shouldn't happen");
            return;
        }

        nextState = _state;
        transitionAni[TRANISITION_NAME].speed = 1;
        transitionAni[TRANISITION_NAME].time = 0;
        transitionAni.Play(TRANISITION_NAME);
    }

    public bool HasWatchRule()
    {
        return hasWatchRule;
    }
    public void SetRuleWatched()
    {
        if (!hasWatchRule)
        {
            hasWatchRule = true;
            PlayerPrefs.SetString(RULE_SAVE_NAME, hasWatchRule.ToString());
        }
    }

    void SetStateStart(EGameState _state)
    {        
        switch (_state)
        {
            case EGameState.TITLE:
                if (!isFirstOpen)
                {
                    LoginUIController.Instance.SetPlayed();
                    if (highScore > 0)
                        LoginUIController.Instance.SetFull();
                }                    
                LoginUIController.Instance.SetHighScore(highScore);
                if (hasUnlockSecret)
                    LoginUIController.Instance.SetUnlockSecret();
                LoginUIController.Instance.SetCanClick(false);
                break;
            case EGameState.READY:
                player.Reset();
                Reset();
                GameUIController.Instance.Reset();
                GameUIController.Instance.SetHp(player.GetHp());
                break;
            case EGameState.GAME:
                break;
            case EGameState.END:
                GameUIController.Instance.ResetHp();
                break;
            case EGameState.SCORE:
                AudioManager.Instance.PlaySound(EAudioClipKind.SCORE, 0.5f);
                bool isHighScore = score > highScore;
                if (isHighScore)
                {
                    highScore = score;
                    PlayerPrefs.SetString(SAVE_NAME, highScore.ToString());
                }
                EndUIController.Instance.SetScore(score, isHighScore);
                EndUIController.Instance.SetCanClick(false);
                if (score > UNLOCK_SECRET_SCORE && !hasUnlockSecret)
                {
                    hasUnlockSecret = true;
                    EndUIController.Instance.SetUnlockSecret();
                }
                break;
        }
    }
    void SetState(EGameState _state)
    {
        state = _state;

        switch (state)
        {
            case EGameState.TITLE:
                if (isFirstOpen)
                {
                    isFirstOpen = false;
                    LoginUIController.Instance.PlayOpenAnimation();
                }
                LoginUIController.Instance.SetHighScore(highScore);
                if (hasUnlockSecret)
                    LoginUIController.Instance.SetUnlockSecret();
                LoginUIController.Instance.SetCanClick(true);
                break;
            case EGameState.READY:
                AudioManager.Instance.PlaySound(EAudioClipKind.COUNTDOWN, 0.3f);
                InvokeRepeating("CountDown", 0, 1);
                break;
            case EGameState.GAME:
                player.SetState(EPlayerState.NORMAL);
                SpawnEnemy(GAME_START_ENEMY_COUNT);
                break;
            case EGameState.END:
                AudioManager.Instance.PlaySound(EAudioClipKind.END, 0.5f);
                GameUIController.Instance.feedbackDie.PlayFeedbacks();

                Time.timeScale = SLOW_MOTION_TIME_SCALE;
                endTimer = SLOW_MOTION_TIME;
                player.SetEnd();
          
                //in use enemy stop moving
                for (int i = 0; i < enemies.Length; i++)
                {
                    foreach (Enemy item in enemyInUsePool[enemies[i].kind])
                    {
                        item.SetState(EEnemyState.IDLE);
                    }

                }
                break;
            case EGameState.SCORE:
                EndUIController.Instance.SetCanClick(true);
                break;
        }
    }
#endregion

#region event
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameObjectRoots.Add(EGameState.TITLE, roots[0]);
        gameObjectRoots.Add(EGameState.READY, roots[1]);
        gameObjectRoots.Add(EGameState.GAME, roots[1]);
        gameObjectRoots.Add(EGameState.END, roots[1]);
        gameObjectRoots.Add(EGameState.SCORE, roots[2]);

        EGameState e = EGameState.TITLE;
        SetState(e);        
        gameObjectRoots[EGameState.TITLE].SetActive(EGameState.TITLE == e);
        gameObjectRoots[EGameState.GAME].SetActive(EGameState.GAME == e);
        gameObjectRoots[EGameState.SCORE].SetActive(EGameState.SCORE == e);
    }
    public void PlayerHurt()
    {
        GameUIController.Instance.feedbackHit.PlayFeedbacks();
        player.SetHit();
    }
    public void OnPlayerDie()
    {
        SetState(EGameState.END);
    }
    public void OnEnemyDie(int _score)
    {
        if (comboTimer > 0)
        {
            if (comboCounter < MAX_COMBO)
                comboCounter++;
            
            score += (int)(_score * COMBO_BONUS);
        }
        else
            score += _score;

        //refresh combo timer
        comboTimer = COMBO_TIME;
        GameUIController.Instance.SetCombo(comboCounter, comboTimer);

        //check score limit
        if (score >= MAX_SCORE)
            score = MAX_SCORE;
        

        AudioManager.Instance.PlayHit();
        player.AddFeverTime();
        //check difficulty
        if (difficulty != fatScoreArray.Length)//TODO reach max level add fixed score to trigger fever?
        {
            int index = difficulty;
            for (int i = difficulty; i < fatScoreArray.Length; i++)
            {
                if (score >= fatScoreArray[i])
                    index = i + 1;
                else
                    break;
            }
            if (index != difficulty)
            {
                int offset = index - difficulty;
                difficulty = index;
                if(difficulty == fatScoreArray.Length)
                    GameUIController.Instance.SetDifficultyScore(fatScoreArray[index - 1], fatScoreArray[index - 1]);
                else
                    GameUIController.Instance.SetDifficultyScore(fatScoreArray[index - 1], fatScoreArray[index]);

                for (int i = 0; i < offset; i++)
                {
                    spawnTime *= SPAWN_TIME_DECREASE_RATIO;
                    AudioManager.Instance.PlaySound(EAudioClipKind.LEVEL_UP);
                    player.SetFever();
                    player.BecomeFatter();

                    for (int k = 0; k < enemies.Length; k++)
                    {
                        int count = enemyInUsePool[enemies[k].kind].Count;
                        for (int j = 0; j < count; j++)
                        {
                            enemyInUsePool[enemies[k].kind][j].AddDifficulty();
                        }
                    }
                }
            }
        }
        GameUIController.Instance.SetScore(score);
    }

    void AnimationEvent(GameObject go)//TODO
    {
        bool isInThere = particleInUsePool.Remove(go);
        
        if (!isInThere)
            return;

        particlePool.Add(go);
        go.transform.position = POOL_IDLE_POSITION;
    }

    void AnimationEvent(string name)
    {
        if (transitionAni[TRANISITION_NAME].speed == 1)
        {
            if (name == "end")
            {
                SetStateStart(nextState);
                gameObjectRoots[state].SetActive(false);
                gameObjectRoots[nextState].SetActive(true);
                Debug.Log("active root->" + state + ", " + nextState);

                transitionAni[TRANISITION_NAME].speed = -1;
                transitionAni[TRANISITION_NAME].time = transitionAni[TRANISITION_NAME].length;
                transitionAni.Play(TRANISITION_NAME);
            }
            else if (name != "start")
            {
                AudioManager.Instance.PlayTransition(int.Parse(name));
            }
        }
        else
        {
            if (name == "start")
            {
                SetState(nextState);
            }

        }
    }
#endregion

#region private
    void Reset()
    {
        score = 0;
        comboCounter = 0;
        comboTimer = 0;
        difficulty = 0;
        spawnTime = SPAWN_TIME;
        spawnTimer = 0;
        countDownCounter = COUNT_DOWN_TIME;
        endTimer = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            int count = enemyInUsePool[enemies[i].kind].Count;
            for (int j = 0; j < count; j++)
            {
                Enemy e = enemyInUsePool[enemies[i].kind][enemyInUsePool[enemies[i].kind].Count - 1];
                ReturnToPool(e);
                e.SetPosition(POOL_IDLE_POSITION);
                //e.SetState(EEnemyState.NONE);
            }
        }
        GameUIController.Instance.SetDifficultyScore(0, fatScoreArray[difficulty]);
    }
    void InitPool()
    {
        enemyInUsePool = new Dictionary<EEnemyKind, List<Enemy>>();
        enemyPool = new Dictionary<EEnemyKind, List<Enemy>>();
        for (int i = 0; i < enemies.Length; i++)
        {
            enemyList = new List<Enemy>(INITIAL_ENEMY_COUNT);
            enemyInUsePool.Add(enemies[i].kind, new List<Enemy>(INITIAL_ENEMY_COUNT));
            for (int j = 0; j < INITIAL_ENEMY_COUNT; j++)
            {
                Enemy e = Instantiate(enemies[i], POOL_IDLE_POSITION, Quaternion.identity, spawnRoot);
                enemyList.Add(e);
            }
            enemyPool.Add(enemies[i].kind, enemyList);
        }

        particlePool = new List<GameObject>(INITIAL_PARTICLE_COUNT);
        particleInUsePool = new List<GameObject>(INITIAL_PARTICLE_COUNT);
        for (int j = 0; j < INITIAL_PARTICLE_COUNT; j++)
        {
            GameObject e = Instantiate(dieParticle, POOL_IDLE_POSITION, Quaternion.identity, spawnRoot);
            e.GetComponent<AnimationEventListener>().senderGameObject += AnimationEvent;//TODO
            particlePool.Add(e);
        }
    }

    int GetRandomEnemyCount(int _difficulty)
    {
        int randomValue = Random.Range(1, 101);
        int sum = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            sum += enemyKindArray[_difficulty, i];
            if (randomValue < sum)
            {
                return i + 2;
            }
        }
        return 1;
    }

    EEnemyKind GetRandomEnemyKind(int _difficulty)
    {
        int randomValue = Random.Range(1, 101);
        int sum = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            sum += enemyKindArray[_difficulty, i];
            if (randomValue < sum)
            {
                return (EEnemyKind)i + 1;//TODO how to improve
            }
        }
        return EEnemyKind.NORMAL;
    }

    public bool CheckCanSpawnEnemy()
    {
        int count = 0;
        foreach (var item in enemyInUsePool)
        {
            count += item.Value.Count;
        }
        return (count < MAX_ENEMY_COUNT);
    }

    void SpawnEnemy(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (!CheckCanSpawnEnemy())
                continue;

            Vector3 pos = enemyPositionList[Random.Range(0, enemyPositionList.Count)];
            enemyPositionList.Remove(pos);
            if (enemyPositionList.Count == 0)
                ResetEnemyPositionList();

            Enemy e = GetEnemy(GetRandomEnemyKind(difficulty));
            e.SetState(EEnemyState.NORMAL);
            e.SetPosition(pos);
            e.SetTarget(player.transform.position);
        }
    }

    void ResetEnemyPositionList()
    {
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            enemyPositionList.Add(spawnPositions[i]);
        }
    }

    void CountDown()
    {
        countDownCounter--;
        if (countDownCounter == 0)
        {
            CancelInvoke();
            SetState(EGameState.GAME);
        }
        GameUIController.Instance.SetCountDown(countDownCounter);
    }
#endregion
}
