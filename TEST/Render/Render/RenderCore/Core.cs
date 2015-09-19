using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Render
{
    using SharpDX;
    using SharpDX.Direct3D12;
    using SharpDX.DXGI;
    using SharpDX.Windows;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Device = SharpDX.Direct3D12.Device;
    using Resource = SharpDX.Direct3D12.Resource;

    public class Core : IDisposable
    {
        public Core(RenderForm form)
        {
            Form = form;
            Config = Config.Default;           
        }

        public Core(RenderForm form, string path)
        {
            Form = form;
            Config = Config.Deserialize(path);          
        }

        public Core(RenderForm form, Config config)
        {
            Form = form;
            Config = config;          
        }

        [ConditionalAttribute("DEBUG")]
        private void EnableDebug()
        {
            DebugInterface.Get().EnableDebugLayer();
        }
        public void Initialize()
        {
            Viewport = new ViewportF()
            {
                Width = Form.ClientSize.Width,
                Height = Form.ClientSize.Height,
                MaxDepth = 1.0f
            };
            ScissorRect = new Rectangle()
            {
                Right = Form.ClientSize.Width,
                Bottom = Form.ClientSize.Height
            };
            EnableDebug();
            Device = new Device(null, SharpDX.Direct3D.FeatureLevel.Level_11_0);
            GraphicCommandQueue = Device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
            SwapChain = DxHelper.CreateSwapchain(Form, GraphicCommandQueue, Config);
            FrameIndex = SwapChain.CurrentBackBufferIndex;
            Resource[] tempRenderTargets;
            RenderTargetViewHeap = DxHelper.CreateRenderTargetViewHeap(Config, SwapChain, out tempRenderTargets);
            RenderTargets = tempRenderTargets;
            RootSignature = DxHelper.CreateRootSignature();
            Fence = Device.CreateFence(0, FenceFlags.None);
            FenceValue = 1;
            FenceEvent = new AutoResetEvent(false);
        }

        public void AddCommandList(CommandListDelegate BuildHandler,Pipeline pipeline = null)
        {
            CommandListBuildHandler += BuildHandler;
            Command C = new Command(pipeline);
            CommandPool.Add(C);
        }

        public void Update()
        {
            Delegate[] Handlers = UpdateHandler.GetInvocationList();
            Parallel.For(0, Handlers.Length, Index => ((UpdateDelegate)(Handlers[Index]))());
        }

        public void Render()
        {
            Delegate[] Handlers = CommandListBuildHandler.GetInvocationList();
            CommandList[] List = new CommandList[Handlers.Length];
            Parallel.For(0, Handlers.Length,Index =>
            {
                ((CommandListDelegate)Handlers[Index])(CommandPool[Index].CommandList,CommandPool[Index].Allocator);
                List[Index] = CommandPool[Index].CommandList;
            });
            GraphicCommandQueue.ExecuteCommandLists(CommandPool.Count,List);

            SwapChain.Present(1, 0);
            WaitForPreviousFrame();
        }

        private void WaitForPreviousFrame()
        {
            int LocalFence = FenceValue;
            GraphicCommandQueue.Signal(Fence, LocalFence);
            FenceValue++;
            if (Fence.CompletedValue < LocalFence)
            {
                Fence.SetEventOnCompletion(LocalFence, FenceEvent.SafeWaitHandle.DangerousGetHandle());
                FenceEvent.WaitOne();
            }
            FrameIndex = SwapChain.CurrentBackBufferIndex;
        }

        public void Dispose()
        {
            foreach (var C in CommandPool)
                C.Dispose();
            RenderTargetViewHeap.Dispose();
            SwapChain.Dispose();
        }

        public Config Config { get; private set; }
        public RenderForm Form { get; private set; }
        public Device Device { get; private set; }      
        public RootSignature RootSignature { get; private set; }
        public CommandQueue GraphicCommandQueue { get; private set; }
        public CommandQueue ComputeCommandQueue { get; private set; }
        public DescriptorHeap RenderTargetViewHeap { get; private set; }
        public Resource[] RenderTargets { get; private set; }
        public int FrameIndex { get; private set; }
        public ViewportF Viewport { get; private set; }
        public Rectangle ScissorRect { get; private set; }


        private SwapChain3 SwapChain;
        private Fence Fence;
        private int FenceValue;
        private AutoResetEvent FenceEvent;
        private List<Command> CommandPool = new List<Command>();

        public delegate void CommandListDelegate(GraphicsCommandList commandList,CommandAllocator allocator);
        public event CommandListDelegate CommandListBuildHandler;

        public delegate void UpdateDelegate();
        public event UpdateDelegate UpdateHandler = delegate { };
    }
}
