﻿using SharpDX.DXGI;
using System.Threading;
using System;
using System.IO;
using System.Collections.Generic;

namespace ModelRender
{
    using SharpDX;
    using SharpDX.Direct3D12;
    using SharpDX.Windows;
    using System.Runtime.InteropServices;
    using SharpDX.Mathematics;
    using ObjParser;

    public class ModelRender : IDisposable
    {
        struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;
        };

        struct ConstantBufferData
        {
            public Matrix Project;
        };

        const int TextureWidth = 512;
        const int TextureHeight = 512;

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
        private DescriptorHeap samplerViewHeap;
        private PipelineState pipelineState;
        private GraphicsCommandList commandList;
        private int rtvDescriptorSize;
        private DescriptorHeap srvCbvHeap;

        //App resources
        Resource vertexBuffer;
        Resource indexBuffer;
        Resource depthBuffer;
        Resource texture;
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

            var rtvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = FrameCount,
                Flags = DescriptorHeapFlags.None,
                Type = DescriptorHeapType.RenderTargetView
            };

            renderTargetViewHeap = device.CreateDescriptorHeap(rtvHeapDesc);
            rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);

            var srvCbvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = 1,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            };

            srvCbvHeap = device.CreateDescriptorHeap(srvCbvHeapDesc);

            var rtcHandle = renderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            for (int n = 0; n < FrameCount; n++)
            {
                renderTargets[n] = swapChain.GetBackBuffer<Resource>(n);
                device.CreateRenderTargetView(renderTargets[n], null, rtcHandle);
                rtcHandle += rtvDescriptorSize;
            }

            var svHeapDesc = new DescriptorHeapDescription()
            {
                Type = DescriptorHeapType.Sampler,
                DescriptorCount = 10,
                Flags = DescriptorHeapFlags.ShaderVisible,
                NodeMask = 0
            };
            samplerViewHeap = device.CreateDescriptorHeap(svHeapDesc);

            commandAllocator = device.CreateCommandAllocator(CommandListType.Direct);
        }

        private void LoadAssets()
        {
            // Create the root signature description.
            var rootSignatureDesc = new RootSignatureDescription(

                RootSignatureFlags.AllowInputAssemblerInputLayout,
                // Root Parameters
                new[]
                {
                    new RootParameter(ShaderVisibility.All,
                        new []
                        {
                            new DescriptorRange()
                            {
                                RangeType = DescriptorRangeType.ShaderResourceView,
                                DescriptorCount = 1,
                                OffsetInDescriptorsFromTableStart = int.MinValue,
                                BaseShaderRegister = 0
                            },
                            new DescriptorRange()
                            {
                                RangeType = DescriptorRangeType.ConstantBufferView,
                                DescriptorCount = 1,
                                OffsetInDescriptorsFromTableStart = int.MinValue + 1,
                                BaseShaderRegister = 0
                            }
                        }),
                    new RootParameter(ShaderVisibility.Pixel,
                        new DescriptorRange()
                        {
                            RangeType = DescriptorRangeType.Sampler,
                            DescriptorCount = 1,
                            OffsetInDescriptorsFromTableStart = int.MinValue,
                            BaseShaderRegister = 0
                        }),
                });
            //// Samplers
            //new[]
            //{
            //    new StaticSamplerDescription(ShaderVisibility.Pixel, 0, 0)
            //    {
            //        Filter = Filter.MinimumMinMagMipPoint,
            //        AddressUVW = TextureAddressMode.Border,
            //    }
            //});

            rootSignature = device.CreateRootSignature(0, rootSignatureDesc.Serialize());

            // Create the pipeline state, which includes compiling and loading shaders.
#if DEBUG
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shaders.hlsl"), "VSMain", "vs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("shaders.hlsl", "VSMain", "vs_5_0"));
#endif

#if DEBUG
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shaders.hlsl"), "PSMain", "ps_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("shaders.hlsl", "PSMain", "ps_5_0"));
#endif

