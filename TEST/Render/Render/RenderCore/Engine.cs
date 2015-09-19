using SharpDX.Windows;
using System;

namespace Render
{
    public class Engine : IDisposable
    {
        public Engine(RenderForm form)
        {
            Instance = this;
            Core = new Core(form);
            ResourceManager = new ResourceManager();
        }

        public Engine(RenderForm form,string path)
        {
            Instance = this;
            Core = new Core(form,path);
            ResourceManager = new ResourceManager();
        }

        public Engine(RenderForm form, Config config)
        {
            Instance = this;
            Core = new Core(form, config);
            ResourceManager = new ResourceManager();
        }

        public void Initialize()
        {
            Core.Initialize();
            // initialize scenes (upload srv cbf etc...)
            ResourceManager.Initialize();
            //Execute = new Thread(new ThreadStart(Loop));
        }

        public void Run()
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
            //Execute.Join();
            Core.Dispose();
            ResourceManager.Dispose();
        }

        //private Thread Execute;
        public Core Core { get; private set; }
        public static Engine Instance { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
    }
}
