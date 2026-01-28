///
/// This script demonstrates how to use the TextureDrawer to create and manipulate textures in Unity.
///

using UnityEngine;
using UnityEngine.UI;
using Mantega.Drawer;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextureDrawerExample : MonoBehaviour
{
    [Header("Image")]
    public Image meuImageUI;

    [Header("Drawer")]
    public TextureDrawer drawer;

    [Header("Texture Defaults")]
    public Color bgColor = Color.white;
    public int width = 512;
    public int height = 512;

    private void SetImageSprite(Texture2D texture)
    {
        Rect rect = new(0, 0, texture.width, texture.height);
        meuImageUI.sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f);
    }

    public void ResetTexture()
    {
        Texture2D defaultTexture = TextureDrawer.CreateSolidTexture(width, height, bgColor);
        drawer.Texture = defaultTexture;
        SetImageSprite(defaultTexture);
    }

    public void PaintLine(Vector2 start, Vector2 end, int thickness, Color color)
    {
        StyledLine newLine = new(
            start,
            end,
            thickness,
            color
        );
        drawer.DrawLine(newLine);
    }

    public void PaintLine(Vector2 start, Vector2 end)
    {
        drawer.DrawLine(start, end);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TextureDrawerExample))]
public class TextureDrawerExampleEditor : Editor
{
    // Temporary variables for the custom line drawing
    private Vector2 lineStart = new(50, 50);
    private Vector2 lineEnd = new(200, 200);
    private int lineThickness = 10;
    private Color lineColor = Color.red;

    private Vector2 pointsStart = new(300, 50);
    private Vector2 pointsEnd = new(50, 300);

    public override void OnInspectorGUI()
    {
        // Default inspector
        DrawDefaultInspector();
        TextureDrawerExample script = (TextureDrawerExample)target;
        EditorGUILayout.Space(20);

        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        // Reset Texture
        if (GUILayout.Button("Reset Texture", GUILayout.Height(30)))
        {
            script.ResetTexture();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        // Line
        EditorGUILayout.LabelField("Draw: Line", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            lineStart = EditorGUILayout.Vector2Field("Start Point", lineStart);
            lineEnd = EditorGUILayout.Vector2Field("End Point", lineEnd);
            lineThickness = EditorGUILayout.IntField("Thickness", lineThickness);
            lineColor = EditorGUILayout.ColorField("Color", lineColor);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Draw"))
            {
                script.PaintLine(lineStart, lineEnd, lineThickness, lineColor);
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // Line Points
        EditorGUILayout.LabelField("Draw: Line Points", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            // Drawer Settings Info
            EditorGUILayout.HelpBox($"Using Drawer Settings\nColor: {script.drawer.BrushColor}\nThickness: {script.drawer.BrushThickness}", MessageType.Info);

            pointsStart = EditorGUILayout.Vector2Field("Start Point", pointsStart);
            pointsEnd = EditorGUILayout.Vector2Field("End Point", pointsEnd);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Draw"))
            {
                script.PaintLine(pointsStart, pointsEnd);
            }
        }
        EditorGUILayout.EndVertical();
    }
}
#endif