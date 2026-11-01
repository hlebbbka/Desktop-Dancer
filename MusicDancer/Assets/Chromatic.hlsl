sampler2D input : register(s0);
float2 shift : register(c0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 radial = (uv - .5) * 2;
    float2 offset = shift * (radial + float2(.2, .04));
    float4 base = tex2D(input, uv);
    float r = tex2D(input, saturate(uv + offset)).r;
    float b = tex2D(input, saturate(uv - offset)).b;
    return float4(r, base.g, b, base.a);
}
