using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Render
{
    public class Engine : IDisposable
    {
        public Engine(RenderForm form)
        {
            Instance = this;
            Core = new Core(form);
        }

        public Engine(RenderForm form,string path)
        {
            Instance = this;
            Core = new Core(form,path);
        }

        public Engine(RenderForm form, Config config)
        {
            Instance = this;
            Core = new Core(form, config);
        }

        public void Initialize()
        {
            ResourceManager = new ResourceManager();
            Core.Initialize();
            // initialize scenes (upload srv cbf etc...)
            ResourceManager.Initialize();
            Execute = new Thread(new ThreadStart(Loop));
        }

        public void Run()
        {
            Execute.Start();
        }

        private void Loop()
        {
            using (var loop = new RenderLoop(Core.Form))
            {
                while (loop.NextFrame())
                {
                    Core.Update();
                    Core.Render();
                }
            }
        }

        public void Dispose()
        {
            Execute.Join();
        }

        private Thread Execute;
        public Core Core { get; private set; }
        public static Engine Instance { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
    }
}
