using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dinwlooc.Common.Core;

public class CommonService : MonoBehaviour
{
    private static CommonService? _instance;
    public static CommonService Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(CommonService));
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<CommonService>();
            }
            return _instance;
        }
    }

    private readonly List<Action<float>> _updateCallbacks = new();
    private readonly List<Action<float>> _fixedUpdateCallbacks = new();
    private readonly List<Action<float>> _lateUpdateCallbacks = new();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterUpdate(Action<float> callback)
    {
        if (!_updateCallbacks.Contains(callback))
            _updateCallbacks.Add(callback);
    }

    public void UnregisterUpdate(Action<float> callback)
    {
        _updateCallbacks.Remove(callback);
    }

    public void RegisterFixedUpdate(Action<float> callback)
    {
        if (!_fixedUpdateCallbacks.Contains(callback))
            _fixedUpdateCallbacks.Add(callback);
    }

    public void UnregisterFixedUpdate(Action<float> callback)
    {
        _fixedUpdateCallbacks.Remove(callback);
    }

    public void RegisterLateUpdate(Action<float> callback)
    {
        if (!_lateUpdateCallbacks.Contains(callback))
            _lateUpdateCallbacks.Add(callback);
    }

    public void UnregisterLateUpdate(Action<float> callback)
    {
        _lateUpdateCallbacks.Remove(callback);
    }

    public Coroutine RunCoroutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    public void StopCoroutineSafe(Coroutine coroutine)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        foreach (var cb in _updateCallbacks)
            cb.Invoke(dt);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        foreach (var cb in _fixedUpdateCallbacks)
            cb.Invoke(dt);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        foreach (var cb in _lateUpdateCallbacks)
            cb.Invoke(dt);
    }
}