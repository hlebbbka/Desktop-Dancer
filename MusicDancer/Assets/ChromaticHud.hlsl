sampler2D input : register(s0);
float2 shift : register(c0);
float4 main(float2 uv : TEXCOORD) : COLOR {
 float4 red=tex2D(input,uv+shift),center=tex2D(input,uv),blue=tex2D(input,uv-shift);
 return float4(red.r,center.g,blue.b,max(red.a,max(center.a,blue.a)));
}
