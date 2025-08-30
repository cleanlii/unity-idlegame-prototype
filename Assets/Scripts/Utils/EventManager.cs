using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    private static Dictionary<Type, List<object>> eventCallbacks = new();
    
    public static void Subscribe<T>(System.Action<T> callback) where T : IGameEvent
    {
        var eventType = typeof(T);
        if (!eventCallbacks.ContainsKey(eventType))
            eventCallbacks[eventType] = new List<object>();
        
        eventCallbacks[eventType].Add(callback);
    }
    
    public static void Publish<T>(T gameEvent) where T : IGameEvent
    {
        var eventType = typeof(T);
        if (!eventCallbacks.ContainsKey(eventType)) return;
        
        foreach (var callback in eventCallbacks[eventType])
        {
            ((System.Action<T>)callback)?.Invoke(gameEvent);
        }
    }
}

/// <summary>
/// 游戏事件基础接口，所有游戏事件都需要实现此接口
/// </summary>
public interface IGameEvent
{
    // 标记接口，用于类型安全的事件系统
}
