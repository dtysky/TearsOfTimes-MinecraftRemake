using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Render.Core
{
    using SharpDX.Direct3D12;
    using SharpDX.Windows;
    public class Engine : IDisposable
    {
        public Engine(RenderForm form)
        {
            Instance = this;
        }

        public void Init()
        {
            ResourceManager.Initialize();
        }

        public void Dispose()
        {
            
        }

        public void Update()
        {
            
        }

        public void Render()
        {
            
        }

        public static Engine Instance;

        public Device Device { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
    }
}
