using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Windows;

namespace Minecraft.Loader.Client
{
    using Minecraft.Common;
    public class Loader
    {
        public Loader()
        {

        }

        public bool Initialize(Profile TargetProfile)
        {
            Load(TargetProfile);
            Verify(TargetProfile);
            // Initialize Mod
            // Initialize Core

            return true;
        }

        private void Load(Profile TargetProfile)
        {
            //TODO configure settings
        }

        private void Verify(Profile TargetProfile)
        {
            //TODO verify things like mod files
        }

        // private void Init_......

    }
}
