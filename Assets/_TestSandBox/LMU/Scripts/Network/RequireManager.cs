using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NetworkSpawnManagerAttribute : Attribute
{
    public Type ManagerType { get; }

    public NetworkSpawnManagerAttribute(Type managerType)
    {
        ManagerType = managerType;
    }
}