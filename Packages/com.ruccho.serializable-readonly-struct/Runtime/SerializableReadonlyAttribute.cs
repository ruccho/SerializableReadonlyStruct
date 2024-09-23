using System;

namespace SerializableReadonlyStruct
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class SerializableReadonlyAttribute : Attribute
    {
    }
}