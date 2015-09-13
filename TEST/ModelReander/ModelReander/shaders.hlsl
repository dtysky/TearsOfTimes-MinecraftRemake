struct VSInput
{
	float4 position : POSITION;
	float3 normal : NORMAL;
	float3 tangent: TANGENT;
	float3 bitangent : BITANGENT;
	float2 texcoord : TEXCOORD;
};

struct PSInput
{
	float4 position : SV_POSITION;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD;
};

struct GSInput
{
	float4 position : SV_POSITION;
	float2 texcoord : TEXCOORD;
};

cbuffer ConstantBufferData : register(b0)
{
	float4x4 world;
	float4x4 worldViewProj;
	float4 lightDirection;
	float4 viewDirection;
	float bias;
};

Texture2D g_texture : register(t0);
SamplerState g_sampler : register(s0);

PSInput VSMain(VSInput input)
{
	PSInput result;

	result.position = mul(worldViewProj, input.position);
	result.normal = mul(world, input.normal);
	result.texcoord = input.texcoord;

	return result;

}

//[maxvertexcount(72)]
//void GSMain(triangle GSInput input[3], inout TriangleStream<PSInput> TriStream)
//{
//	PSInput result[3];
//	float4 P = float4(0.0f, 0.0f, 0.0f, 1);
//
//	for (int i = 0; i < 5; i++)
//	{
//		result[0].position = mul(project, input[0].position + P);
//		result[0].uv = input[0].uv;
//		result[1].position = mul(project, input[1].position + P);
//		result[1].uv = input[1].uv;
//		result[2].position = mul(project, input[2].position + P);
//		result[2].uv = input[2].uv;
//		P = P + float4(2.0f, 2.0f, 2.0f, 0);
//
//		TriStream.Append(result[0]);
//		TriStream.Append(result[1]);
//		TriStream.Append(result[2]);
//		TriStream.RestartStrip();
//	}
//}

float4 PSMain(PSInput input) : SV_TARGET
{
	float4 D = g_texture.Sample(g_sampler, input.texcoord);

	return saturate(dot(normalize(input.normal), lightDirection))*D + 0.2F;
}
