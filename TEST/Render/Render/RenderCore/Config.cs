﻿using System;
using System.IO;
using System.Xml.Serialization;

namespace Render
{
    using Format = SharpDX.DXGI.Format;
    [Serializable]
    public class Config
    {
        public int    FrameCount            = 2;
        public int    Width                 = 800;
        public int    Height                = 600;
        public int    RefreshRate           = 60;
        public Format Format                = Format.R8G8B8A8_UNorm;
        public int    SampleCount           = 1;
        public int    SampleQuality         = 0;

        public void Serialize(string path)
        {
            XmlSerializer XS = new XmlSerializer(typeof(Config));
            Stream S = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            XS.Serialize(S, this);
            S.Close();
        }

        public static Config Deserialize(string path)
        {
            XmlSerializer XS = new XmlSerializer(typeof(Config));
            Stream S = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Config C = XS.Deserialize(S) as Config;
            S.Close();
            return C;
        }

        public static Config Default = new Config();
    }
}
