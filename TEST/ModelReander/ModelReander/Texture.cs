using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelRender
{
    using DevIL;
    using System.IO;
    using SharpDX;
    class Texture
    {
        private Texture(Image image, string filePath)
        {
            Image = image;
            image.ConvertToDxtc(CompressedDataFormat.DXT5);
            Width = image.Width;
            Height = image.Height;
            Depth = image.Depth;
            PixelWdith = image.BytesPerPixel;
            if (Path.GetExtension(filePath) == "dds")
            {
                Data = Utilities.ReadStream(new FileStream(filePath, FileMode.Open));
            }
            else
            {
                Data = image.GetImageData(0).Data;
            }
        }

        private Image Image;

        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }

        public int PixelWdith { get; }

        public byte[] Data { get; }


        static public Texture LoadFromFile(string filePath)
        {      
            ImageImporter importer = new ImageImporter();
            Image image = importer.LoadImage(filePath);
            return new Texture(image, filePath);
        }
    }
}
