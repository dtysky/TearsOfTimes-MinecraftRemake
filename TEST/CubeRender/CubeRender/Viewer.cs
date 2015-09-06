using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CubeRender
{
    using SharpDX;
    class Viewer
    {
        public Viewer()
        {

        }

        private Matrix World;
        private Matrix View;
        private Matrix Project;

        public Matrix GetProjection()
        {
            return  View * Project;
        }

        private Vector3 Position;
        private Vector3 Look;
        private Vector3 Right;
        private Vector3 Up;
        private float NearZ;
        private float FarZ;
        private float Aspect;
        private float FovY;
        private float NearWindowHeight;
        private float FarWindowHeight;


        public void SetPosition(Vector3 Position)
        {
            this.Position = Position;
        }

        public void SetLens(float fovY, float aspect, float zn, float zf)
        {
            FovY = fovY;
            Aspect = aspect;
            NearZ = zn;
            FarZ = zf;

            NearWindowHeight = 2.0f * NearZ * (float)Math.Tan(0.5f * FovY);
            FarWindowHeight = 2.0f * FarZ * (float)Math.Tan(0.5f * FovY);

            Project = Matrix.PerspectiveFovLH(FovY, Aspect, NearZ, FarZ);
        }

        public void LookAt(Vector3 Position, Vector3 Target, Vector3 WorldUp)
        {
            this.Position = Position;
            this.Look = Vector3.Normalize(Vector3.Subtract(Target, Position));
            this.Right = Vector3.Normalize(Vector3.Cross(WorldUp, Look));
            this.Up = Vector3.Cross(Look, Right);
        }

        public void UpdateViewMatrix()
        {
            Vector3 R = Right;
            Vector3 U = Up;
            Vector3 L = Look;
            Vector3 P = Position;

            // Keep camera's axes orthogonal to each other and of unit length.
            L = Vector3.Normalize(L);
            U = Vector3.Normalize(Vector3.Cross(L, R));

            // U, L already ortho-normal, so no need to normalize cross product.
            R = Vector3.Cross(U, L);

            // Fill in the view matrix entries.
            float x = -Vector3.Dot(P, R);
            float y = -Vector3.Dot(P, U);
            float z = -Vector3.Dot(P, L);

            Right = R;
            Up = U;
            Look = L;

            View = new Matrix(Right.X, Up.X, Look.X, 0.0f,
                              Right.Y, Up.Y, Look.Y, 0.0f,
                              Right.Z, Up.Z, Look.Z, 0.0f,
                              z,       y,    z     , 1.0f);
        }
    }
}
