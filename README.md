# SerializableReadonlyStruct

> [!WARNING]
> This package is currently very experimental.

An IL Post-processor for Unity to make `readonly` structs serializable.

`readonly` fields cannot be serialized by default in Unity, but this package allows you to serialize `readonly` fields in structs.

```cs
using System;
using UnityEngine;
using SerializableReadonlyStruct;

public class Example : MonoBehaviour
{
    [SerializeField] private S s;
}

[Serializable, SerializableReadonly]
public readonly struct S
{
    [SerializeField] private readonly int a;
}
```

## Installation

Add git URL to Package Manager:

```
https://github.com/ruccho/SerializableReadonlyStruct.git?path=/Packages/com.ruccho.serializable-readonly-struct
```

## Compatibility

Actually, `[SerializableReadonly]` just removes the `readonly` keyword from the struct definition from the compiled DLLs. `readonly` is an annotation only for the compiler and the runtime does not care about it (unless the metadata is used by reflection). The effect of `readonly` is still valid in the compiled code.

If `[SerializableReadonly]` is used in precompiled assemblies (e.g. libraries), other `csproj`s will reference the compiled assembly which has no `readonly` keyword. This means the fields seem to be mutable on IDE but actually immutable in the compilation.

```cs

#region Assembly-CSharp.dll

public class Example : MonoBehaviour
{
    [SerializeField] private S s;
    
    private void Start()
    {
        s.a = 100; // This is legal on the IDE, but cause a compilation error on Unity
    }
}

#endregion

// If SomeLibrary.dll doesn't have Unity-generated csproj, it will precompiled and referenced by Assembly-CSharp.dll
#region SomeLibrary.dll

[Serializable, SerializableReadonly]
public readonly struct S
{
    public readonly int a;
}

#endregion

```

