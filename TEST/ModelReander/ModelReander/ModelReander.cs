using SharpDX.DXGI;
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
    using Assimp;
    using Assimp.Configs;
    using System.Windows.Forms;
    using ModelReander;

    public class ModelRender : IDisposable
    {
        public struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector3 Tangent;
            public Vector3 BiTangent;
            public Vector4 Diffuse;
            public Vector4 emissive;
            public Vector4 Specular;
            public Vector2 TexCoord;
        };

        struct MeshCtrBufferData
        {
            public int TexsCount;
        }

        struct Light
        {
            public LightSourceType Type;
            public Vector4 Diffuse;
            public Vector4 Specular;
            public Vector4 Ambient;
            public Vector3 Position;
            public Vector3 Direction;
            public Vector3 Attenuation;
            public float Range;
            public float Palloff;
            public float Theta;
            public float Phi;
        }
        
        
        struct ConstantBufferData
        {
            public Matrix Wrold;
            public Matrix View;
            public Matrix Project;
            public int TexsCount;
            public Light Light;
        };

        private struct BufferView
        {
            public VertexBufferView vertexBufferView;
            public IndexBufferView indexBufferView;
            public int IndexCount;
            public int ViewStep;
            public int TexsCount;
        }

        private FallowingCamera Hero;

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
        private DescriptorHeap shaderRenderViewHeap;

        Resource meshCtrBuffer;
        MeshCtrBufferData meshCtrBufferData;
        IntPtr meshCtrBufferPointer;
        private DescriptorHeap meshCtrBufferViewHeap;

        //App resources
        Resource vertexBuffer;
        Resource indexBuffer;
        Resource depthBuffer;
        Resource texture;
        Resource constantBuffer;
        ConstantBufferData constantBufferData;
        IntPtr constantBufferPointer;

        List<BufferView> bufferViews;

        //Synchronization objetcs.
        private int frameIndex;
        private AutoResetEvent fenceEvent;

        private Fence fence;
        private int fenceValue;
        
        Matrix World = Matrix.Identity;
        Camera Player = new Camera();
        private CpuDescriptorHandle handleDSV;

        float Scalling = 50.0f;
        private float AngleY;

        public void Initialize(RenderForm form)
        {
            LoadPipeline(form);
            LoadAssets();
            form.KeyDown += Form_KeyDown;
            form.MouseMove += Form_MouseMove;
            form.MouseWheel += Form_MouseWheel;
            Player.SetPosition(0, 100f, -100f);
            Hero = new FallowingCamera(new Vector3(0, -50, 100));
            Hero.SetPosition(0, 100f, -100f);
        }

        private void Form_MouseWheel(object sender, MouseEventArgs e)
        {
            Scalling += 0.05f * e.Delta;
        }

        public Vector2 Mouse_Pos = new Vector2(0, 0);

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            Vector2 delta_move = new Vector2(e.X - Mouse_Pos.X, e.Y - Mouse_Pos.Y);
            Mouse_Pos.X = e.X;Mouse_Pos.Y = e.Y;
            if(e.Button == MouseButtons.Left)
            {
                Player.Pitch(Convert.ToSingle(Math.Asin(delta_move.Y / 100.0)));
                Player.RotateY(Convert.ToSingle(Math.Asin(delta_move.X / 100.0)));
                Hero.Pitch(Convert.ToSingle(Math.Asin(delta_move.Y / 100.0)));
                Hero.RotateY(Convert.ToSingle(Math.Asin(delta_move.X / 100.0)));
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.W:
                    //Front
                    Player.Walk(1.0f);
                    Hero.Walk(1.0f);
                    break;
                case Keys.A:
                    //Left
                    Player.Strafe(-1.0f);
                    Hero.Strafe(-1.0f);
                    break;
                case Keys.S:
                    //Back
                    Player.Walk(-1.0f);
                    Hero.Walk(-1.0f);
                    break;
                case Keys.D:
                    //Right
                    Player.Strafe(1.0f);
                    Hero.Strafe(1.0f);
                    break;
            }
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

            var srvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = 200,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            };

            shaderRenderViewHeap = device.CreateDescriptorHeap(srvHeapDesc);

            var meshCtrCbvDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = 1,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            };

            meshCtrBufferViewHeap = device.CreateDescriptorHeap(meshCtrCbvDesc);

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
                DescriptorCount = 1,
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
                                RangeType = DescriptorRangeType.ConstantBufferView,
                                DescriptorCount = 1,
                                OffsetInDescriptorsFromTableStart = 0,
                                BaseShaderRegister = 0
                            }
                        }),
                    new RootParameter(ShaderVisibility.All,
                        new RootConstants() {
                            ShaderRegister = 1,
                            Value32BitCount = 1
                        }),
                    new RootParameter(ShaderVisibility.All,
                        new []
                        {
                            new DescriptorRange()
                            {
                                RangeType = DescriptorRangeType.ShaderResourceView,
                                DescriptorCount = 2,
                                OffsetInDescriptorsFromTableStart = 0,
                                BaseShaderRegister = 0
                            }
                            //new DescriptorRange()
                            //{
                            //    RangeType = DescriptorRangeType.ShaderResourceView,
                            //    DescriptorCount = 1,
                            //    OffsetInDescriptorsFromTableStart = -1,
                            //    BaseShaderRegister = 1
                            //}
                        }),
                    new RootParameter(ShaderVisibility.Pixel,
                        new DescriptorRange()
                        {
                            RangeType = DescriptorRangeType.Sampler,
                            DescriptorCount = 1,
                            OffsetInDescriptorsFromTableStart = 0,
                            BaseShaderRegister = 0
                        }),
                });

            rootSignature = device.CreateRootSignature(0, rootSignatureDesc.Serialize());

            // Create the pipeline state, which includes compiling and loading shaders.
