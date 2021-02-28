using System;
using System.Reflection;
using UnityEngine;

public class VariableDump : MonoBehaviour
{
    public string playerName = string.Empty;
    public int number;
    public float dec;



    /// <summary>
    /// Sets a variable in the given VariableDump
    /// </summary>
    public void SetVariable(string variableName, string value)
    {
        FieldInfo field = GetType().GetField(variableName);

        if (field != null)
        {
            switch (field.FieldType)
            {
                case Type t when t == typeof(string):
                    field.SetValue(this, value);
                    break;
                case Type t when t == typeof(bool):
                    field.SetValue(this, value.Equals("true"));
                    break;
                case Type t when t == typeof(int):
                    if (int.TryParse(value, out int parsedInt))
                        field.SetValue(this, parsedInt);
                    else
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to int");
                    break;
                case Type t when t == typeof(float):
                    if (float.TryParse(value, out float parsedFloat))
                        field.SetValue(this, parsedFloat);
                    else
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to float");
                    break;
                default:
                    Debug.LogError($"Field type \"{field.FieldType}\" is not supported. Supported types: float, int, string, bool");
                    break;
            }
        }
        else
        {
            Debug.LogError($"Field \"{variableName}\" not found");
        }
    }

    public void AddVariable(string variableName, string value)
    {
        FieldInfo field = GetType().GetField(variableName);

        if (field != null)
        {
            switch (field.FieldType)
            {
                case Type t when t == typeof(int):
                    if (int.TryParse(value, out int parsedInt))
                    {
                        field.SetValue(this, (int)field.GetValue(this) + parsedInt);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to int");
                    }
                    break;
                case Type t when t == typeof(float):
                    if (float.TryParse(value, out float parsedFloat))
                    {
                        field.SetValue(this, (float)field.GetValue(this) + parsedFloat);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to float");
                    }
                    break;
                default:
                    Debug.LogError($"[Add] only supports ints and floats! Field type \"{field.FieldType}\" is not supported.");
                    break;
            }
        }
        else
        {
            Debug.LogError($"Field \"{variableName}\" not found");
        }
    }

    public void SubtractVariable(string variableName, string value)
    {
        FieldInfo field = GetType().GetField(variableName);

        if (field != null)
        {
            switch (field.FieldType)
            {
                case Type t when t == typeof(int):
                    if (int.TryParse(value, out int parsedInt))
                    {
                        field.SetValue(this, (int)field.GetValue(this) - parsedInt);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to int");
                    }
                    break;
                case Type t when t == typeof(float):
                    if (float.TryParse(value, out float parsedFloat))
                    {
                        field.SetValue(this, (float)field.GetValue(this) - parsedFloat);
                    }
                    else
                    {
                        Debug.LogError($"Invalid Conversion: \"{value}\" could not be cast to float");
                    }
                    break;
                default:
                    Debug.LogError($"[Add] only supports ints and floats! Field type \"{field.FieldType}\" is not supported.");
                    break;
            }
        }
        else
        {
            Debug.LogError($"Field \"{variableName}\" not found");
        }
    }

    public void AppendVariable(string variableName, string value)
    {
        FieldInfo field = GetType().GetField(variableName);

        if (field != null)
        {
            if (field.FieldType == typeof(string))
            {
                field.SetValue(this, field.GetValue(this) + value);
            }
            else
            {
                Debug.LogError($"[Append] only supports strings! Field type \"{field.FieldType}\" is not supported.");
            }

        }
        else
        {
            Debug.LogError($"Field \"{variableName}\" not found");
        }
    }
}
