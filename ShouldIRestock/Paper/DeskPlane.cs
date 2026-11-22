using UnityEngine;

namespace ShouldIRestock
{
    // 桌面平面在画面里的样子（照电话底座的轮廓估的）：横向轴略向右下，纵深轴向右上，且被压短。
    public static class DeskPlane
    {
        private const float AcrossAngle = -8f;
        private const float DepthAngle = 50f;
        private const float DepthScale = 0.6f;

        public static void Decompose(out float outer, out float sx, out float sy, out float inner)
        {
            float across = AcrossAngle * Mathf.Deg2Rad;
            float depth = DepthAngle * Mathf.Deg2Rad;
            float a = Mathf.Cos(across);
            float c = Mathf.Sin(across);
            float b = DepthScale * Mathf.Cos(depth);
            float d = DepthScale * Mathf.Sin(depth);
            float e = (a + d) * 0.5f;
            float f = (a - d) * 0.5f;
            float g = (c + b) * 0.5f;
            float h = (c - b) * 0.5f;
            float q = Mathf.Sqrt(e * e + h * h);
            float r = Mathf.Sqrt(f * f + g * g);
            float a1 = Mathf.Atan2(g, f);
            float a2 = Mathf.Atan2(h, e);
            sx = q + r;
            sy = q - r;
            inner = (a2 - a1) * 0.5f * Mathf.Rad2Deg;
            outer = (a2 + a1) * 0.5f * Mathf.Rad2Deg;
        }
    }
}