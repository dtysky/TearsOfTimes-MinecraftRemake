using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Render
{
    using Core;
    using SharpDX.Direct3D12;
    using SharpDX.DXGI;
    using SharpDX.Windows;
    using Device = SharpDX.Direct3D12.Device;

    public class Engine : IDisposable
    {
        public Engine(RenderForm form)
        {
            Instance = this;
            Init(form,Config.Default);
        }

        public Engine(RenderForm form, string path)
        {
            Instance = this;           
            Init(form,Config.Deserialize(path));
        }

        public Engine(RenderForm form, Config config)
        {
            Instance = this;
            Init(form,config);
        }

        private void Init(RenderForm form,Config config)
        {
            // device, swap chain, render target and etc...
            Device = new Device(null, SharpDX.Direct3D.FeatureLevel.Level_11_0);
            using (var Factory = new Factory4())
            {
                var QueueDesc = new CommandQueueDescription(CommandListType.Direct);
                GraphicCommandQueue = Device.CreateCommandQueue(QueueDesc);

                var swapChainDesc = new SwapChainDescription()
                {
                    BufferCount = config.FrameCount,
                    ModeDescription = new ModeDescription(config.Width, config.Height, new Rational(config.RefreshRate, 1), config.Format),
                    Usage = Usage.RenderTargetOutput,
                    SwapEffect = SwapEffect.FlipDiscard,
                    OutputHandle = form.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    IsWindowed = true
                };

                var tempSwapChain = new SwapChain(Factory, GraphicCommandQueue, swapChainDesc);
                SwapChain = tempSwapChain.QueryInterface<SwapChain3>();
                int frameIndex = SwapChain.CurrentBackBufferIndex;

            }
        }

        public void InitEvents()
        {
            ResourceManager.Initialize();
        }

        public void Dispose()
        {
            
        }

        public void Update()
        {
            
        }

        public void Render()
        {
            
        }

        public static Engine Instance { get; private set; }
        public Device Device { get; private set; }
        public SwapChain3 SwapChain { get; private set; }
        public CommandQueue GraphicCommandQueue { get; private set; }
        public CommandQueue ComputeCommandQueue { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
    }
}
