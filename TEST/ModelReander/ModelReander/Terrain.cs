using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelRender
{
    using SharpDX;
    class Terrain
    {
        public Terrain(int width, int height, float cellSpacing)
        {
            Width = width;
            Height = height;
            CellSpacing = cellSpacing;
            Generate();
        }

        private void Generate()
        {
            Data = new float[Width * Height];
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                    Data[i * Width + j] = Convert.ToSingle(Math.Sin(Math.PI * 2.0 * i) * Math.Cos(Math.PI * 2.0 * j));
        }

        public float[] Data { get; private set; }

        private int Width;
        private int Height;
        private float CellSpacing;
    }
}
