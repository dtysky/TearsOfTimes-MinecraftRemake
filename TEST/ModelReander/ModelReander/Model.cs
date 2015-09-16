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
                    mesh.Normals,
                    mesh.Tangents,
                    mesh.BiTangents,
                    scene.Materials[mesh.MaterialIndex])
                    );
            }
            
            return model;
        }
    }

    class ModelComponent
    {
        public ModelComponent(string name,List<Vector3D> vertices, int[] indices, List<Vector3D> uv, List<Vector3D> normals, List<Vector3D> tangents, List<Vector3D> bitangents, Material material)
        {
            Vertices = new Vector3[vertices.Count];
            for(int i = 0; i < vertices.Count; i++)
            {
                Vertices[i].X = vertices[i].X;
                Vertices[i].Y = vertices[i].Y;
                Vertices[i].Z = vertices[i].Z;
            }
            UV = new Vector2[uv.Count];
            for(int i = 0; i < uv.Count; i++)
            {
                UV[i].X = uv[i].X;
                UV[i].Y = uv[i].Y;
            }
            Normals = new Vector3[normals.Count];
            for (int i = 0; i < normals.Count; i++)
            {
                Normals[i].X = normals[i].X;
                Normals[i].Y = normals[i].Y;
                Normals[i].Z = normals[i].Z;
            }
            Tangents = new Vector3[tangents.Count];
            for (int i = 0; i < tangents.Count; i++)
            {
                Tangents[i].X = tangents[i].X;
                Tangents[i].Y = tangents[i].Y;
                Tangents[i].Z = tangents[i].Z;
            }
            BiTangents = new Vector3[bitangents.Count];
            for (int i = 0; i < bitangents.Count; i++)
            {
                BiTangents[i].X = bitangents[i].X;
                BiTangents[i].Y = bitangents[i].Y;
                BiTangents[i].Z = bitangents[i].Z;
            }
            Diffuse = new Vector4()
            {
                X = material.ColorDiffuse.R,
                Y = material.ColorDiffuse.G,
                Z = material.ColorDiffuse.B,
                W = material.ColorDiffuse.A
            };
            Emissive = new Vector4()
            {
                X = material.ColorEmissive.R,
                Y = material.ColorEmissive.G,
                Z = material.ColorEmissive.B,
                W = material.ColorEmissive.A
            };
            Specular = new Vector4()
            {
                X = material.ColorSpecular.R,
                Y = material.ColorSpecular.G,
                Z = material.ColorSpecular.B,
                W = material.ColorSpecular.A
            };
            Indices = indices;
            TexturePath = material.TextureDiffuse.FilePath;
        }

        public string Name { get; }
        public Vector3[] Vertices { get; }
        public int[] Indices { get; }
        public Vector2[] UV { get; }
        public Vector3[] Normals { get; }
        public Vector3[] Tangents { get; }
        public Vector3[] BiTangents { get; }
        public Vector4 Diffuse { get; }
        public Vector4 Emissive { get; }
        public Vector4 Specular { get; }
        public string TexturePath { get; }

        public Matrix MatrixProject { get; }
    }
}
