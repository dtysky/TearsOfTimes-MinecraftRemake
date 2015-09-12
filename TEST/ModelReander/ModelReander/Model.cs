using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelRender
{
    using Assimp;
    using Assimp.Configs;
    using SharpDX;

    class Model
    {     
        private Model()
        {
            Components = new List<ModelComponent>();
        }

        public List<ModelComponent> Components { get; }

        static public Model LoadFromFile(string Path)
        {
            Model model = new Model();
            AssimpContext Context = new AssimpContext();
            Scene scene = Context.ImportFile(Path);
            foreach(Mesh mesh in scene.Meshes)
            {
                model.Components.Add(new ModelComponent(
                    mesh.Name, 
                    mesh.Vertices, 
                    mesh.GetIndices(), 
                    mesh.TextureCoordinateChannels[0], 
                    scene.Materials[mesh.MaterialIndex])
                    );
            }
            
            return model;
        }
    }

    class ModelComponent
    {
        public ModelComponent(string name,List<Vector3D> vertices, int[] indices, List<Vector3D> uv, Material material)
        {
            Vertices = new Vector3[vertices.Count];
            for(int i=0; i<vertices.Count; i++)
            {
                Vertices[i].X = vertices[i].X;
                Vertices[i].Y = vertices[i].Y;
                Vertices[i].Z = vertices[i].Z;
            }
            UV = new Vector2[uv.Count];
            for(int i=0; i<uv.Count; i++)
            {
                UV[i].X = uv[i].X;
                UV[i].Y = uv[i].Y;
            }
            Indices = indices;
            Material = material;
        }

        public string Name { get; }
        public Vector3[] Vertices { get; }
        public int[] Indices { get; }
        public Vector2[] UV { get; }
        public Material Material { get; }

        public Matrix MatrixProject { get; }
    }
}
