using SharpDX.DXGI;
using System.Threading;
using System;

namespace CubeRender
{
    using SharpDX;
    using SharpDX.Direct3D12;
    using SharpDX.Windows;
    using SharpDX.Mathematics;

    public class CubeRender : IDisposable
    {
        private Viewer Player = new Viewer();
        struct Vertex
        {
            public Vector3 Position;
            public Vector3 Color;
        };

        struct ConstantBufferData
        {
            public Matrix Project;
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
        private DescriptorHeap constantBufferViewHeap;
        private PipelineState pipelineState;
        private GraphicsCommandList commandList;
        private int rtvDescriptorSize;

        //App resources
        Resource vertexBuffer;
        Resource indexBuffer;
        Resource depthBuffer;
        VertexBufferView vertexBufferView;
        IndexBufferView indexBufferView;
        Resource constantBuffer;
        ConstantBufferData constantBufferData;
        IntPtr constantBufferPointer;


        //Synchronization objetcs.
        private int frameIndex;
        private AutoResetEvent fenceEvent;

        private Fence fence;
        private int fenceValue;

        Matrix World = Matrix.Identity;
        Matrix Project = Matrix.Identity;
        Matrix View = Matrix.Identity;
        private CpuDescriptorHandle handleDSV;

        private int Count = 0;

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

            using (var factory = new Factory4())
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

            var cbvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = 1,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            };

            constantBufferViewHeap = device.CreateDescriptorHeap(cbvHeapDesc);

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
            var rootSignatureDesc = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout,
                // Root Parameters
                new[]
                {
                    new RootParameter(ShaderVisibility.Vertex,
                        new DescriptorRange()
                        {
                            RangeType = DescriptorRangeType.ConstantBufferView,
                            BaseShaderRegister = 0,
                            OffsetInDescriptorsFromTableStart = int.MinValue,
                            DescriptorCount = 1
                        })
                });
            rootSignature = device.CreateRootSignature(rootSignatureDesc.Serialize());

#if DEBUG
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shaders.hlsl"), "VSMain", "vs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("haders.hlsl", "VSMain", "vs_5_0"));
#endif

