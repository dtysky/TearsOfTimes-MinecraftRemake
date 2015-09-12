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
            if (Path.GetExtension(filePath) == ".dds")
            {
                var fileStream = new FileStream(filePath, FileMode.Open);
                Data = Utilities.ReadStream(fileStream);
                fileStream.Close();
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
            string fp = filePath;
            if (filePath.IndexOf("*") > 0)
            {
                fp = filePath.Substring(0, filePath.IndexOf("*"));
            }
            Image image = importer.LoadImage(fp);
            return new Texture(image, fp);
        }
    }
}
