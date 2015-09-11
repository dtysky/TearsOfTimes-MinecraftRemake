using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelReander
{
    using DevIL;
    using System.Drawing;
    class TextureLoader
    {
        public byte[] Data
        {
            get
            {
                return data;
            }
        }

        private byte[] data;

        public TextureLoader()
        {
            data = new byte[0];
        }

        void Load(String filePath)
        {
            var ImConvert = new ImageConverter();
            data = (byte[])ImConvert.ConvertTo(DevIL.LoadBitmap(filePath), typeof(byte[]));
        }
    }
}
