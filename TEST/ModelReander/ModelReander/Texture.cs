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


        static public List<Texture> LoadFromFile(string rootPath ,string filePath)
        {   
            ImageImporter importer = new ImageImporter();
            Image image;
            List<Texture> textures = new List<Texture>();
            string[] fps = filePath.Split('*');
            string fp;
            for (int i = 0; i < fps.Length; i++)
            {
                fp = rootPath + fps[i];
                image = importer.LoadImage(fp);
                textures.Add(new Texture(image, fp));
            }
            return textures;
        }
    }
}
