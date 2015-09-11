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
            Context.SetConfig(new NormalSmoothingAngleConfig(66.0f));
            Scene scene = Context.ImportFile(Path);
            foreach( Mesh mesh in scene.Meshes )
            {
                model.Components.Add(new ModelComponent(
                    mesh.Name, 
                    mesh.Vertices.ToArray(), 
                    mesh.Faces, 
                    mesh.TextureCoordinateChannels[0].ToArray(), 
                    scene.Materials[mesh.MaterialIndex])
                    );
            }

            return model;
        }
    }

    class ModelComponent
    {
        public ModelComponent(string name,Vector3D[] vertices, List<Face> faces, Vector3D[] uv, Material material)
        {
            Vertices = new ModelRender.Vertex[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vertices[i].Position.X = vertices[i].X;
                Vertices[i].Position.Y = vertices[i].Y;
                Vertices[i].Position.Z = vertices[i].Z;
                Vertices[i].TexCoord.X = uv[i].X;
                Vertices[i].TexCoord.Y = uv[i].Y;
            }
            List<uint> index = new List<uint>();
            foreach (var face in faces)
                foreach (var i in face.Indices)
                    index.Add((uint)i);
            Indices = index.ToArray();
            Material = material;
        }

        public string Name { get; }
        public uint[] Indices { get; }
        public Material Material { get; }

        public ModelRender.Vertex[] Vertices { get; } 
    }
}
