using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NetworkSpawnDelayAttribute : Attribute
{
    public Type ManagerType { get; }

    public NetworkSpawnDelayAttribute(Type managerType)
    {
        ManagerType = managerType;
    }
}