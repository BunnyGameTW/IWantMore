using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EAudioClipKind
{
    COUNTDOWN = 0,
    ENEMY_DIE = 1,
    SCORE = 2,
    LEVEL_UP = 3,
    END = 4,
    BUTTON = 5,
    BUTTON_NEXT = 6,
    FEVER = 7,
    HURT = 8,

    //SHOOT = 0,
    //SPAWN = 1,
    //HIT = 4,
    //TRANSITION = 9,
}

public class AudioManager : MonoBehaviour
{
    static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            //if (instance == null)
            return instance;
        }
    }

    public AudioClip[] audioClips;
   

    public AudioClip[] transitionClips;
    public AudioClip[] hitClips;

    const int EXPAND_POOL_COUNT = 5;
    const int INITIAL_POOL_COUNT = 3;

    Dictionary<EAudioClipKind, AudioClip> map;
    List<Dictionary<float, AudioSource>> inUsePool;
    List<AudioSource> pool;
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        map = new Dictionary<EAudioClipKind, AudioClip>(audioClips.Length);
        for (int i = 0; i < audioClips.Length; i++)
        {
            map.Add((EAudioClipKind)i, audioClips[i]);
        }
        InitPool();
    }
 
    void InitPool()
    {
        pool = new List<AudioSource>(INITIAL_POOL_COUNT);
        inUsePool = new List<Dictionary<float, AudioSource>>(INITIAL_POOL_COUNT);
        for (int i = 0; i < INITIAL_POOL_COUNT; i++)
        {
            GameObject go = new GameObject("audio");
            AudioSource a = go.AddComponent<AudioSource>();
            pool.Add(a);
        }
    }
    AudioSource GetAudioSource(float sec)
    {
        float t = Time.realtimeSinceStartup + sec;
        if (pool.Count > 0)
        {
            AudioSource a = pool[0];
            pool.Remove(a);
            
            Dictionary<float, AudioSource> dic = new Dictionary<float, AudioSource>(1);
            dic.Add(t, a);
            inUsePool.Add(dic);
            return a;
        }
        else//expand pool
        {
            pool.Capacity += EXPAND_POOL_COUNT;
            inUsePool.Capacity += EXPAND_POOL_COUNT;

            GameObject go = new GameObject("audio");
            AudioSource a = go.AddComponent<AudioSource>();

            Dictionary<float, AudioSource> dic = new Dictionary<float, AudioSource>(1);
            dic.Add(t, a);
            inUsePool.Add(dic);

            for (int i = 1; i < EXPAND_POOL_COUNT; i++)
            {
                GameObject go2 = new GameObject("audio");
                AudioSource a2 = go2.AddComponent<AudioSource>();
                pool.Add(a2);
            }
            return a;
        }
    }

    public void PlaySound(EAudioClipKind clip)
    {
        AudioSource a = GetAudioSource(map[clip].length);
        a.PlayOneShot(map[clip]);
    }

    // Update is called once per frame
    void Update()
    {
        
        for (int i = inUsePool.Count; i > 0; i--)
        {
            float t = CheckReturnToPool(inUsePool[i - 1]);
            if (t != 0)
            {
                var item = inUsePool[i - 1];
                inUsePool.Remove(item);
                pool.Add(item[t]);
            }
        }
    }

    float CheckReturnToPool(Dictionary<float, AudioSource> poolItem)
    {
        foreach (var item in poolItem)
        {
            if (Time.realtimeSinceStartup > item.Key)
            {
                return item.Key;
            }
        }
        return 0;
    }
}
