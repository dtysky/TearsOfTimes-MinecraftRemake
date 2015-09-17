using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
namespace ModelRender
{   
    class FollowingCamera : Camera
    {
        public FollowingCamera()
        {
            World = Matrix.Identity;
        }

        public void SetOffset(Vector3 offset)
        {
            Offset = offset;
        }

        public void SetOffset(float x, float y, float z)
        {
            Offset.X = x;
            Offset.Y = y;
            Offset.Z = z;
        }

        public new void Update()
        {
            base.Update();
            //update World
            World = Matrix.Translation(Position+Offset);
        }

        public Matrix World { get; private set; }

        private Vector3 Offset = new Vector3(0, -100f, 100f);

    }
}
