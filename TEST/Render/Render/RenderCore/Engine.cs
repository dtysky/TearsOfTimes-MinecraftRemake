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
    using Resource = SharpDX.Direct3D12.Resource;

    public class Engine : IDisposable
    {
        public Engine(RenderForm form)
        {
            Instance = this;
            Config = Config.Default;
            Form = form;
        }

        public Engine(RenderForm form, string path)
        {
            Instance = this;
            Config = Config.Deserialize(path);
            Form = form;
        }

        public Engine(RenderForm form, Config config)
        {
            Instance = this;
            Config = config;
            Form = form;
        }

        public void Initialize()
        {
            ResourceManager = new ResourceManager();
            Device = new Device(null, SharpDX.Direct3D.FeatureLevel.Level_11_0);
            GraphicCommandQueue = Device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
            SwapChain = DxHelper.CreateSwapchain(Form, GraphicCommandQueue, Config);
            FrameIndex = SwapChain.CurrentBackBufferIndex;
            RenderTargetViewHeap = DxHelper.CreateRenderTargetViewHeap(Config, SwapChain, out RenderTargets);
            // Create root signature
            // Initialize heaps srv ......etc
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
        public Config Config { get; private set; }
        public RenderForm Form { get; private set; }
        public Device Device { get; private set; }      
        public CommandQueue GraphicCommandQueue { get; private set; }
        public CommandQueue ComputeCommandQueue { get; private set; }
        public ResourceManager ResourceManager { get; private set; }

        private SwapChain3 SwapChain;
        private DescriptorHeap RenderTargetViewHeap;
        private Resource[] RenderTargets;
        private int FrameIndex;
    }
}
