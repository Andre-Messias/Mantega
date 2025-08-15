using System;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[InitializeOnLoad]
public class StatGUIDrawerManager
{
    private static Dictionary<Type, IStatCustomGUIDrawer> _drawers;

    static StatGUIDrawerManager()
    {
        Initialize();
    }

    private static void Initialize()
    {
        _drawers = new Dictionary<Type, IStatCustomGUIDrawer>();

        // Get all types implementing IStatCustomGUIDrawer
        var drawerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IStatCustomGUIDrawer).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        foreach (var type in drawerTypes)
        {
            var attribute = (StatCustomGUIAttribute)Attribute.GetCustomAttribute(type, typeof(StatCustomGUIAttribute));
            if (attribute != null)
            {
                var drawerInstance = (IStatCustomGUIDrawer)Activator.CreateInstance(type);
                _drawers[attribute.TargetType] = drawerInstance;
            }
        }
    }

    public static IStatCustomGUIDrawer GetDrawerForType(Type type)
    {
        _drawers.TryGetValue(type, out var drawer);
        return drawer;
    }
}
public interface IStatCustomGUIDrawer
{
    object Draw(StatType target, object currentValue);
}

#region Default Drawers
[StatCustomGUI(typeof(int))]
public class IntDrawer : IStatCustomGUIDrawer
{
    public object Draw(StatType target, object currentValue)
    {
        int currentVal = (currentValue is int val) ? val : 0;
        return EditorGUILayout.IntField("Value", currentVal);
    }
}

[StatCustomGUI(typeof(float))]
public class FloatDrawer : IStatCustomGUIDrawer
{
    public object Draw(StatType target, object currentValue)
    {
        float currentVal = (currentValue is float val) ? val : 0f;
        return EditorGUILayout.FloatField("Value", currentVal);
    }
}

[StatCustomGUI(typeof(string))]
public class StringDrawer : IStatCustomGUIDrawer
{
    public object Draw(StatType target, object currentValue)
    {
        string currentVal = (currentValue is string val) ? val : string.Empty;
        return EditorGUILayout.TextField("Value", currentVal);
    }
}
#endregion
#endif

[AttributeUsage(AttributeTargets.Class)]
public class StatCustomGUIAttribute : Attribute
{
    public Type TargetType { get; private set; }

    public StatCustomGUIAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}