using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class EventChannelSO<T> : ScriptableObject
{
    List<Action<T>> _listeners = new List<Action<T>>();
    List<Action<T>> _temporaryListeners = new List<Action<T>>();

    void OnEnable()
    {
        _listeners = new List<Action<T>>();
    }

    public void Raise(T value)
    {
        for (var i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i]?.Invoke(value);
        }

        for (var i = 0; i < _temporaryListeners.Count; i++)
        {
            _temporaryListeners[i]?.Invoke(value);
        }
        _temporaryListeners.Clear();
    }

    public void RegisterListener(Action<T> listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    public void RegisterTemporaryListener(Action<T> listener)
    {
        _temporaryListeners.Add(listener);
    }

    public void UnregisterListener(Action<T> listener)
    {
        if (_listeners.Contains(listener))
        {
            _listeners.Remove(listener);
        }
    }
}
