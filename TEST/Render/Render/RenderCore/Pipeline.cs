using System;
using System.Collections.Generic;

namespace Render.Core
{
    using SharpDX.Direct3D12;
    using SharpDX.D3DCompiler;
    public class Pipeline : IDisposable
    {
        public Pipeline()
        {

        }

        public void Dispose()
        {
            State.Dispose();
        }

        public void Initialize()
        {
            State = Engine.Device.CreateGraphicsPipelineState(Description);
        }       

        public GraphicsPipelineStateDescription Description;

        public PipelineState State { get; private set; }
    }

    public class Shader : IDisposable
    {
        private Shader()
        {

        }

        public void Dispose()
        {
            Bytecode.Dispose();
        }

        public static Shader CompileFromFile(ShaderType Type, string Path, string Entry = "",ShaderFlags Flags = ShaderFlags.None)
        {
            Shader S = new Shader();
            S.Type = Type;
            S.Bytecode = (Entry == "") ?
                SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile(Path, TypeMap[Type], Flags) :
                SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile(Path, Entry, TypeMap[Type], Flags);
            return S;
        }

        public static Shader CompileFrom(ShaderType Type, string ShaderString, string Entry = "", ShaderFlags Flags = ShaderFlags.None)
        {
            Shader S = new Shader();
            S.Type = Type;
            S.Bytecode = (Entry == "") ?
                SharpDX.D3DCompiler.ShaderBytecode.Compile(ShaderString, TypeMap[Type], Flags) :
                SharpDX.D3DCompiler.ShaderBytecode.Compile(ShaderString, Entry, TypeMap[Type], Flags);
            return S;
        }

        public ShaderType Type { get; private set; }

        public enum ShaderType
        {
            Domain,
            Geometry,
            Hull,
            Pixel,
            Vertex
        }

        private static Dictionary<ShaderType, string> TypeMap = new Dictionary<ShaderType, string>()
        {
            { ShaderType.Domain,  "ds_5_0" },
            { ShaderType.Geometry,"gs_5_0" },
            { ShaderType.Hull,    "hs_5_0" },
            { ShaderType.Pixel,   "ps_5_0" },
            { ShaderType.Vertex,  "vs_5_0" }
        };

        SharpDX.D3DCompiler.ShaderBytecode Bytecode;
    }
}
