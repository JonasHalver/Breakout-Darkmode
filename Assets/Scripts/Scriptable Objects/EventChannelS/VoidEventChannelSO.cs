using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannelSO : ScriptableObject
{
    List<Action> _listeners = new ();

    void OnEnable()
    {
        _listeners = new List<Action>();
    }

    public void Raise()
    {
        for (var i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i]?.Invoke();
        }
    }

    public void RegisterListener(Action listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    public void UnregisterListener(Action listener)
    {
        if (_listeners.Contains(listener))
        {
            _listeners.Remove(listener);
        }
    }
}

