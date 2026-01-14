using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mantega.Drawer.TextureDrawer
{
    public static class TextureDrawer
    {
        [System.Serializable]
        public struct Linha
        {
            public Vector2 inicio;
            public Vector2 fim;
            public int espessura;
            public Color cor;

            public Linha(Vector2 inicio, Vector2 fim, int espessura, Color cor)
            {
                this.inicio = inicio;
                this.fim = fim;
                this.espessura = espessura;
                this.cor = cor;
            }
        }

        // --- FUNÇÃO 1: CRIAR TEXTURA SÓLIDA ---
        public static Texture2D CriarTexturaSolida(int largura, int altura, Color cor, FilterMode filter = FilterMode.Point)
        {
            Texture2D tex = new Texture2D(largura, altura);

            // Importante: FilterMode.Point evita o efeito "embasado"
            tex.filterMode = filter;

            // Preenche todos os pixels de uma vez (mais rápido que loop for)
            Color[] pixels = new Color[largura * altura];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = cor;
            }

            tex.SetPixels(pixels);
            tex.Apply(); // Aplica a mudança na GPU

            return tex;
        }

        // --- FUNÇÃO 2: DESENHAR LISTA DE LINHAS ---
        public static void DesenharLinhas(Texture2D tex, List<Linha> linhas)
        {
            foreach (var linha in linhas)
            {
                DesenharLinhaNaMemoria(tex, linha.inicio, linha.fim, linha.espessura, linha.cor);
            }

            // Chamamos Apply apenas UMA vez no final para performance
            tex.Apply();
        }

        // --- FUNÇÃO 3: DESENHAR UMA ÚNICA LINHA ---
        public static void DesenharLinha(Texture2D tex, Vector2 inicio, Vector2 fim, int espessura, Color cor)
        {
            DesenharLinhaNaMemoria(tex, inicio, fim, espessura, cor);
            tex.Apply();
        }

        // Lógica interna do desenho (Agora usa Vector2 para manter a precisão float)
        private static void DesenharLinhaNaMemoria(Texture2D tex, Vector2 inicio, Vector2 fim, int espessura, Color cor)
        {
            float distancia = Vector2.Distance(inicio, fim);

            // OTIMIZAÇÃO: Não avance de 1 em 1 se a espessura for grande. 
            // Avançar 1/4 da espessura garante cobertura suave sem loops excessivos.
            float passo = Mathf.Max(1f, espessura * 0.25f);

            for (float i = 0; i <= distancia; i += passo)
            {
                float t = i / distancia;

                // MUDANÇA 1: Não usamos RoundToInt aqui. Passamos o float direto.
                float x = Mathf.Lerp(inicio.x, fim.x, t);
                float y = Mathf.Lerp(inicio.y, fim.y, t);

                DesenharPincel(tex, x, y, espessura, cor);
            }

            // Garante o desenho do último ponto exato
            DesenharPincel(tex, fim.x, fim.y, espessura, cor);
        }

        // Desenha um circulo ao redor da coordenada FLOAT precisa
        private static void DesenharPincel(Texture2D tex, float cx, float cy, int diametro, Color cor)
        {
            float raio = diametro / 2f;
            float raioQuadrado = raio * raio;

            // Define a área de busca
            // Adicionamos uma margem de segurança pequena para garantir arredondamentos corretos
            int minX = Mathf.RoundToInt(cx - raio);
            int maxX = Mathf.RoundToInt(cx + raio);
            int minY = Mathf.RoundToInt(cy - raio);
            int maxY = Mathf.RoundToInt(cy + raio);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;

                    // --- CORREÇÃO AQUI ---
                    // Removemos o "+ 0.5f". Agora tratamos a coordenada inteira 'x' 
                    // como o centro do ponto que queremos verificar.
                    // Se cx = 0 e x = 0, a distância é 0.
                    float dx = x - cx;
                    float dy = y - cy;

                    float distanciaQuadrada = dx * dx + dy * dy;

                    // DICA: Para espessuras muito pequenas (como 1), o raioQuadrado é 0.25.
                    // Se a distância for exatamente 0 (ponto exato), ele entra.
                    if (distanciaQuadrada <= raioQuadrado)
                    {
                        tex.SetPixel(x, y, cor);
                    }
                }
            }
        }
    }
}