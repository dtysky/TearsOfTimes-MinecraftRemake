using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Minecraft.Common
{
    [DataContract]
    public class Profile
    {
        [DataMember]
        string Name;

        // TODO ...
    }
}
