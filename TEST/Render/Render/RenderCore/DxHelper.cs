using SharpDX.Direct3D12;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render
{
    using Resource = SharpDX.Direct3D12.Resource;
    static class DxHelper
    {
        public static SwapChain3 CreateSwapchain(RenderForm form, CommandQueue queue,Config config)
        {
            using (var Factory = new Factory4())
            {
                var swapChainDesc = new SwapChainDescription()
                {
                    BufferCount = config.FrameCount,
                    ModeDescription = new ModeDescription(config.Width, config.Height, new Rational(config.RefreshRate, 1), config.Format),
                    Usage = Usage.RenderTargetOutput,
                    SwapEffect = SwapEffect.FlipDiscard,
                    OutputHandle = form.Handle,
                    SampleDescription = new SampleDescription(config.SampleCount, config.SampleQuality),
                    IsWindowed = true
                };
                var tempSwapChain = new SwapChain(Factory, queue, swapChainDesc);
                return tempSwapChain.QueryInterface<SwapChain3>();              
            }
        }

        public static DescriptorHeap CreateRenderTargetViewHeap(Config config, SwapChain3 swapchain, out Resource[] renderTargets)
        {          
            var Heap = Engine.Instance.Device.CreateDescriptorHeap(new DescriptorHeapDescription()
            {
                DescriptorCount = config.FrameCount,
                Flags = DescriptorHeapFlags.None,
                Type = DescriptorHeapType.RenderTargetView
            });
            renderTargets = new Resource[config.FrameCount];
            int Step = Engine.Instance.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
            for (int n = 0; n < config.FrameCount; n++)
            {
                renderTargets[n] = swapchain.GetBackBuffer<Resource>(n);
                Engine.Instance.Device.CreateRenderTargetView(renderTargets[n], null, Heap.CPUDescriptorHandleForHeapStart + n* Step);
            }
            return Heap;
        }
    }
}