#if DEBUG
            var warn = SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shaders.hlsl"), "VSMain", "vs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug);
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shaders.hlsl"), "VSMain", "vs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("shaders.hlsl", "VSMain", "vs_5_0"));
#endif

#if DEBUG
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.Compile(SharpDX.IO.NativeFile.ReadAllText("../../shaders.hlsl"), "PSMain", "ps_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("shaders.hlsl", "PSMain", "ps_5_0"));
#endif

            // Define the vertex input layout.
            var inputElementDescs = new[]
            {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                    new InputElement("TANGENT", 0, Format.R32G32B32_Float, 24, 0),
                    new InputElement("BITANGENT", 0, Format.R32G32B32_Float, 36, 0),
                    new InputElement("DIFFUSE", 0, Format.R32G32B32_Float, 48, 0),
                    new InputElement("EMISSIVE", 0, Format.R32G32B32_Float, 64, 0),
                    new InputElement("SPECULAR", 0, Format.R32G32B32_Float, 80, 0),
                    new InputElement("TEXCOORD", 0, Format.R32G32_Float, 96, 0)
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

            // build constant buffer

            constantBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(1024 * 64), ResourceStates.GenericRead);

            var cbDesc = new ConstantBufferViewDescription()
            {
                BufferLocation = constantBuffer.GPUVirtualAddress,
                SizeInBytes = (Utilities.SizeOf<ConstantBufferData>() + 255) & ~255
            };
            device.CreateConstantBufferView(cbDesc, shaderRenderViewHeap.CPUDescriptorHandleForHeapStart);

            constantBufferData = new ConstantBufferData
            {
                Wrold = Matrix.Identity,
                View = Matrix.Identity,
                Project = Matrix.Identity,
                TexsCount = 1
            };

            constantBufferPointer = constantBuffer.Map(0);
            Utilities.Write(constantBufferPointer, ref constantBufferData);

            //build mesh controll buffer

            meshCtrBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(1024 * 64), ResourceStates.GenericRead);

            cbDesc = new ConstantBufferViewDescription()
            {
                BufferLocation = meshCtrBuffer.GPUVirtualAddress,
                SizeInBytes = (Utilities.SizeOf<MeshCtrBufferData>() + 255) & ~255
            };
            device.CreateConstantBufferView(cbDesc, meshCtrBufferViewHeap.CPUDescriptorHandleForHeapStart);

            meshCtrBufferData = new MeshCtrBufferData
            {
                TexsCount = 1
            };

            meshCtrBufferPointer = meshCtrBuffer.Map(0);
            Utilities.Write(meshCtrBufferPointer, ref meshCtrBufferData);

            //model test
            var modePath = "../../models/MikuDeepSea/";
            Model model = Model.LoadFromFile(modePath + "DeepSeaGirl.x");

            Vertex[] triangleVertices;
            int[] triangleIndexes;
            List<Texture> texs;
            byte[] textureData;
            GCHandle handle;
            IntPtr ptr;
            ResourceDescription textureDesc;
            //int viewStep = 0;
            int viewStep = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            bufferViews = new List<BufferView>();
            
            foreach (ModelComponent m in model.Components)
            {
                if (m.TexturePath != null)
                {
                    texs = Texture.LoadFromFile(modePath, m.TexturePath);
                }
                else
                {
                    continue;
                    //texs = Texture.LoadFromFile(modePath, "tex/jacket.png");
                }

                int texsCount = 0;
                foreach (Texture tex in texs)
                {
                    textureData = tex.Data;
                    textureDesc = ResourceDescription.Texture2D(tex.ColorFormat, tex.Width, tex.Height, 1, 1, 1, 0, ResourceFlags.None, TextureLayout.Unknown, 0);
                    // Create the texture.
                    // Describe and create a Texture2D.
                    texture = device.CreateCommittedResource(
                        new HeapProperties(HeapType.Upload),
                        HeapFlags.None,
                        textureDesc,
                        ResourceStates.GenericRead, null);

                    // Copy data to the intermediate upload heap and then schedule a copy 
                    // from the upload heap to the Texture2D.          

                    handle = GCHandle.Alloc(textureData, GCHandleType.Pinned);
                    ptr = Marshal.UnsafeAddrOfPinnedArrayElement(textureData, 0);
                    texture.WriteToSubresource(0, null, ptr, tex.Width * tex.PixelWdith, textureData.Length);
                    handle.Free();

                    // Describe and create a SRV for the texture.
                    device.CreateShaderResourceView(
                        texture,
                        new ShaderResourceViewDescription
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
                            }
                        },
                        shaderRenderViewHeap.CPUDescriptorHandleForHeapStart + viewStep + texsCount * device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView));

                    texsCount++;
                    //break;
                }

                triangleVertices = (new Func<Vertex[]>(() =>
                {
                    var v = new Vertex[m.Vertices.Length];
                    for (int i = 0; i < m.Vertices.Length; i++)
                    {
                        v[i].Position = m.Vertices[i];
                        v[i].TexCoord = m.UV[i];
                        v[i].Normal = m.Normals[i];
                        //v[i].Tangent = m.Tangents[i];
                        //v[i].BiTangent = m.BiTangents[i];
                        v[i].Diffuse = m.Diffuse;
                        v[i].emissive = m.Emissive;
                        v[i].Specular = m.Specular;
                    }
                    return v;
                }))();
                triangleIndexes = m.Indices;

                // build vertex buffer
                vertexBuffer = device.CreateCommittedResource(
                    new HeapProperties(HeapType.Upload), 
                    HeapFlags.None, 
                    ResourceDescription.Buffer(Utilities.SizeOf(triangleVertices)), 
                    ResourceStates.GenericRead);
                Utilities.Write(vertexBuffer.Map(0), triangleVertices, 0, triangleVertices.Length);
                vertexBuffer.Unmap(0);

                // build index buffer
                indexBuffer = device.CreateCommittedResource(
                    new HeapProperties(HeapType.Upload), 
                    HeapFlags.None, 
                    ResourceDescription.Buffer(Utilities.SizeOf(triangleIndexes)), 
                    ResourceStates.GenericRead);
                Utilities.Write(indexBuffer.Map(0), triangleIndexes, 0, triangleIndexes.Length);
                indexBuffer.Unmap(0);

                bufferViews.Add(new BufferView()
                {
                    vertexBufferView = new VertexBufferView()
                    {
                        BufferLocation = vertexBuffer.GPUVirtualAddress,
                        StrideInBytes = Utilities.SizeOf<Vertex>(),
                        SizeInBytes = Utilities.SizeOf(triangleVertices)
                    },
                    indexBufferView = new IndexBufferView()
                    {
                        BufferLocation = indexBuffer.GPUVirtualAddress,
                        SizeInBytes = Utilities.SizeOf(triangleIndexes),
                        Format = Format.R32_UInt
                    },
                    IndexCount = triangleIndexes.Length,
                    ViewStep = viewStep,
                    TexsCount = texsCount
                });

                viewStep += texsCount * device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            }
            
            //===========

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

            commandList.SetViewport(viewport);
            commandList.SetScissorRectangles(scissorRect);
            commandList.ResourceBarrierTransition(renderTargets[frameIndex], ResourceStates.Present, ResourceStates.RenderTarget);
            var rtvHandle = renderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            rtvHandle += frameIndex * rtvDescriptorSize;
            
            
            commandList.ClearDepthStencilView(handleDSV, ClearFlags.FlagsDepth, 1.0f, 0);
            commandList.ClearRenderTargetView(rtvHandle, new Color4(0, 0.2F, 0.4f, 1), 0, null);
            commandList.SetRenderTargets(1, rtvHandle, false, handleDSV);

            DescriptorHeap[] descHeaps = new[] { samplerViewHeap, shaderRenderViewHeap }; // shaderRenderViewHeap, 
            commandList.SetDescriptorHeaps(descHeaps.GetLength(0), descHeaps);
            commandList.SetGraphicsRootDescriptorTable(0, shaderRenderViewHeap.GPUDescriptorHandleForHeapStart);
            commandList.SetGraphicsRootDescriptorTable(3, samplerViewHeap.GPUDescriptorHandleForHeapStart);
            commandList.PipelineState = pipelineState;

            commandList.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

            foreach (BufferView b in bufferViews)
            {
                commandList.SetGraphicsRoot32BitConstant(1, b.TexsCount, 0);
                commandList.SetGraphicsRootDescriptorTable(2, shaderRenderViewHeap.GPUDescriptorHandleForHeapStart + b.ViewStep);
                commandList.SetVertexBuffer(0, b.vertexBufferView);
                commandList.SetIndexBuffer(b.indexBufferView);
                commandList.DrawIndexedInstanced(b.IndexCount, 1, 0, 0, 0);
            }
            //int i = 15;
            //commandList.SetGraphicsRootDescriptorTable(1, shaderRenderViewHeap.GPUDescriptorHandleForHeapStart + bufferViews[i].ViewStep);
            //commandList.SetVertexBuffer(0, bufferViews[i].vertexBufferView);
            //commandList.SetIndexBuffer(bufferViews[i].indexBufferView);
            //commandList.DrawIndexedInstanced(bufferViews[i].IndexCount, 1, 0, 0, 0);
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

        Matrix p = Matrix.Identity;

        public void Update()
        {
            Player.SetLens(
                MathUtil.Pi / 3f, 
                viewport.Width / viewport.Height, 
                1.0f, 
                1000.0f);
            Hero.SetLens(
                MathUtil.Pi / 3f,
                viewport.Width / viewport.Height,
                1.0f,
                1000.0f);
            Player.Update();
            Hero.Update();
            World = Matrix.Identity;
            World *= Matrix.Scaling(Scalling);
            World *= Matrix.Translation(Hero.Position);
            World *= Matrix.RotationY(Hero.Angle.Y);
            World *= Matrix.RotationX(Hero.Angle.X);
            constantBufferData.Wrold = World;
            constantBufferData.View = Player.View;
            constantBufferData.Project = Player.Project;

            constantBufferData.Light = new Light
            {
                Type = LightSourceType.Point,
                Ambient = new Vector4(1f, 1f, 1f, 1f),
                Diffuse = new Vector4(1f, 1f, 1f, 1f),
                Specular = new Vector4(1f, 1f, 1f, 1f),
                Position = new Vector3(-10f, -10f, -10f),
                Range = 1000f,
                Attenuation = new Vector3(1f, 0f, 0f)
            };

            Utilities.Write(constantBufferPointer, ref constantBufferData);
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
