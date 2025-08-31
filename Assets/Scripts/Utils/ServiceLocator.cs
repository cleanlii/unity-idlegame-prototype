using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Service Locator for general service registration and access
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    /// <summary>
    ///     注册服务
    /// </summary>
    /// <typeparam name="T">服务接口类型</typeparam>
    /// <param name="service">服务实例</param>
    public static void Register<T>(T service) where T : class
    {
        var serviceType = typeof(T);

        if (_services.ContainsKey(serviceType)) Debug.LogWarning($"[ServiceLocator] Service {serviceType.Name} is already registered. Overriding...");

        _services[serviceType] = service;
        Debug.Log($"[ServiceLocator] Registered service: {serviceType.Name}");
    }

    /// <summary>
    ///     获取服务
    /// </summary>
    /// <typeparam name="T">服务接口类型</typeparam>
    /// <returns>服务实例</returns>
    public static T Get<T>() where T : class
    {
        var serviceType = typeof(T);

        if (_services.TryGetValue(serviceType, out var service)) return service as T;

        Debug.LogError($"[ServiceLocator] Service {serviceType.Name} not found!");
        return null;
    }

    /// <summary>
    ///     检查服务是否已注册
    /// </summary>
    /// <typeparam name="T">服务接口类型</typeparam>
    /// <returns>是否已注册</returns>
    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    /// <summary>
    ///     取消注册服务
    /// </summary>
    /// <typeparam name="T">服务接口类型</typeparam>
    public static void Unregister<T>() where T : class
    {
        var serviceType = typeof(T);
        if (_services.Remove(serviceType)) Debug.Log($"[ServiceLocator] Unregistered service: {serviceType.Name}");
    }

    /// <summary>
    ///     清空所有服务
    /// </summary>
    public static void Clear()
    {
        _services.Clear();
        Debug.Log("[ServiceLocator] Cleared all services");
    }
}