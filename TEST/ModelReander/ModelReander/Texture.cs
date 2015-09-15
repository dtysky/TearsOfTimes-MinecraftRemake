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
    using SharpDX.DXGI;

    class Texture
    {
        private Texture(Image image, string filePath)
        {
            //Data = raw;
            Image = image;
            //image.ConvertToDxtc(CompressedDataFormat.DXT5);
            Width = image.Width;
            Height = image.Height;
            Depth = image.Depth;
            PixelWdith = image.BytesPerPixel;

            var imageFormat = image.Format;
            var imageBitPerChannal = image.BitsPerPixel / image.ChannelCount;

            if (imageFormat == DataFormat.BGR && imageBitPerChannal == 8)
            {
                ColorFormat = Format.B8G8R8X8_UNorm;
            }
            else if (imageFormat == DataFormat.BGRA && imageBitPerChannal == 8)
            {
                ColorFormat = Format.B8G8R8A8_UNorm;
            }
            else if (imageFormat == DataFormat.RGB && imageBitPerChannal == 8)
            {
                ColorFormat = Format.R8G8B8A8_UNorm;
            }
            else if (imageFormat == DataFormat.RGBA && imageBitPerChannal == 8)
            {
                ColorFormat = Format.R8G8B8A8_UNorm;
            }
            else if (imageFormat == DataFormat.RGBA && imageBitPerChannal == 16)
            {
                ColorFormat = Format.R16G16B16A16_UNorm;
            }

            Data = image.GetImageData(0).Data;

        }

        private Image Image;

        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int PixelWdith { get; }
        public Format ColorFormat { get; }
        public byte[] Data { get; }


        static public List<Texture> LoadFromFile(string rootPath ,string filePath)
        {   
            ImageImporter importer = new ImageImporter();
            ImageExporter exporter = new ImageExporter();
            Image image;
            List<Texture> textures = new List<Texture>();
            string[] fps = filePath.Split('*');
            string fp;
            for (int i = 0; i < fps.Length; i++)
            {
                fp = rootPath + fps[i];
                image = importer.LoadImage(fp);

                if (Path.GetExtension(fp) != ".tga" && Path.GetExtension(fp) != ".png")
                {
                    if (!File.Exists(fp.Replace(Path.GetExtension(fp), ".tga")))
                        exporter.SaveImage(image, ImageType.Tga, fp.Replace(Path.GetExtension(fp),".tga"));                   
                    image = importer.LoadImage(fp.Replace(Path.GetExtension(fp), ".tga"));
                }

                //image.Bind();
                //var info = DevIL.Unmanaged.IL.GetImageInfo();
                //var bitmap = new System.Drawing.Bitmap(info.Width, info.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                //var rect = new System.Drawing.Rectangle(0, 0, info.Width, info.Height);
                //var data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                //DevIL.Unmanaged.IL.CopyPixels(0, 0, 0, info.Width, info.Height, 1, DataFormat.BGRA, DataType.UnsignedByte);
                //bitmap.UnlockBits(data);
                //var converter = new System.Drawing.ImageConverter();
                //var raw = (byte[])converter.ConvertTo(bitmap, typeof(byte[]));

                textures.Add(new Texture(image, fp));
            }
            return textures;
        }
    }
}
