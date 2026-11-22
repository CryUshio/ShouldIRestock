using System.Collections.Generic;
using UnityEngine;

namespace ShouldIRestock
{
    public static class ShapeMask
    {
        public static bool[] Read(GridShape shape, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (shape == null)
            {
                return null;
            }

            try
            {
                width = shape.width;
                height = shape.height;
            }
            catch
            {
                return null;
            }

            if (width <= 0 || height <= 0 || width > 64 || height > 64)
            {
                return null;
            }

            var cells = new bool[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte local;
                    try
                    {
                        local = shape.GetLocal(x, y);
                    }
                    catch
                    {
                        return null;
                    }

                    cells[y * width + x] = local != 0;
                }
            }

            return cells;
        }
    }
}
