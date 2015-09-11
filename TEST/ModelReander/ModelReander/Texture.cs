using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelRender
{
    using DevIL;
    class Texture
    {
        private Texture(Image image)
        {
            Image = image;
            image.ConvertToDxtc(CompressedDataFormat.DXT5);
            Width = image.Width;
            Height = image.Height;
            Depth = image.Depth;
            Data = image.GetImageData(0).Data;
        }

        private Image Image;

        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }

        public byte[] Data { get; }

        static public Texture LoadFromFile(string Path)
        {         
            ImageImporter importer = new ImageImporter();
            Image image = importer.LoadImage(Path);
            return new Texture(image);
        }
    }
}
