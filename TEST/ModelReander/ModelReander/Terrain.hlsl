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
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD;
	//1: diffuse
	//2: emissive
	float4 light[3] : COLOR;
};

struct GSInput
{
	float4 position : SV_POSITION;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD;
};

struct Light
{
	int type;
	float4 diffuse;
	float4 specular;
	float4 ambient;
	float3 position;
	float3 direction;
	float3 attenuation;
	float range;
	float palloff;
	float theta;
	float phi;
};

cbuffer ConstantBufferData : register(b0)
{
	matrix world;
	matrix view;
	matrix project;
	Light light;
};

Texture2D heightMap : register(t0);
Texture2D terrainTexture : register(t1);
SamplerState heightMap_Sampler : register(s0);
SamplerState terrainTexture_Sampler : register(s1);

PSInput VSMain(VSInput input)
{
	PSInput result;
	input.position.y = heightMap.Sample(heightMap_Samper, float2(input.position.x, input.position, z);
	result.position = mul(world, input.position);
	result.position = mul(view, result.position);
	result.position = mul(project, result.position);
	result.normal = mul(world, input.normal);
	result.texcoord = input.texcoord;
	result.light[0] = input.diffuse;
	result.light[2] = input.emissive;
	result.light[1] = input.specular;
	return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
	float4 lightDirection = input.position - float4(light.position, 0);
	float4 diffuse = terrainTexture.Sample(terrainTexture_Sampler, input.texcoord);
	float distance = length(lightDirection.xyz);
	lightDirection = normalize(lightDirection);
	float lightIntensity = dot(input.normal, -lightDirection);
	float4 lightColor = input.light[0] * lightIntensity + input.light[1] * lightIntensity + input.light[2];
	float garma = 1.5;
	return garma * (saturate(lightColor / (pow(distance,0.5))) * diffuse * 0.4 + diffuse * 0.6);
}