//#if DEBUG
//            //var result = SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shaders.hlsl"), "GSMain", "gs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug);
//            var geometryShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shader_standard_lim.hlsl"), "GSMain", "gs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
//#else
//            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("shaders.hlsl", "PSMain", "ps_5_0"));
//#endif

            // Define the vertex input layout.
            var inputElementDescs = new[]
            {
                    new InputElement("POSITION",0,Format.R32G32B32_Float,0,0),
                    new InputElement("TEXCOORD",0,Format.R32G32_Float,12,0)
            };

            // Describe and create the graphics pipeline state object (PSO).
            var psoDesc = new GraphicsPipelineStateDescription()
            {
                InputLayout = new InputLayoutDescription(inputElementDescs),
                RootSignature = rootSignature,
                VertexShader = vertexShader,
                //GeometryShader = geometryShader,
                PixelShader = pixelShader,
                RasterizerState = RasterizerStateDescription.Default(),
                BlendState = BlendStateDescription.Default(),
                DepthStencilFormat = SharpDX.DXGI.Format.D32_Float,
                DepthStencilState = new DepthStencilStateDescription()
                {
                    IsDepthEnabled = true,
                    DepthComparison = Comparison.LessEqual,
                    DepthWriteMask = DepthWriteMask.All,
                    IsStencilEnabled = false
                },
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

            //build model with .obj
            var mesh = new Obj();
            mesh.LoadObj("../../dog.obj");

            // build vertex buffer

            var meshVertices = mesh.VertexList;
            var meshTextures = mesh.TextureList;
            var meshFaces = mesh.FaceList;

            var triangleVertices = new List<Vertex>();
            var triangleIndexes = new List<uint>();

            uint i = 0;
            foreach (ObjParser.Types.Face f in meshFaces)
            {
                int l = f.VertexIndexList.Length;
                for (int j = 0; j < l; j++)
                {
                    var v = meshVertices[f.VertexIndexList[j] - 1];
                    var t = meshTextures[f.TextureVertexIndexList[j] - 1];
                    triangleVertices.Add(new Vertex()
                    {
                        Position = new Vector3((float)v.X, (float)v.Y, (float)v.Z),
                        TexCoord = new Vector2((float)t.X, (float)t.Y)
                    });
                }
                if (l == 3)
                {
                    triangleIndexes.Add(i);
                    triangleIndexes.Add(i + 1);
                    triangleIndexes.Add(i + 2);
                    i += 3;
                }
                else
                {
                    triangleIndexes.Add(i);
                    triangleIndexes.Add(i + 1);
                    triangleIndexes.Add(i + 3);
                    triangleIndexes.Add(i + 3);
                    triangleIndexes.Add(i + 2);
                    triangleIndexes.Add(i + 1);
                    i += 4;
                }
                
            }

            int vertexBufferSize = Utilities.SizeOf(triangleVertices.ToArray());

            vertexBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(vertexBufferSize), ResourceStates.GenericRead);
            IntPtr pVertexDataBegin = vertexBuffer.Map(0);
            Utilities.Write(pVertexDataBegin, triangleVertices.ToArray(), 0, triangleVertices.ToArray().Length);
            vertexBuffer.Unmap(0);

            vertexBufferView = new VertexBufferView();
            vertexBufferView.BufferLocation = vertexBuffer.GPUVirtualAddress;
            vertexBufferView.StrideInBytes = Utilities.SizeOf<Vertex>();
            vertexBufferView.SizeInBytes = vertexBufferSize;

            // build index buffer

            int indexBufferSize = Utilities.SizeOf(triangleIndexes.ToArray());

            indexBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(indexBufferSize), ResourceStates.GenericRead);
            IntPtr pIndexDataBegin = indexBuffer.Map(0);
            Utilities.Write(pIndexDataBegin, triangleIndexes.ToArray(), 0, triangleIndexes.ToArray().Length);
            indexBuffer.Unmap(0);

            indexBufferView = new IndexBufferView();
            indexBufferView.BufferLocation = indexBuffer.GPUVirtualAddress;
            indexBufferView.SizeInBytes = indexBufferSize;
            indexBufferView.Format = Format.R32_UInt;

            // Create the texture.
            // Describe and create a Texture2D.
            var textureDesc = ResourceDescription.Texture2D(Format.R8G8B8A8_UNorm, TextureWidth, TextureHeight, 1, 1, 1, 0, ResourceFlags.None, TextureLayout.Unknown, 0);
            texture = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, textureDesc, ResourceStates.GenericRead, null);

            // Copy data to the intermediate upload heap and then schedule a copy 
            // from the upload heap to the Texture2D.
            byte[] textureData = Utilities.ReadStream(new FileStream("../../Texture.dds", FileMode.Open));

            texture.Name = "Texture";

            var handle = GCHandle.Alloc(textureData, GCHandleType.Pinned);
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(textureData, 0);
            texture.WriteToSubresource(0, null, ptr, TextureWidth * 4, textureData.Length);
            handle.Free();

            // Describe and create a SRV for the texture.
            var srvDesc = new ShaderResourceViewDescription
            {
                Shader4ComponentMapping = ((((0) & 0x7) | (((1) & 0x7) << 3) | (((2) & 0x7) << (3 * 2)) | (((3) & 0x7) << (3 * 3)) | (1 << (3 * 4)))),

                Format = textureDesc.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D =
                {
                    MipLevels = 1,
                    MostDetailedMip = 0,
                    PlaneSlice = 0,
                    ResourceMinLODClamp = 0.0f
                },
            };

            device.CreateShaderResourceView(texture, srvDesc, srvCbvHeap.CPUDescriptorHandleForHeapStart);

            SamplerStateDescription samplerDesc = new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                MaximumAnisotropy = 0,
                MaximumLod = float.MaxValue,
                MinimumLod = -float.MaxValue,
                MipLodBias = 0,
                ComparisonFunction = Comparison.Never
            };

            device.CreateSampler(samplerDesc, samplerViewHeap.CPUDescriptorHandleForHeapStart);

            // build constant buffer

            constantBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(1024 * 64), ResourceStates.GenericRead);

            var cbDesc = new ConstantBufferViewDescription()
            {
                BufferLocation = constantBuffer.GPUVirtualAddress,
                SizeInBytes = (Utilities.SizeOf<ConstantBufferData>() + 255) & ~255
            };
            var srvCbvStep = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            device.CreateConstantBufferView(cbDesc, srvCbvHeap.CPUDescriptorHandleForHeapStart + srvCbvStep);

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
                SampleDescription = new SampleDescription() { Count = 1 }
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

            Resource renderTargetDepth = device.CreateCommittedResource(new HeapProperties(HeapType.Default), HeapFlags.None, descDepth, ResourceStates.GenericRead, dsvClearValue);

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

            //commandList.SetDescriptorHeaps(1, new DescriptorHeap[] { shaderRenderViewHeap });
            //commandList.SetGraphicsRootDescriptorTable(0, constantBufferViewHeap.GPUDescriptorHandleForHeapStart);

            commandList.SetViewport(viewport);
            commandList.SetScissorRectangles(scissorRect);
            commandList.ResourceBarrierTransition(renderTargets[frameIndex], ResourceStates.Present, ResourceStates.RenderTarget);
            var rtvHandle = renderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            rtvHandle += frameIndex * rtvDescriptorSize;


            commandList.ClearDepthStencilView(handleDSV, ClearFlags.FlagsDepth, 1.0f, 0);
            commandList.ClearRenderTargetView(rtvHandle, new Color4(0, 0.2F, 0.4f, 1), 0, null);
            commandList.SetRenderTargets(1, rtvHandle, false, handleDSV);

            commandList.SetGraphicsRootSignature(rootSignature);
            DescriptorHeap[] descHeaps = new[] { srvCbvHeap, samplerViewHeap };
            commandList.SetDescriptorHeaps(descHeaps.GetLength(0), descHeaps);
            commandList.SetGraphicsRootDescriptorTable(0, srvCbvHeap.GPUDescriptorHandleForHeapStart);
            commandList.SetGraphicsRootDescriptorTable(1, samplerViewHeap.GPUDescriptorHandleForHeapStart);
            commandList.PipelineState = pipelineState;

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
            ////Player.SetPosition(new Vector3(0.0f, 0.0f, 0.0f));
            ////Player.SetLens(0.25f * (float)Math.PI, 1.0, 1.0f, 1000.0f);
            ////Player.LookAt(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f));
            ////Player.UpdateViewMatrix();
            ////constantBufferData.Project = Player.GetProjection();

            View = Matrix.LookAtLH(
                //Position of camera
                new Vector3(0.0f, 100.0f, -80.0f),
                //Viewpoint of camera
                new Vector3(0.0f, 50.0f, 0.0f),
                //"Up"
                new Vector3(0.0f, 1.0f, 0.0f));

            Project = Matrix.PerspectiveFovLH(
                //Range of vertical
                MathUtil.Pi / 3f,
                //Aspect ratio
                viewport.Width / viewport.Height,
                //The nearest distance
                1.0f,
                //The farthest distance
                1000.0f);

            //
            World = Matrix.RotationY(Count * 0.02f);
            //World = Matrix.RotationY(0f);

            constantBufferData.Project = (World * View) * Project;

            //


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
