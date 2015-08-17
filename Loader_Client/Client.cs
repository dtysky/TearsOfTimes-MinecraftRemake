using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Minecraft.Loader.Render;

namespace Minecraft.Loader
{
    using Minecraft.Common;
    using Minecraft.Core;

    public class Client
    {
        private DxForm Form;
        public Client()
        {

        }

        public void Run()
        {
            Form.Run();
        }

        public void Initialize(Profile TargetProfile)
        {
            Load(TargetProfile);
            Verify(TargetProfile);

            Init_Client();        
            // Initialize Core               
            // Initialize Mod
        }

        private void Load(Profile TargetProfile)
        {
            //TODO configure settings
        }

        private void Verify(Profile TargetProfile)
        {
            //TODO verify things like mod files
        }

        private void Init_Client()
        {
            Form = new DxForm(800, 600);
            Form.Initialize();
            //throw new Exception("test exception");
        }

        private void Init_Core()
        {

        }

        private void Init_Mod()
        {

        }

    }
}
