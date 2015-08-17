using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Windows;

namespace Minecraft.Loader.Render
{
    public class DxForm : IDisposable
    {
        private RenderForm Form = new RenderForm("Minecraft Remake");
        public DxForm(int Width,int Height)
        {
            Form.Icon = Properties.Resources.Icon;
            Form.Width = Width;
            Form.Height = Height;
        }

        public void Dispose()
        {
            
        }

        public void Initialize()
        {
            Form.Show();

        }

        public void Run()
        {
            using (var Loop = new RenderLoop(Form))
            {
                while (Loop.NextFrame())
                {
                    Update();
                    Render();
                }
            }
        }

        public void Update()
        {
            
        }

        public void Render()
        {
            // record all the commands we need to render the scene into the command list

            // execute the command list

            // swap the back and front buffers

            // wait and reset EVERYTHING

        }
    }
}
