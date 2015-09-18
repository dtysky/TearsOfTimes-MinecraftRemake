using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SharpDX.Windows;

namespace Render
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            var form = new RenderForm("Render");
            using (var engine = new Engine(form))
            {
                form.Width = 1440;
                form.Height = 900;
                form.Icon = null;
                form.Show();

                var TestCase = new Test(engine);

                using (var loop = new RenderLoop(form))
                {
                    while (loop.NextFrame())
                    {
                        engine.Update();
                        engine.Render();
                    }
                }
            }
        }
    }
}
