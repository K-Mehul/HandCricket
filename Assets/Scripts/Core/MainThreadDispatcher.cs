using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// A thread-safe helper to dispatch actions to the Unity Main Thread.
/// This is required because Nakama callbacks run on background threads.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static MainThreadDispatcher Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void Update()
    {
        lock(_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// </summary>
    /// <param name="action">The action to execute</param>
    public static void Enqueue(Action action)
    {
        if (action == null)
        {
            return;
        }

        if (Instance == null)
        {
            Debug.LogError("MainThreadDispatcher not initialized. Add it to the Scene.");
            return;
        }

        lock(_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
