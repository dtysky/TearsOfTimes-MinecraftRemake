struct PSInput
{
	float4 position : SV_POSITION;
	float4 color : COLOR;
};

cbuffer ConstantBufferData : register(b0)
{
	float4x4 project;
};

PSInput VSMain(float4 position : POSITION, float4 color : COLOR)
{
	PSInput result = (PSInput)0;

	result.position = mul(project, position);
	result.color = color;

	return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
	return input.color;
}
