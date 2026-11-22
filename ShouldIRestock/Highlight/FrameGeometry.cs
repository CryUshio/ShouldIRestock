using System.Collections.Generic;
using UnityEngine;

namespace ShouldIRestock
{
    public static class FrameGeometry
    {
        public static void Build(bool[] cells, int cols, int rows, float width, float height, float thickness, List<Vector3> pos, List<int> tris)
        {
            pos.Clear();
            tris.Clear();
            if (width < 1f || height < 1f)
            {
                return;
            }

            if (cols <= 0 || rows <= 0 || cells == null)
            {
                cols = 1;
                rows = 1;
                cells = null;
            }

            float cellW = width / cols;
            float cellH = height / rows;
            float line = Mathf.Clamp(thickness, 1f, Mathf.Min(cellW, cellH) * 0.45f);
            float left = -width * 0.5f;
            float top = height * 0.5f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (!Filled(cells, cols, rows, x, y))
                    {
                        continue;
                    }

                    float x0 = left + x * cellW;
                    float y0 = top - (y + 1) * cellH;
                    float x1 = x0 + cellW;
                    float y1 = y0 + cellH;
                    if (!Filled(cells, cols, rows, x, y - 1))
                    {
                        Quad(pos, tris, x0, y1 - line, x1, y1);
                    }

                    if (!Filled(cells, cols, rows, x, y + 1))
                    {
                        Quad(pos, tris, x0, y0, x1, y0 + line);
                    }

                    if (!Filled(cells, cols, rows, x - 1, y))
                    {
                        Quad(pos, tris, x0, y0, x0 + line, y1);
                    }

                    if (!Filled(cells, cols, rows, x + 1, y))
                    {
                        Quad(pos, tris, x1 - line, y0, x1, y1);
                    }
                }
            }
        }

        private static bool Filled(bool[] cells, int cols, int rows, int x, int y)
        {
            if (cells == null)
            {
                return x == 0 && y == 0;
            }

            if (x < 0 || y < 0 || x >= cols || y >= rows)
            {
                return false;
            }

            return cells[y * cols + x];
        }

        private static void Quad(List<Vector3> pos, List<int> tris, float x0, float y0, float x1, float y1)
        {
            int i = pos.Count;
            pos.Add(new Vector3(x0, y0, 0f));
            pos.Add(new Vector3(x0, y1, 0f));
            pos.Add(new Vector3(x1, y1, 0f));
            pos.Add(new Vector3(x1, y0, 0f));
            tris.Add(i);
            tris.Add(i + 1);
            tris.Add(i + 2);
            tris.Add(i);
            tris.Add(i + 2);
            tris.Add(i + 3);
        }
    }
}
