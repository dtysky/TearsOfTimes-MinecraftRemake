struct VSInput
{
	float4 position : POSITION;
	float3 normal : NORMAL;
	float3 tangent: TANGENT;
	float3 bitangent : BITANGENT;
	float4 diffuse : DIFFUSE;
	float4 emissive : EMISSIVE;
	float4 specular : SPECULAR;
	float2 texcoord : TEXCOORD;
};

struct PSInput
{
	float4 position : SV_POSITION;
	float2 texcoord : TEXCOORD;
};

cbuffer ConstantBufferData : register(b0)
{
	matrix world;
	matrix view;
	matrix project;
};

Texture2D heightMap : register(t0);
Texture2D terrainTexture : register(t1);
SamplerState Sampler : register(s0);

PSInput VSMain(VSInput input)
{
	PSInput result;
	result.position = input.position;
	//result.position.y = heightMap.Sample(Sampler, float2(input.position.x,input.position.z));
	//result.position.y = heightMap.SampleLevel(Sampler, float2(input.position.x, input.position.z),0).r;
	result.position = mul(world, result.position);
	result.position = mul(view, result.position);
	result.position = mul(project, result.position);
	result.texcoord = input.texcoord;
	return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
	return terrainTexture.Sample(Sampler, input.texcoord);
}