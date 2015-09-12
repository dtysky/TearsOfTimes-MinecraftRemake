struct PSInput
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD;
};

struct GSInput
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD;
};

cbuffer ConstantBufferData : register(b0)
{
	float4x4 project;
};

Texture2D g_texture : register(t0);
SamplerState g_sampler : register(s0);

GSInput VSMain(float4 position : POSITION, float4 uv : TEXCOORD)
{
	PSInput result;
	result.position = mul(project, position);
	result.uv = uv;
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
	return g_texture.SampleLevel(g_sampler, input.uv, 0);
	//return float4(0.0f, 0.0f, 0.0f, 1.0f);
}
