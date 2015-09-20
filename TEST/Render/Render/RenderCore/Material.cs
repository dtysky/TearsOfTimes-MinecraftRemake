using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render
{
    using Assimp;
    using SharpDX;
    using SharpDX.Direct3D12;
    using System.Runtime.InteropServices;

    public class Material
    {
        private Material()
        {
            TextureCount = 0;
            ViewStep = 0;
            Light = new MaterialLight();
        }

        public int Initialize(string rootPath, int viewStep, Assimp.Material meterial)
        {
            Name = meterial.Name;
            if (meterial.TextureDiffuse.FilePath != null)
            {
                TexturePath = meterial.TextureDiffuse.FilePath.Split('*');
                TextureCount = TexturePath.Length;
                for (int i = 0; i < TextureCount; i++)
                {
                    TexturePath[i] = rootPath + TexturePath[i];
                }
            }
            else
            {
                TextureCount = 0;
            }
            ViewStep = viewStep;
            Light.Opacity = meterial.Opacity;
            Light.Shininess = meterial.Shininess;
            Light.ShininessStrength = meterial.ShininessStrength;
            for(int i = 0; i < 4; i++)
            {
                Light.Ambient[i] = meterial.ColorAmbient[i];
                Light.Diffuse[i] = meterial.ColorDiffuse[i];
                Light.Emissive[i] = meterial.ColorEmissive[i];
                Light.Reflective[i] = meterial.ColorReflective[i];
                Light.Specular[i] = meterial.ColorSpecular[i];
                Light.Transparent[i] = meterial.ColorTransparent[i];
            }
            return TextureCount + 1;
        }

        public void WriteToResource(Device device, DescriptorHeap shaderRenderViewHeap)
        {
            //for (int i = 0; i < TextureCount; i++)
            //{
            //    var tex = Texture.LoadFromFile(TexturePath[i]);
            //    var textureData = tex.Data;
            //    var textureDesc = ResourceDescription.Texture2D(tex.ColorFormat, tex.Width, tex.Height, 1, 1, 1, 0, ResourceFlags.None, TextureLayout.Unknown, 0);
            //    var texture = device.CreateCommittedResource(
            //        new HeapProperties(HeapType.Upload),
            //        HeapFlags.None,
            //        textureDesc,
            //        ResourceStates.GenericRead, null);
            //    var handle = GCHandle.Alloc(textureData, GCHandleType.Pinned);
            //    var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(textureData, 0);
            //    texture.WriteToSubresource(0, null, ptr, tex.Width * tex.PixelWdith, textureData.Length);
            //    handle.Free();
            //    device.CreateShaderResourceView(
            //        texture,
            //        new ShaderResourceViewDescription
            //        {
            //            Shader4ComponentMapping = ((((0) & 0x7) | (((1) & 0x7) << 3) | (((2) & 0x7) << (3 * 2)) | (((3) & 0x7) << (3 * 3)) | (1 << (3 * 4)))),
            //            Format = textureDesc.Format,
            //            Dimension = ShaderResourceViewDimension.Texture2D,
            //            Texture2D =
            //            {
            //                    MipLevels = 1,
            //                    MostDetailedMip = 0,
            //                    PlaneSlice = 0,
            //                    ResourceMinLODClamp = 0.0f
            //            }
            //        },
            //        shaderRenderViewHeap.CPUDescriptorHandleForHeapStart + ViewStep + i * device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView));
            //}
        }

        public bool SetTexture(string rootPath, string texturePath)
        {
            if (texturePath != null && texturePath.Count(c => c == '*') == TextureCount - 1)
            {
                TexturePath = texturePath.Split('*');
                TextureCount = TexturePath.Length;
                for (int i = 0; i < TextureCount; i++)
                {
                    TexturePath[i] = rootPath + TexturePath[i];
                }
                return true;
            }
            return false;
        }

        public void WriteTexture(Device device, DescriptorHeap shaderRenderViewHeap)
        {

        }

        public void WriteTexture(Device device, DescriptorHeap shaderRenderViewHeap, int offset)
        {

        }

        public void SetLight(MaterialLight light)
        {
            Light = light;
        }

        public void WriteLight(Device device, DescriptorHeap shaderRenderViewHeap)
        {
            var bufferDesc = ResourceDescription.Buffer(new ResourceAllocationInformation(), ResourceFlags.None);
            IntPtr bufferData = Utilities.AllocateMemory(Utilities.SizeOf<MaterialLight>());
            Utilities.Write(bufferData, ref Light);         
            var buffer = device.CreateCommittedResource(
                new HeapProperties(HeapType.Upload),
                HeapFlags.None,
                bufferDesc,
                ResourceStates.GenericRead, null);
            buffer.WriteToSubresource(0, null, bufferData, width * pixelwidth, length);


            device.CreateShaderResourceView(
                buffer,
                new ShaderResourceViewDescription
                {
                    Shader4ComponentMapping = ((((0) & 0x7) | (((1) & 0x7) << 3) | (((2) & 0x7) << (3 * 2)) | (((3) & 0x7) << (3 * 3)) | (1 << (3 * 4)))),
                    Format = bufferDesc.Format,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D =
                    {
                                MipLevels = 1,
                                MostDetailedMip = 0,
                                PlaneSlice = 0,
                                ResourceMinLODClamp = 0.0f
                    }
                },
                shaderRenderViewHeap.CPUDescriptorHandleForHeapStart + ViewStep);
        }

        public int TextureCount { get; private set; }
        public int ViewStep { get; private set; }
        public string Name { get; private set; }
        
        private string[] TexturePath;
        private MaterialLight Light;
    }

}
