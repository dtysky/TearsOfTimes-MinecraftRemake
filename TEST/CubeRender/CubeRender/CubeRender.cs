using SharpDX.DXGI;
using System.Threading;
using System;

namespace CubeRender
{
    using SharpDX;
    using SharpDX.Direct3D12;
    using SharpDX.Windows;

    public class CubeRender : IDisposable
    {
        struct Vertex
        {
            public Vector3 Position;
            public Vector4 Color;
        };

        const int FrameCount = 2;

        private ViewportF viewport;
        private Rectangle scissorRect;
        //Pipeline objects.
        private SwapChain3 swapChain;
        private Device device;
        private readonly Resource[] renderTargets = new Resource[FrameCount];
        private CommandAllocator commandAllocator;
        private CommandQueue commandQueue;
        private RootSignature rootSignature;
        private DescriptorHeap renderTargetViewHeap;
        private PipelineState pipelineState;
        private GraphicsCommandList commandList;
        private int rtvDescriptorSize;

        //App resources
        Resource vertexBuffer;
        VertexBufferView vertexBufferView;

        //Synchronization objetcs.
        private int frameIndex;
        private AutoResetEvent fenceEvent;

        private Fence fence;
        private int fenceValue;

        public void Initialize(RenderForm form)
        {
            LoadPipeline(form);
            LoadAssets();
        }
    
        private void LoadPipeline(RenderForm form)
        {
            int width = form.ClientSize.Width;
            int height = form.ClientSize.Height;

            viewport.Width = width;
            viewport.Height = height;
            viewport.MaxDepth = 1.0f;

            scissorRect.Right = width;
            scissorRect.Bottom = height;

            device = new Device(null, SharpDX.Direct3D.FeatureLevel.Level_11_0);

            using(var factory = new Factory4())
            {
                var queueDesc = new CommandQueueDescription(CommandListType.Direct);
                commandQueue = device.CreateCommandQueue(queueDesc);

                var swapChainDesc = new SwapChainDescription()
                {
                    BufferCount = FrameCount,
                    ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                    Usage = Usage.RenderTargetOutput,
                    SwapEffect = SwapEffect.FlipDiscard,
                    OutputHandle = form.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    IsWindowed = true
                };

                var tempSwapChain = new SwapChain(factory, commandQueue, swapChainDesc);
                swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                frameIndex = swapChain.CurrentBackBufferIndex;

            }

            var rtvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = FrameCount,
                Flags = DescriptorHeapFlags.None,
                Type = DescriptorHeapType.RenderTargetView
            };

            renderTargetViewHeap = device.CreateDescriptorHeap(rtvHeapDesc);
            rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);

            var rtcHandle = renderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            for (int n = 0; n < FrameCount; n++)
            {
                renderTargets[n] = swapChain.GetBackBuffer<Resource>(n);
                device.CreateRenderTargetView(renderTargets[n], null, rtcHandle);
                rtcHandle += rtvDescriptorSize;
            }

