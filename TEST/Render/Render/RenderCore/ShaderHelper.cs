

namespace Render
{
    using SharpDX;

    public struct Vertex
    {
        public Vector3 Position;
    }

    public struct Vertex2
    {
        public Vector3 Position;
        public Vector4 Color;
    }

    public struct Vertex3
    {
        public Vector3 Position;
        public Vector2 TexCoord;
    }

    public struct Vertex4
    {
        public Vector3 Position;
        public Vector2 TexCoord;
        public Vector3 Normal;
        public Vector3 Tangent;         
    }

    public struct Vertex5 
    {
        public Vector3 Position;
        public Vector2 TexCoord;
        public Vector3 BiTangent;
        public Vector4 Diffuse;
        public Vector4 Emissive;
        public Vector4 Specular;
    }

    public struct MaterialLight
    {
        public float Shininess;
        public float ShininessStrength;
        public float Opacity; 
        public Vector4 Ambient;
        public Vector4 Diffuse;
        public Vector4 Emissive;
        public Vector4 Reflective;
        public Vector4 Specular;
        public Vector4 Transparent;
    }

    public class Light
    {
        public LightSource Type;
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

    public enum LightSource
    {
        Undefined = 0,
        Directional = 1,
        Point = 2,
        Spot = 3
    }
}
