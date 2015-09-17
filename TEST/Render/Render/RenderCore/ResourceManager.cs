using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render.Core
{
    public static class ResourceManager
    {
        public static Dictionary<string, Pipeline> Pipelines = new Dictionary<string,Pipeline>();

        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static Dictionary<string, Model> Models = new Dictionary<string, Model>();

    }
}
