using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render.Core
{
    using DevIL;
    using System.IO;
    using SharpDX;
    using SharpDX.DXGI;

    class Texture
    {
        private Texture(Image image)
        {
            //Data = raw;
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


        static public Texture LoadFromFile(string filePath)
        {
            ImageImporter importer = new ImageImporter();
            ImageExporter exporter = new ImageExporter();
            Image image;
            image = importer.LoadImage(filePath);

            if (Path.GetExtension(filePath) != ".tga" && Path.GetExtension(filePath) != ".png")
            {
                if (!File.Exists(filePath.Replace(Path.GetExtension(filePath), ".tga")))
                    exporter.SaveImage(image, ImageType.Tga, filePath.Replace(Path.GetExtension(filePath), ".tga"));
                image = importer.LoadImage(filePath.Replace(Path.GetExtension(filePath), ".tga"));
            }

            //image.Bind();
            //var info = DevIL.Unmanaged.IL.GetImageInfo();
            //var bitmap = new System.Drawing.Bitmap(info.Width, info.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            //var rect = new System.Drawing.Rectangle(0, 0, info.Width, info.Height);
            //var data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            //DevIL.Unmanaged.IL.CopyPixels(0, 0, 0, info.Width, info.Height, info.Depth, DataFormat.BGRA, DataType.UnsignedByte);
            //bitmap.UnlockBits(data);
            //var converter = new System.Drawing.ImageConverter();
            //var test = converter.ConvertTo(bitmap, typeof(byte[]));
            //var raw = (byte[])converter.ConvertTo(bitmap, typeof(byte[]));
            return new Texture(image);
        }
    }
}
