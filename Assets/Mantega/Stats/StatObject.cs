using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class StatObject
{
    [SerializeField] private Stat _stat;
    [SerializeReference] private object _data;

    [SerializeField] private string _name = "Stat Object";
    public string Name => _name;

    public StatObject(Stat stat)
    {
        _stat = stat;
        _data = stat.CloneData();
    }

    public void PrintType()
    {
        if (_data == null)
        {
            Debug.Log("Data is null");
            return;
        }
        Debug.Log($"{_data.GetType()}: {_data}");
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(StatObject))]
public class StatObjectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Encontra as propriedades filhas
        SerializedProperty statProperty = property.FindPropertyRelative("_stat");
        SerializedProperty dataProperty = property.FindPropertyRelative("_data");
        SerializedProperty nameProperty = property.FindPropertyRelative("_name");

        // Desenha o campo do nome
        Rect nameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(nameRect, nameProperty);
        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;


        // Inicia a verificação de mudanças para o campo _stat
        EditorGUI.BeginChangeCheck();

        // Desenha o campo _stat (o ScriptableObject)
        Rect statRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(statRect, statProperty);

        // Se o BeginChangeCheck detectar uma mudança, este bloco é executado
        if (EditorGUI.EndChangeCheck())
        {
            // Obtém a nova referência do ScriptableObject
            Stat newStat = statProperty.objectReferenceValue as Stat;

            if (newStat != null)
            {
                // Se o novo stat não for nulo, clona seus dados para o campo _data
                // Usamos managedReferenceValue por causa do [SerializeReference]
                dataProperty.managedReferenceValue = newStat.CloneData();
            }
            else
            {
                // Se o campo _stat foi limpo, limpa o _data também
                dataProperty.managedReferenceValue = null;
            }

            // Aplica as modificações para garantir que a serialização seja salva
            property.serializedObject.ApplyModifiedProperties();
        }

        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Desenha o campo _data (que agora está atualizado)
        // O 'true' garante que os campos internos do objeto de dados sejam desenhados
        Rect dataRect = new Rect(position.x, position.y, position.width, EditorGUI.GetPropertyHeight(dataProperty, true));
        EditorGUI.PropertyField(dataRect, dataProperty, true);

        EditorGUI.EndProperty();
    }

    // Ajusta a altura total do drawer para acomodar todos os campos
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = 0;

        // Altura para o campo _name
        totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        // Altura para o campo _stat
        totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        // Altura para o campo _data (que pode ter múltiplos campos)
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_data"), true);

        return totalHeight;
    }
}
#endif