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
            ViewStep = 0;
            Color = new MaterialColor();
            TextureFlags = 0;
            ColorFlags = 0;
        }

        public int Initialize(string rootPath, string modelName, int viewStep, Assimp.Material meterial)
        {
            Name = modelName + "." + meterial.Name;
            ViewStep = viewStep;
            Color.Opacity = meterial.Opacity;
            Color.Shininess = meterial.Shininess;
            for(int i = 0; i < 4; i++)
            {
                Color.Ambient[i] = meterial.ColorAmbient[i];
                Color.Diffuse[i] = meterial.ColorDiffuse[i];
                Color.Emissive[i] = meterial.ColorEmissive[i];
                Color.Reflective[i] = meterial.ColorReflective[i];
                Color.Specular[i] = meterial.ColorSpecular[i];
                Color.Transparent[i] = meterial.ColorTransparent[i];
            }
            SetTexture(rootPath, meterial);
            return viewStep + 5 * Engine.Instance.Core.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        }

        public void SetTexture(string rootPath, Assimp.Material meterial)
        {
            SetTexture(rootPath, meterial.TextureDiffuse.FilePath, TextureType.Diffuse);
            SetTexture(rootPath, meterial.TextureNormal.FilePath, TextureType.Normal);
            SetTexture(rootPath, meterial.TextureDiffuse.FilePath, TextureType.Gamma);
            SetTexture(rootPath, meterial.TextureDiffuse.FilePath, TextureType.Shadow);
            SetTexture(rootPath, meterial.TextureDiffuse.FilePath, TextureType.Bump);
        }

        public void SetTexture(string rootPath, string texturePath, TextureType type)
        {
            if (texturePath != null)
            {
                TexturePath[type] = rootPath + texturePath;
                HasTexture[type] = true;
                TextureFlags |= 1 << (int)type;
            }
            else
            {
                //nothing changed
            }
        }

        public void WriteTextureToRAM(DescriptorHeap shaderRenderViewHeap)
        {
            for (TextureType type = TextureType.Diffuse; type < TextureType.Bump; type++)
            {
                if (!HasTexture[type])
                {
                    continue;
                }
                WriteTextureToRAM(shaderRenderViewHeap, type);
            }
        }

        public void WriteTextureToRAM(DescriptorHeap shaderRenderViewHeap, TextureType type)
        {
            var tex = Texture.LoadFromFile(TexturePath[type]);
            var textureData = tex.Data;
            var textureDesc = ResourceDescription.Texture2D(tex.ColorFormat, tex.Width, tex.Height, 1, 1, 1, 0, ResourceFlags.None, TextureLayout.Unknown, 0);
            var texture = Engine.Instance.Core.Device.CreateCommittedResource(
                new HeapProperties(HeapType.Upload),
                HeapFlags.None,
                textureDesc,
                ResourceStates.GenericRead, null);
            var handle = GCHandle.Alloc(textureData, GCHandleType.Pinned);
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(textureData, 0);
            texture.WriteToSubresource(0, null, ptr, tex.Width * tex.PixelWdith, textureData.Length);
            Marshal.FreeHGlobal(ptr);
            handle.Free();
            Engine.Instance.Core.Device.CreateShaderResourceView(
                texture,
                new ShaderResourceViewDescription
                {
                    Shader4ComponentMapping = ((((0) & 0x7) | (((1) & 0x7) << 3) | (((2) & 0x7) << (3 * 2)) | (((3) & 0x7) << (3 * 3)) | (1 << (3 * 4)))),
                    Format = textureDesc.Format,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D =
                    {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                            PlaneSlice = 0,
                            ResourceMinLODClamp = 0.0f
                    }
                },
                shaderRenderViewHeap.CPUDescriptorHandleForHeapStart + ViewStep + (int)type * Engine.Instance.Core.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView));
        }

        public void SetColor(MaterialColor color)
        {
            Color = color;
        }


        public int TextureFlags { get; private set; }
        public int ColorFlags { get; private set; }
        public int ViewStep { get; private set; }
        public string Name { get; private set; }

        private Dictionary<TextureType, string> TexturePath;
        private Dictionary<TextureType, bool> HasTexture;
        private MaterialColor Color;
    }

}
