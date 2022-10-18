using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class AnimationEventListener : MonoBehaviour
{
    public event Action<string> sender;
    public event Action<GameObject> senderGameObject;

    public void OnEvent(string name)
    {
        sender?.Invoke(name);
    }

    public void OnEventSelf()
    {
        senderGameObject?.Invoke(gameObject);
    }
}
