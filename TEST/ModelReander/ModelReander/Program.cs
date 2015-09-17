using SharpDX.Windows;
using System;

namespace ModelRender
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var form = new RenderForm("ModelRender");

            using (var app = new ModelRender())
            {
                form.Width = 1440;
                form.Height = 900;
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
