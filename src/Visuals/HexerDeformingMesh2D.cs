using System;
using Godot;

namespace HexerMod.Visuals
{
    /// <summary>
    /// Plain C# wrapper around a Godot Polygon2D subdivided into a 5x6 grid
    /// for smooth 2D skeletal bending and travelling cloth/spectral wave deformation.
    /// Does not subclass Godot.Node2D/Polygon2D so raw csc.exe builds require no source generators.
    /// </summary>
    public sealed class HexerDeformingMesh2D
    {
        private const int Cols = 5;
        private const int Rows = 6;
        private const int VertCount = Cols * Rows;

        public Polygon2D Node { get; }
        public float LastFlareX { get; private set; } = 1f;

        private readonly Vector2[] _restLocal;
        private readonly float[] _uNorm;
        private readonly float[] _vNorm;
        private readonly Vector2[] _deformed;

        public HexerDeformingMesh2D(
            string name,
            Texture2D texture,
            float width,
            float height,
            float pivotX,
            float pivotY,
            int zIndex = 0)
        {
            _restLocal = new Vector2[VertCount];
            _uNorm = new float[VertCount];
            _vNorm = new float[VertCount];
            _deformed = new Vector2[VertCount];

            var uvs = new Vector2[VertCount];
            float texW = Math.Max(1f, texture.GetWidth());
            float texH = Math.Max(1f, texture.GetHeight());

            int idx = 0;
            for (int r = 0; r < Rows; r++)
            {
                float v = r / (float)(Rows - 1);
                for (int c = 0; c < Cols; c++)
                {
                    float u = c / (float)(Cols - 1);
                    _uNorm[idx] = u;
                    _vNorm[idx] = v;
                    float x0 = (u - pivotX) * width;
                    float y0 = (v - pivotY) * height;
                    _restLocal[idx] = new Vector2(x0, y0);
                    _deformed[idx] = _restLocal[idx];
                    uvs[idx] = new Vector2(u * texW, v * texH);
                    idx++;
                }
            }

            var polygons = new Godot.Collections.Array();
            for (int r = 0; r < Rows - 1; r++)
            {
                for (int c = 0; c < Cols - 1; c++)
                {
                    int i00 = r * Cols + c;
                    int i10 = i00 + 1;
                    int i01 = i00 + Cols;
                    int i11 = i01 + 1;
                    polygons.Add(new int[] { i00, i10, i11 });
                    polygons.Add(new int[] { i00, i11, i01 });
                }
            }

            Node = new Polygon2D
            {
                Name = name,
                Texture = texture,
                Polygon = _deformed,
                UV = uvs,
                Polygons = polygons,
                Antialiased = true,
                TextureFilter = CanvasItem.TextureFilterEnum.Linear,
                ZIndex = zIndex
            };
        }

        public void UpdateDeformation(
            float bendRad,
            float wavePhase,
            float waveAmpPx,
            float flareX = 1f,
            float scaleY = 1f)
        {
            LastFlareX = flareX;
            for (int i = 0; i < VertCount; i++)
            {
                float v = _vNorm[i];
                float w = v * v;
                float theta = bendRad * w;
                float wave = MathF.Sin(wavePhase - v * 4.2f) * waveAmpPx * MathF.Pow(v, 1.25f);

                float x = _restLocal[i].X * (1f + (flareX - 1f) * v);
                float y = _restLocal[i].Y * scaleY;

                float cos = MathF.Cos(theta);
                float sin = MathF.Sin(theta);
                float rx = x * cos - y * sin + wave;
                float ry = x * sin + y * cos;

                _deformed[i] = new Vector2(rx, ry);
            }
            Node.Polygon = _deformed;
        }
    }
}
