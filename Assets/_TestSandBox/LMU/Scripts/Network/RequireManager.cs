using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequiredManagerAttribute : Attribute
{
    public Type ManagerType { get; }

    public RequiredManagerAttribute(Type managerType)
    {
        ManagerType = managerType;
    }
}