struct PSInput
{
	float4 position : SV_POSITION;
	float4 color : COLOR;
};

cbuffer ConstantBufferData : register(b0)
{
	float4 offset;
	PSInput cube[8];
};

PSInput VSMain(float4 position : POSITION, float4 color : COLOR)
{
	PSInput result;
	
	result.position = position + cube[0].position;
	result.color = color + cube[0].color;

	return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
	return input.color;
}