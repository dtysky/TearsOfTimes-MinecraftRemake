using System;
using System.Collections.Generic;

namespace Render
{
    using SharpDX.Direct3D12;
    using ShaderBytecode = SharpDX.Direct3D12.ShaderBytecode;
    public class Pipeline
    {
        public GraphicsPipelineStateDescription Description;
        public PipelineState State;
    }

    public class Shader
    {
        public ShaderType Type;
        public ShaderBytecode Bytecode;      
    }

    public enum ShaderType
    {
        Domain,
        Geometry,
        Hull,
        Pixel,
        Vertex
    }
}
