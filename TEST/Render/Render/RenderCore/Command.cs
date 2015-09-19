using SharpDX.Direct3D12;
using System;

namespace Render
{
    public class Command : IDisposable
    {
        public Command(Pipeline pipeline = null)
        {
            Allocator = Engine.Instance.Core.Device.CreateCommandAllocator(CommandListType.Direct);
            CommandList = Engine.Instance.Core.Device.CreateCommandList(CommandListType.Direct, Allocator, pipeline==null?null:pipeline.State);           
            CommandList.Close();
        }

        public CommandAllocator Allocator { get; private set; }
        public GraphicsCommandList CommandList { get; private set; }

        public void Dispose()
        {
            Allocator.Dispose();
            CommandList.Dispose();
        }
    }
}
