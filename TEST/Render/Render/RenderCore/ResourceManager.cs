using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render
{
    using SharpDX.Direct3D12;
    public class ResourceManager : IDisposable
    {
        public ConcurrentDictionary<string, DescriptorHeap> DescriptorHeaps = new ConcurrentDictionary<string, DescriptorHeap>();

        public ConcurrentDictionary<string, Resource>       Resources       = new ConcurrentDictionary<string, Resource>();

        public ConcurrentDictionary<string, Shader>         Shaders         = new ConcurrentDictionary<string, Shader>();

        public ConcurrentDictionary<string, Pipeline>       Pipelines       = new ConcurrentDictionary<string, Pipeline>();      

        public ConcurrentDictionary<string, Material>       Materials       = new ConcurrentDictionary<string, Material>();

        public ConcurrentDictionary<string, Model>          Models          = new ConcurrentDictionary<string, Model>();

        /// <summary>
        /// Initialize all resources
        /// </summary>
        public void Initialize()
        {
            InitShaderEvent(Add);
            InitPipelineEvent(Add,Shaders);
            InitDescriptorHeapEvent(Add);
            InitResourceEvent(Add, DescriptorHeaps);
            InitMaterialEvent(Add);
            InitModelEvent(Add);
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        public void Dispose()
        {
            foreach (var p in Pipelines.Values)
                p.State.Dispose();
            //foreach (var s in Shaders.Values)
            //    s.Bytecode.Dispose();
            foreach (var m in Materials.Values)
                m.ToString();// Dispose
            foreach (var m in Models.Values)
                m.ToString();// Dispose
        }

        /// <summary>
        /// Add shader to resource manager
        /// </summary>
        /// <param name="Identity"></param>
        /// <param shader="Shader"></param>
        /// <returns>Return false if the identity already exists.</returns>
        public delegate bool AddResource(string name, Resource resource);
        public delegate void InitResourceHandler(AddShader add, ConcurrentDictionary<string, DescriptorHeap> heaps);
        public event InitResourceHandler InitResourceEvent = delegate { };
        private bool Add(string name, Resource resource)
        {
            return Resources.TryAdd(name, resource);
        }

        /// <summary>
        /// Add description heap to resource manager
        /// </summary>
        /// <param name="Identity"></param>
        /// <param description="Description"></param>
        /// <returns>Return false if the identity already exists.</returns>
        public delegate bool AddDescriptorHeap(string name, DescriptorHeapDescription description);
        public delegate void InitDescriptorHeap(AddDescriptorHeap add);
        public event InitDescriptorHeap InitDescriptorHeapEvent = delegate { };
        private bool Add(string name, DescriptorHeapDescription description)
        {
            DescriptorHeap d = Engine.Instance.Core.Device.CreateDescriptorHeap(description);
            if (DescriptorHeaps.TryAdd(name, d))
                return true;
            d.Dispose();
            return false;
        }

        /// <summary>
        /// Add shader to resource manager
        /// </summary>
        /// <param name="Identity"></param>
        /// <param shader="Shader"></param>
        /// <returns>Return false if the identity already exists.</returns>
        public delegate bool AddShader(string name, Shader shader);
        public delegate void InitShaderHandler(AddShader add);
        public event InitShaderHandler InitShaderEvent = delegate { };
        private bool Add(string name, Shader shader)
        {
            return Shaders.TryAdd(name, shader);
        }

        /// <summary>
        /// Add pipeline to resource manager
        /// </summary>
        /// <param name="Identity"></param>
        /// <param description="Pipeline Description"></param>
        /// <returns>Return false if the identity already exists.</returns>
        public delegate bool AddPipeline(string name, GraphicsPipelineStateDescription description);
        public delegate void InitPipelineHandler(AddPipeline add, ConcurrentDictionary<string, Shader> shaders);
        public event InitPipelineHandler InitPipelineEvent = delegate { };
        private bool Add(string name, GraphicsPipelineStateDescription description)
        {
            Pipeline pipeline = new Pipeline();
            pipeline.Description = description;
            pipeline.Description.RootSignature = Engine.Instance.Core.RootSignature;
            pipeline.State = Engine.Instance.Core.Device.CreateGraphicsPipelineState(pipeline.Description);
            return Pipelines.TryAdd(name, pipeline);
        }

        /// <summary>
        /// Add material to resource manager
        /// </summary>
        /// <param name="Identity"></param>
        /// <param material="Material"></param>
        /// <returns>Return false if the identity already exists.</returns>
        public delegate bool AddMaterial(string name, Material material);
        public delegate void InitMaterialHandler(AddMaterial add);
        public event InitMaterialHandler InitMaterialEvent = delegate { };
        private bool Add(string name, Material material)
        {
            return Materials.TryAdd(name, material);
        }

        /// <summary>
        /// Add model to resource manager
        /// </summary>
        /// <param name="Identity"></param>
        /// <param model="Model"></param>
        /// <returns>Return false if the identity already exists.</returns>
        public delegate bool AddModel(string name, Model model);
        public delegate void InitModelHandler(AddModel add);
        public event InitModelHandler InitModelEvent = delegate { };
        private bool Add(string name, Model model)
        {
            return Models.TryAdd(name, model);
        }
    }
}
