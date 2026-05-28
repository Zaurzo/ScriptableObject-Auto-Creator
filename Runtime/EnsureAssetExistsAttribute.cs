using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EnsureAssetExistsAttribute : Attribute
{
    public string AssetFolder { get; private set; }
    public string AssetName { get; set; }

    public EnsureAssetExistsAttribute(string path)
    {
        AssetFolder = path;
    }
}
