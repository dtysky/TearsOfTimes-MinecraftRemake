using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelReander
{
    using SharpDX;
    class FallowingCamera : ModelRender.Camera
    {
        public Vector3 Position { get; private set; }
        public Vector3 Angle { get; private set; }

        private Vector3 PositionInViewSpace;
        private Vector3 InnerAngle;

        public FallowingCamera(Vector3 positionInViewSpace)
        {
            PositionInViewSpace = positionInViewSpace;
            InnerAngle = new Vector3(0, 0, 0);
        }

        public new void Pitch(float angle)
        {
            base.Pitch(angle);
            InnerAngle.X += angle;
        }
        public new void RotateY(float angle)
        {
            base.RotateY(angle);
            InnerAngle.Y += angle;
        }

        public new void Update()
        {
            base.Update();
            var PosTmp = Vector3.Transform(PositionInViewSpace, base.MatrixCameraToWorld());
            Position = new Vector3(PosTmp.X, PosTmp.Y, PosTmp.Z);
            Angle = InnerAngle;
        }

    }
}
