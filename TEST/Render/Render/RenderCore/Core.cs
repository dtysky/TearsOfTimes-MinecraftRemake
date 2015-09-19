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
            Device = new Device(null, SharpDX.Direct3D.FeatureLevel.Level_11_0);
            GraphicCommandQueue = Device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
            SwapChain = DxHelper.CreateSwapchain(Form, GraphicCommandQueue, Config);
            FrameIndex = SwapChain.CurrentBackBufferIndex;
            RenderTargetViewHeap = DxHelper.CreateRenderTargetViewHeap(Config, SwapChain, out RenderTargets);
            RootSignature = DxHelper.CreateRootSignature();
            Fence = Device.CreateFence(0, FenceFlags.None);
            FenceValue = 1;
            FenceEvent = new AutoResetEvent(false);
            PreProcess = new Command(null); 
            PostProcess = new Command(null); 
        }

        public Command CreateCommand(CommandListDelegate BuildHandler,Pipeline pipeline = null)
        {
            CommandListBuildHandler += BuildHandler;
            Command C = new Command(pipeline);
            CommandPool.Add(C);
            return C;
        }

        public void Update()
        {
            Delegate[] Handlers = UpdateHandler.GetInvocationList();
            Parallel.For(0, Handlers.Length, Index => ((UpdateDelegate)(Handlers[Index]))());
        }

        public void Render()
        {
            InitFrame();
            Delegate[] Handlers = CommandListBuildHandler.GetInvocationList();
            CommandList[] List = new CommandList[Handlers.Length+2];
            List[0] = PreProcess.CommandList;
            List[Handlers.Length+1] = PostProcess.CommandList;
            Parallel.For(0, Handlers.Length,Index =>
            {
                ((CommandListDelegate)Handlers[Index])();
                List[Index+1] = CommandPool[Index].CommandList;
            });

            GraphicCommandQueue.ExecuteCommandLists(CommandPool.Count+2,List);
            SwapChain.Present(1, 0);
            WaitForPreviousFrame();
        }

        private void InitFrame()
        {
            var rtvHandle = RenderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            rtvHandle += FrameIndex * Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);

            PreProcess.Allocator.Reset();
            PreProcess.CommandList.Reset(PreProcess.Allocator, null);
            PreProcess.CommandList.ClearRenderTargetView(rtvHandle, new Color4(0, 0.2F, 0.4f, 1), 0, null);
            PreProcess.CommandList.SetRenderTargets(1, rtvHandle, false, null);
            PreProcess.CommandList.SetViewport(Viewport);
            PreProcess.CommandList.SetScissorRectangles(ScissorRect);
            PreProcess.CommandList.ResourceBarrierTransition(RenderTargets[FrameIndex], ResourceStates.Present, ResourceStates.RenderTarget);
            PreProcess.CommandList.Close();

            PostProcess.Allocator.Reset();
            PostProcess.CommandList.Reset(PostProcess.Allocator, null);
            PostProcess.CommandList.ResourceBarrierTransition(RenderTargets[FrameIndex], ResourceStates.RenderTarget, ResourceStates.Present);
            PostProcess.CommandList.Close();
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
            PreProcess.Dispose();
            PostProcess.Dispose();
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

        public ViewportF Viewport { get; private set; }
        public Rectangle ScissorRect { get; private set; }


        private SwapChain3 SwapChain;
        private DescriptorHeap RenderTargetViewHeap;        
        private Resource[] RenderTargets;
        private int FrameIndex;
        private Fence Fence;
        private int FenceValue;
        private AutoResetEvent FenceEvent;
        private List<Command> CommandPool = new List<Command>();
        private Command PreProcess;
        private Command PostProcess;

        public delegate void CommandListDelegate();
        public event CommandListDelegate CommandListBuildHandler;

        public delegate void UpdateDelegate();
        public event UpdateDelegate UpdateHandler = delegate { };
    }
}
