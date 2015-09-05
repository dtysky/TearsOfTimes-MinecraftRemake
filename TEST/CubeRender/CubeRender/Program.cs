using SharpDX.Windows;
using System;

namespace CubeRender
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var form = new RenderForm("CubeRender");

            using (var app = new CubeRender())
            {
                form.Show();
                app.Initialize(form);

                using (var loop = new RenderLoop(form))
                {
                    while (loop.NextFrame())
                    {
                        app.Update();
                        app.Render();
                    }
                }
            }
        }
    }
}