            commandAllocator = device.CreateCommandAllocator(CommandListType.Direct);
        }

        private void LoadAssets()
        {
            var rootSignatureDesc = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout);
            rootSignature = device.CreateRootSignature(rootSignatureDesc.Serialize());

            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("../../shaders.hlsl", "VSMain", "vs_5_0"));
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("../../shaders.hlsl", "PSMain", "ps_5_0"));

            var inputElementDescs = new[]
            {
                new InputElement("POSTION", 0, Format.R32G32B32_Float, 0 ,0),
                new InputElement("COLOR", 0, Format.R32G32B32_Float, 12, 0)
            };

            var psoDesc = new GraphicsPipelineStateDescription()
            {
                InputLayout = new InputLayoutDescription(inputElementDescs),
                RootSignature = rootSignature,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                RasterizerState = RasterizerStateDescription.Default(),
                BlendState = BlendStateDescription.Default(),
                DepthStencilFormat = SharpDX.DXGI.Format.D32_Float,
                DepthStencilState = new DepthStencilStateDescription() { IsDepthEnabled = false, IsStencilEnabled = false },
                SampleMask = int.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                RenderTargetCount = 1,
                Flags = PipelineStateFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                StreamOutput = new StreamOutputDescription()
            };
            psoDesc.RenderTargetFormats[0] = SharpDX.DXGI.Format.R8G8B8A8_UNorm;

            pipelineState = device.CreateGraphicsPipelineState(psoDesc);
            
            commandList = device.CreateCommandList(CommandListType.Direct, commandAllocator, pipelineState);

            float aspectRatio = viewport.Width / viewport.Height;

            var triangleVertices = new[]
            {
                new Vertex() {Position=new Vector3(0.0f, 0.25f * aspectRatio, 0.0f ),Color=new Vector4(1.0f, 0.0f, 0.0f, 1.0f ) },
                new Vertex() {Position=new Vector3(0.25f, -0.25f * aspectRatio, 0.0f),Color=new Vector4(0.0f, 1.0f, 0.0f, 1.0f) },
                new Vertex() {Position=new Vector3(-0.25f, -0.25f * aspectRatio, 0.0f),Color=new Vector4(0.0f, 0.0f, 1.0f, 1.0f ) },
            };

            int vertexBufferSize = Utilities.SizeOf(triangleVertices);

            vertexBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(vertexBufferSize), ResourceStates.GenericRead);
            IntPtr pVertexDataBegin = vertexBuffer.Map(0);
            Utilities.Write(pVertexDataBegin, triangleVertices, 0, triangleVertices.Length);
            vertexBuffer.Unmap(0);

            vertexBufferView = new VertexBufferView();
            vertexBufferView.BufferLocation = vertexBuffer.GPUVirtualAddress;
            vertexBufferView.StrideInBytes = Utilities.SizeOf<Vertex>();
            vertexBufferView.SizeInBytes = vertexBufferSize;

            commandList.Close();

            fence = device.CreateFence(0, FenceFlags.None);
            fenceValue = 1;
            fenceEvent = new AutoResetEvent(false);

        }

        private void PopulateCommanList()
        {
            commandAllocator.Reset();
            commandList.Reset(commandAllocator, pipelineState);
            commandList.SetGraphicsRootSignature(rootSignature);
            commandList.SetViewport(viewport);
            commandList.SetScissorRectangles(scissorRect);
            commandList.ResourceBarrierTransition(renderTargets[frameIndex], ResourceStates.Present, ResourceStates.RenderTarget);
            var rtvHandle = renderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            rtvHandle += frameIndex * rtvDescriptorSize;

            commandList.SetRenderTargets(1, rtvHandle, false, null);
            commandList.ClearRenderTargetView(rtvHandle, new Color4(0, 0.2F, 0.4f, 1), 0, null);

            commandList.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            commandList.SetVertexBuffer(0, vertexBufferView);
            commandList.DrawInstanced(3, 1, 0, 0);

            commandList.Close();

        }


        private void WaitForPreviousFrame()
        {

            int localFence = fenceValue;
            commandQueue.Signal(this.fence, localFence);
            fenceValue++;

            if (this.fence.CompletedValue < localFence)
            {
                this.fence.SetEventOnCompletion(localFence, fenceEvent.SafeWaitHandle.DangerousGetHandle());
                fenceEvent.WaitOne();
            }

            frameIndex = swapChain.CurrentBackBufferIndex;
        }

        public void Update()
        {

        }

        public void Render()
        {
            PopulateCommanList();
            commandQueue.ExecuteCommandList(commandList);
            swapChain.Present(1, 0);
            WaitForPreviousFrame();
        }

        public void Dispose()
        {
            WaitForPreviousFrame();

            foreach (var target in renderTargets)
            {
                target.Dispose();
            }
            commandAllocator.Dispose();
            commandQueue.Dispose();
            rootSignature.Dispose();
            renderTargetViewHeap.Dispose();
            pipelineState.Dispose();
            commandList.Dispose();
            vertexBuffer.Dispose();
            fence.Dispose();
            swapChain.Dispose();
            device.Dispose();
        }
    }
}
