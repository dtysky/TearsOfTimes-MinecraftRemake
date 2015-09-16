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

cbuffer MeshCtrBufferData : register(b1)
{
	int TexsCount;
}

Texture2D g_texture : register(t0);
Texture2D g_texture1 : register(t1);
SamplerState g_sampler : register(s0);

PSInput VSMain(VSInput input)
{
	PSInput result;

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

float4 PSMain1(PSInput input) : SV_TARGET
{
	float4 lightDirection = input.position - float4(light.position, 1);
	float4 D;
	if (TexsCount == 1)
	{
		D = g_texture.Sample(g_sampler, input.texcoord);
		//D = float4(0, 0, 1, 0);
	}
	else
	{
		//D = float4(0, 1, 0, 0);
		D = g_texture1.Sample(g_sampler, input.texcoord) * g_texture.Sample(g_sampler, input.texcoord);
	}
	//D = (g_texture1.Sample(g_sampler, input.texcoord) + g_texture.Sample(g_sampler, input.texcoord)) / 2;
	/*D = g_texture.Sample(g_sampler, input.texcoord);
	float distance = length(lightDirection.xyz) / 1000;
	lightDirection = normalize(lightDirection);

	return saturate(dot(normalize(input.normal), -lightDirection))*D / distance;*/
	return D;
}

float4 PSMain(PSInput input) : SV_TARGET
{
	float4 lightDirection = input.position - float4(light.position, 0);
	float4 diffuse = g_texture.Sample(g_sampler, input.texcoord);
	float distance = length(lightDirection.xyz);
	lightDirection = normalize(lightDirection);

	float lightIntensity = dot(input.normal, -lightDirection);

	float4 lightColor = input.light[0] * lightIntensity + input.light[1] * lightIntensity + input.light[2];

	float garma = 1.5;
	if (TexsCount != 1)
	{
		return garma * (saturate((g_texture1.Sample(g_sampler, input.texcoord) * lightColor) / (pow(distance, 0.5))) * diffuse * 0.4 + diffuse * 0.6);
	}
	return garma * (saturate(lightColor / (pow(distance,0.5))) * diffuse * 0.4 + diffuse * 0.6);
}