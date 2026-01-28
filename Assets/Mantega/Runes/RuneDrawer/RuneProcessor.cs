using System.Collections.Generic;
using UnityEngine;

using Mantega.Drawer;   

namespace Mantega.Runes
{
    public static class RuneProcessor
    {
        public static List<StyledLine> NormalizeAndCenter(List<StyledLine> originalLines, int targetResolution, float padding)
        {
            if (originalLines == null || originalLines.Count == 0) return new List<StyledLine>();

            // 1. Encontrar o Bounding Box (Limites do desenho atual)
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var line in originalLines)
            {
                UpdateBounds(line.Segment.Start, ref minX, ref minY, ref maxX, ref maxY);
                UpdateBounds(line.Segment.End, ref minX, ref minY, ref maxX, ref maxY);
            }

            float currentWidth = maxX - minX;
            float currentHeight = maxY - minY;

            // Evita divisão por zero se for um ponto único
            if (currentWidth == 0) currentWidth = 1;
            if (currentHeight == 0) currentHeight = 1;

            // 2. Calcular o Fator de Escala (Fit to Frame)
            // Queremos que o maior lado do desenho caiba na (resolução - padding)
            float drawableSize = targetResolution - (padding * 2);
            float scaleX = drawableSize / currentWidth;
            float scaleY = drawableSize / currentHeight;
            float finalScale = Mathf.Min(scaleX, scaleY); // Mantém a proporção (aspect ratio)

            // 3. Calcular o Offset para Centralizar
            Vector2 currentCenter = new(minX + currentWidth / 2f, minY + currentHeight / 2f);
            Vector2 targetCenter = new(targetResolution / 2f, targetResolution / 2f);

            // O movimento é: Levar o centro atual para (0,0), escalar, depois levar para o centro alvo
            List<StyledLine> processedLines = new();

            foreach (var line in originalLines)
            {
                Vector2 newStart = TransformPoint(line.Segment.Start, currentCenter, targetCenter, finalScale);
                Vector2 newEnd = TransformPoint(line.Segment.End, currentCenter, targetCenter, finalScale);

                // Recriar a linha com os novos pontos e espessura ajustada (opcional escalar espessura também)
                int newThickness = Mathf.Max(1, Mathf.RoundToInt(line.Thickness)); // Pode multiplicar pelo scale se quiser traço proporcional

                processedLines.Add(new StyledLine(newStart, newEnd, newThickness, line.Color));
            }

            return processedLines;
        }

        private static void UpdateBounds(Vector2 point, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            if (point.x < minX) minX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }

        private static Vector2 TransformPoint(Vector2 point, Vector2 oldCenter, Vector2 newCenter, float scale)
        {
            // 1. Centraliza na origem (0,0) relativa ao desenho original
            Vector2 centered = point - oldCenter;
            // 2. Escala
            Vector2 scaled = centered * scale;
            // 3. Move para o centro da textura alvo
            return scaled + newCenter;
        }
    }
}