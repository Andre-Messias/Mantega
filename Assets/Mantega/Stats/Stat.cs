using UnityEngine;
using Unity.VisualScripting;
using System;
using System.Reflection;
using System.Linq;
using static Mantega.Stats.StatType;

[CreateAssetMenu(fileName = "NewStat", menuName = "Stat/Stat")]
public class Stat : ScriptableObject
{
    public string StatName = "New Stat";

    [SerializeReference] private object _data;

    public object Data => _data;

    public void Log()
    {
        Debug.Log($"{_data?.GetType()}: {_data}");
        if (_data == null) return;
    }

    public object CloneValue()
    {
        if (_data == null) return null;

        var value = GetDataValue();
        if (value == null) return null;

        var json = value.Serialize().json;
        return JsonUtility.FromJson(json, value.GetType());
    }

    public object CloneData()
    {
        if (_data == null) return null;
        Debug.Log($"{_data.Serialize().json}");
        return _data.CloneViaFakeSerialization();
    }

    public object GetValue()
    {
        return GetDataValue();
    }


    private object GetDataValue()
    {
        if (_data == null)
        {
            throw new InvalidOperationException("Data object is null.");
        }

        Type dataType = _data.GetType();
        var statInterface = dataType.GetInterfaces()
                                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStatType<,>));

        if (statInterface != null)
        {
            PropertyInfo propInfo = dataType.GetProperty("Value");
            if (propInfo != null)
            {
                return propInfo.GetValue(_data, null);
            }
        }
        else
        {
            throw new InvalidOperationException($"Data does not implement {typeof(IStatType<,>)} or the 'Value' property is missing.");
        }

        return null;
    }
}