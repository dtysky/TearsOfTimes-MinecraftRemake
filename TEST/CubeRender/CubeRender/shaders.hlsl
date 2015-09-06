struct PSInput
{
	float4 position : SV_POSITION;
	float4 color : COLOR;
};

cbuffer ConstantBufferData : register(b0)
{
	float4 position_offset;
	float4 color_offset;
};

PSInput VSMain(float4 position : POSITION, float4 color : COLOR)
{
	PSInput result;

	result.position = position+ position_offset;
	result.color = color + color_offset;

	return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
	return input.color;
}
