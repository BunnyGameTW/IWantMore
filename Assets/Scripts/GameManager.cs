using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum EGameState
{
    TITLE = 1,
    READY = 2,
    GAME = 3,
    END = 4
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

    const int MAX_SCORE = 99999;

    const int INITIAL_ENEMY_COUNT = 20;
    const int EXPAND_POOL_COUNT = 5;

    const int COUNT_DOWN_TIME = 4;

    const float SPAWN_TIME = 3.0f;
    const float SPAWN_TIME_DECREASE_RATIO = 0.8f;

    const string SCENE_NAME_LOGIN = "Login";
    const string SCENE_NAME_GAME = "Game";
    const string DEFAULT_SCENE = SCENE_NAME_GAME;

    const string SPAWN_POSITION_NAME = "spawnPos";
    const string SPAWN_ROOT_NAME = "enemyRoot";

    const string TRANISITION_NAME = "GameTransition";

    Vector2 POOL_IDLE_POSITION = new Vector2(15, 0);
    int[] fatScoreArray = {
        10, 200, 500, 1000, 2000,
        5000, 7000, 10000, 50000, 80000
    };
    int[,] enemyKindArray = {
       {60, 30, 10},
       {50, 40, 10},
       {50, 30, 20},
       {40, 30, 30},
       {35, 35, 30},

       {30, 30, 40},
       {20, 40, 40},
       {20, 35, 45},
       {10, 40, 50},
       {10, 30, 60},
    };

    public Enemy[] enemies;
    public Animation transitionAni;
    public GameObject dieParticle;
    public int difficulty { get; set; }
    public float maxDistance { get; set; }

    EGameState state, nextState;
    Player player;
    float comboTimer, spawnTimer, spawnTime;
    int score, comboCounter;
    Dictionary<EEnemyKind, List<Enemy>> enemyPool, enemyInUsePool;
    List<Enemy> enemyList;
    Vector3[] spawnPositions;
    int countDownCounter;
    Transform spawnRoot;
    Dictionary<EGameState, GameObject> gameObjectRoots;
    List<GameObject> particlePool, particleInUsePool;
    #region life cycle
    void Awake()
    {
        Debug.Log("game manager");
        instance = this;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        AnimationEventListener [] events = FindObjectsOfType<AnimationEventListener>();//TODO
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

        spawnRoot = GameObject.Find(SPAWN_ROOT_NAME).transform;
        InitPool();
        gameObjectRoots = new Dictionary<EGameState, GameObject>();
        SceneManager.sceneLoaded += OnSceneLoaded;       
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SetState(EGameState.READY);
        else if (Input.GetKeyDown(KeyCode.S))
        {
            SpawnEnemy(2);
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            SetState(EGameState.TITLE);
        }

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
    }
    #endregion
   
    #region public
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
        e.SetState(EEnemyState.NONE);
        e.SetPosition(POOL_IDLE_POSITION);
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

    void SetStateStart(EGameState _state)
    {
        
        switch (_state)
        {
            case EGameState.TITLE:
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
                break;
        }
    }

    void SetState(EGameState _state)
    {
        state = _state;

        GameUIController.Instance.SetState(_state);
        switch (state)
        {
            case EGameState.TITLE:
                break;
            case EGameState.READY:
                InvokeRepeating("CountDown", 0, 1);
                break;
            case EGameState.GAME:
                player.SetState(EPlayerState.NORMAL);
                break;
            case EGameState.END:
                player.SetEnd();

                //in use enemy stop moving
                for (int i = 0; i < enemies.Length; i++)
                {
                    foreach (Enemy item in enemyInUsePool[enemies[i].kind])
                    {
                        item.SetState(EEnemyState.NONE);
                    }

                }

                Debug.Log("game end->" + score);
                break;
        }
    }
    #endregion

    #region event
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);       

        GameObject [] gos = scene.GetRootGameObjects();
        for (int i = 0; i < gos.Length; i++)
        {
            if (gos[i].name == "GameObjectRoot")
            {
                if (scene.name == SCENE_NAME_GAME)
                {
                    gameObjectRoots.Add(EGameState.READY, gos[i]);//TODO
                    gameObjectRoots.Add(EGameState.GAME, gos[i]);
                    gameObjectRoots.Add(EGameState.END, gos[i]);
                }
                else
                {
                    gameObjectRoots.Add(EGameState.TITLE, gos[i]);
                }
                
                break;
            }                
        }
        

        if (scene.name == DEFAULT_SCENE)
        {            
            SceneManager.LoadScene(SCENE_NAME_LOGIN, LoadSceneMode.Additive);
        }
        else
        {
            bool b = true;
            //SetStateStart(EGameState.READY);
            //SetState(EGameState.READY);
            //gameObjectRoots[EGameState.TITLE].SetActive(!b);
            //gameObjectRoots[EGameState.GAME].SetActive(b);

            SetState(EGameState.TITLE);
            gameObjectRoots[EGameState.TITLE].SetActive(b);
            gameObjectRoots[EGameState.GAME].SetActive(!b);
        }

    }
    public void PlayerHurt()
    {
        player.SetHit();
    }
    public void OnPlayerDie()
    {
        SetState(EGameState.END);

    }
    public void OnEnemyDie(int _score)
    {
        //refresh combo timer
        if (comboTimer > 0)
        {
            comboCounter++;
            score += (int)(_score * COMBO_BONUS);
        }
        else
            score += _score;

        comboTimer = COMBO_TIME;        
        GameUIController.Instance.SetCombo(comboCounter, comboTimer);

        Debug.Log("score->" + score + ", combo->" + comboCounter);

        //check difficulty
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
            GameUIController.Instance.SetDifficultyScore(fatScoreArray[index - 1], fatScoreArray[index]);

            for (int i = 0; i < offset; i++)
            {
                spawnTime *= SPAWN_TIME_DECREASE_RATIO;
                player.BecomeFatter();
                player.SetFever();

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

        GameUIController.Instance.SetScore(score);
        //TODO secret end?!
        //if (score >= MAX_SCORE)

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
                Debug.Log("active root");

                transitionAni[TRANISITION_NAME].speed = -1;
                transitionAni[TRANISITION_NAME].time = transitionAni[TRANISITION_NAME].length;
                transitionAni.Play(TRANISITION_NAME);
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
        for (int i = 0; i < enemies.Length; i++)
        {
            int count = enemyInUsePool[enemies[i].kind].Count;
            for (int j = 0; j < count; j++)
            {
                ReturnToPool(enemyInUsePool[enemies[i].kind][enemyInUsePool[enemies[i].kind].Count - 1]);
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

        particlePool = new List<GameObject>(INITIAL_ENEMY_COUNT);
        particleInUsePool = new List<GameObject>(INITIAL_ENEMY_COUNT);
        for (int j = 0; j < INITIAL_ENEMY_COUNT; j++)
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
                return i + 1;
            }
        }
        return 1;
    }

    EEnemyKind GetRandomEnemyKind(int _difficulty)
    {
       // return EEnemyKind.SPAWN;
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

    void SpawnEnemy(int count = 1)
    {

        List<int> number = new List<int>(count);
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            number.Add(i);
        }

        
        for (int i = 0; i < count; i++)
        {
            Enemy e = GetEnemy(GetRandomEnemyKind(difficulty));
            e.SetState(EEnemyState.NORMAL);
            int v = Random.Range(0, number.Count);
            number.Remove(v);
            e.SetPosition(spawnPositions[v]);
            e.SetTarget(player.transform.position);
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
        //Debug.Log("CountDown->" + countDownCounter);
    }
    #endregion
}
