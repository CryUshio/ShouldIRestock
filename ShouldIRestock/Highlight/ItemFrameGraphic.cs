using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShouldIRestock
{
    public sealed class ItemFrameGraphic : MaskableGraphic
    {
        private readonly List<Vector3> positions = new List<Vector3>(128);
        private readonly List<int> triangles = new List<int>(256);
        private bool built;

        public void Configure(bool[] cells, int cols, int rows, float width, float height, float thickness)
        {
            FrameGeometry.Build(cells, cols, rows, width, height, thickness, positions, triangles);
            built = positions.Count > 0;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!built)
            {
                return;
            }

            var tint = (Color32)color;
            for (int i = 0; i < positions.Count; i++)
            {
                vh.AddVert(positions[i], tint, Vector2.zero);
            }

            for (int i = 0; i < triangles.Count; i += 3)
            {
                vh.AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
            }
        }
    }
}
