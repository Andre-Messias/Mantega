using Mantega.Drawer.TextureDrawer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextureDrawerExample : MonoBehaviour
{
    public Image meuImageUI;
    public TextureDrawer drawer = new();

    void Start()
    {
        drawer.Texture = TextureDrawer.CreateSolidTexture(512, 512, Color.white);

        // 2. Criando uma lista de linhas para desenhar
        List<TextureDrawer.Line> listaDeLinhas = new();

        // Linha Vermelha
        listaDeLinhas.Add(new TextureDrawer.Line(
            new Vector2(50, 50),
            new Vector2(450, 450),
            10,
            Color.red
        ));

        // Linha Azul (cruzando)
        listaDeLinhas.Add(new TextureDrawer.Line(
            new Vector2(50, 450),
            new Vector2(450, 50),
            5,
            Color.blue
        ));

        // 3. Enviando a textura e a lista para serem processadas
        drawer.DrawLines(listaDeLinhas);

        // 4. Jogando na UI
        Rect rect = new(0, 0, 512, 512);
        meuImageUI.sprite = Sprite.Create(drawer.Texture, rect, Vector2.one * 0.5f);
    }
}