#if DEBUG
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shaders.hlsl"), "PSMain", "ps_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("shaders.hlsl", "PSMain", "ps_5_0"));
#endif

            var inputElementDescs = new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0 ,0),
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
                DepthStencilState = new DepthStencilStateDescription() { IsDepthEnabled = true, DepthComparison = Comparison.LessEqual, DepthWriteMask = DepthWriteMask.All },
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
            commandList.Close();

            // build vertex buffer
            var triangleVertices = new[]
            {
                new Vertex() {Position=new Vector3(-1.0f, -1.0f, -1.0f),Color=new Vector3(0.0f, 0.0f, 0.0f)},
                new Vertex() {Position=new Vector3(-1.0f, -1.0f,  1.0f),Color=new Vector3(0.0f, 0.0f, 1.0f)},
                new Vertex() {Position=new Vector3(-1.0f,  1.0f, -1.0f),Color=new Vector3(0.0f, 1.0f, 0.0f)},
                new Vertex() {Position=new Vector3(-1.0f,  1.0f,  1.0f),Color=new Vector3(0.0f, 1.0f, 1.0f)},
                new Vertex() {Position=new Vector3( 1.0f, -1.0f, -1.0f),Color=new Vector3(1.0f, 0.0f, 0.0f)},
                new Vertex() {Position=new Vector3( 1.0f, -1.0f,  1.0f),Color=new Vector3(1.0f, 0.0f, 1.0f)},
                new Vertex() {Position=new Vector3( 1.0f,  1.0f, -1.0f),Color=new Vector3(1.0f, 1.0f, 0.0f)},
                new Vertex() {Position=new Vector3( 1.0f,  1.0f,  1.0f),Color=new Vector3(1.0f, 1.0f, 1.0f)}
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

            // build index buffer

            var triangleIndexes = new uint[]
            {
                 0,2,1, // -x
                 1,2,3,
                 
                 4,5,6, // +x
                 5,7,6,
                 
                 0,1,5, // -y
                 0,5,4,
                 
                 2,6,7, // +y
                 2,7,3,
                 
                 0,4,6, // -z
                 0,6,2,
                 
                 1,3,7, // +z
                 1,7,5
            };

            int indexBufferSize = Utilities.SizeOf(triangleIndexes);

            indexBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(indexBufferSize), ResourceStates.GenericRead);
            IntPtr pIndexDataBegin = indexBuffer.Map(0);
            Utilities.Write(pIndexDataBegin, triangleIndexes, 0, triangleIndexes.Length);
            indexBuffer.Unmap(0);

            indexBufferView = new IndexBufferView();
            indexBufferView.BufferLocation = indexBuffer.GPUVirtualAddress;
            indexBufferView.SizeInBytes = indexBufferSize;
            indexBufferView.Format = Format.R32_UInt;

            // build constant buffer

            constantBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(1024 * 64), ResourceStates.GenericRead);

            var cbDesc = new ConstantBufferViewDescription()
            {
                BufferLocation = constantBuffer.GPUVirtualAddress,
                SizeInBytes = (Utilities.SizeOf<ConstantBufferData>() + 255) & ~255
            };
            device.CreateConstantBufferView(cbDesc, constantBufferViewHeap.CPUDescriptorHandleForHeapStart);

            constantBufferData = new ConstantBufferData
            {
                Project = Matrix.Identity
            };

            constantBufferPointer = constantBuffer.Map(0);
            Utilities.Write(constantBufferPointer, ref constantBufferData);

            // build depth buffer

            DescriptorHeapDescription descDescriptorHeapDSB = new DescriptorHeapDescription()
            {
                DescriptorCount = 1,
                Type = DescriptorHeapType.DepthStencilView,
                Flags = DescriptorHeapFlags.None
            };

            DescriptorHeap descriptorHeapDSB = device.CreateDescriptorHeap(descDescriptorHeapDSB);
            ResourceDescription descDepth = new ResourceDescription()
            {
                Dimension = ResourceDimension.Texture2D,
                DepthOrArraySize = 1,
                MipLevels = 0,
                Flags = ResourceFlags.AllowDepthStencil,
                Width = (int)viewport.Width,
                Height = (int)viewport.Height,
                Format = Format.R32_Typeless,
                Layout = TextureLayout.Unknown,
                SampleDescription = new SampleDescription() { Count = 1}
            };

            ClearValue dsvClearValue = new ClearValue()
            {
                Format = Format.D32_Float,
                DepthStencil = new DepthStencilValue()
                {
                    Depth = 1.0f,
                    Stencil = 0
                }
            };

            Resource renderTargetDepth = device.CreateCommittedResource(new HeapProperties(HeapType.Default), HeapFlags.None, descDepth, ResourceStates.GenericRead,dsvClearValue);

            DepthStencilViewDescription depthDSV = new DepthStencilViewDescription()
            {
                Dimension = DepthStencilViewDimension.Texture2D,
                Format = Format.D32_Float,
                Texture2D = new DepthStencilViewDescription.Texture2DResource()
                {
                    MipSlice = 0
                }
            };

            device.CreateDepthStencilView(renderTargetDepth, depthDSV, descriptorHeapDSB.CPUDescriptorHandleForHeapStart);
            handleDSV = descriptorHeapDSB.CPUDescriptorHandleForHeapStart;

            fence = device.CreateFence(0, FenceFlags.None);
            fenceValue = 1;
            fenceEvent = new AutoResetEvent(false);

        }

        

        private void PopulateCommandList()
        {
            commandAllocator.Reset();
            commandList.Reset(commandAllocator, pipelineState);
            commandList.SetGraphicsRootSignature(rootSignature);

            commandList.SetDescriptorHeaps(1, new DescriptorHeap[] { constantBufferViewHeap });
            commandList.SetGraphicsRootDescriptorTable(0, constantBufferViewHeap.GPUDescriptorHandleForHeapStart);

            commandList.SetViewport(viewport);
            commandList.SetScissorRectangles(scissorRect);
            commandList.ResourceBarrierTransition(renderTargets[frameIndex], ResourceStates.Present, ResourceStates.RenderTarget);
            var rtvHandle = renderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            rtvHandle += frameIndex * rtvDescriptorSize;

            
            commandList.ClearDepthStencilView(handleDSV, ClearFlags.FlagsDepth, 1.0f, 0);
            commandList.ClearRenderTargetView(rtvHandle, new Color4(0, 0.2F, 0.4f, 1), 0, null);
            commandList.SetRenderTargets(1, rtvHandle, false, handleDSV);

            commandList.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;        
            commandList.SetVertexBuffer(0, vertexBufferView);
            commandList.SetIndexBuffer(indexBufferView);
            commandList.DrawIndexedInstanced(36, 1, 0, 0, 0);
            //commandList.DrawInstanced(3, 1, 0, 0);
            commandList.ResourceBarrierTransition(renderTargets[frameIndex], ResourceStates.RenderTarget, ResourceStates.Present);

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
            //Player.SetPosition(new Vector3(0.0f, 0.0f, 0.0f));
            //Player.SetLens(0.25f * (float)Math.PI, 1.0, 1.0f, 1000.0f);
            //Player.LookAt(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f));
            //Player.UpdateViewMatrix();
            //constantBufferData.Project = Player.GetProjection();

            View = Matrix.LookAtLH(new Vector3(0.0f,2.0f,-4.5f), new Vector3(0.0f,0.0f,0.0f), new Vector3(0.0f,1.0f,0.0f));
            Project = Matrix.PerspectiveFovLH(MathUtil.Pi / 3.0f, viewport.Width / viewport.Height, 0.1f, 100.0f);
            World = Matrix.RotationY(Count * 0.02f);
            constantBufferData.Project = (World * View) * Project;

            Utilities.Write(constantBufferPointer, ref constantBufferData);
            Count++;
        }

        public void Render()
        {
            PopulateCommandList();
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

