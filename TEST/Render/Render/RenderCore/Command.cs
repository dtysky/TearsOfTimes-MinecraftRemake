using SharpDX.Direct3D12;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render
{
    public class Command
    {
        public Command(Pipeline pipeline = null)
        {
            Allocator = Engine.Instance.Core.Device.CreateCommandAllocator(CommandListType.Direct);
            CommandList = Engine.Instance.Core.Device.CreateCommandList(CommandListType.Direct, Allocator, pipeline==null?null:pipeline.State);           
            CommandList.Close();
            Allocator.Reset();
        }

        public CommandAllocator Allocator { get; private set; }
        public GraphicsCommandList CommandList { get; private set; }
    }
}
