#ifndef ARENA_LIGHTING_INCLUDED
#define ARENA_LIGHTING_INCLUDED

// ArenaLighting 스크립트가 매 프레임 채우는 전역 값이다. 판 그림은 면 방향이 의미 없어서 방향과 무관하게 밝기만 더한다
#define ARENA_MAX_LIGHTS 8

float4 _ArenaLightPos[ARENA_MAX_LIGHTS];   // xyz 위치, w 사거리
float4 _ArenaLightColor[ARENA_MAX_LIGHTS]; // rgb 색×세기
float4 _ArenaBase;                         // rgb 앰비언트+햇빛, w 1이면 조명 켜짐
float4 _ArenaParams;                       // x 점광 개수, y 밝기 단계 수(0이면 부드럽게)

half3 ArenaLight(float3 positionWS)
{
    // 스크립트가 없는 씬에서도 까맣게 나오지 않도록 꺼져 있으면 원래 밝기를 쓴다
    if (_ArenaBase.w < 0.5)
    {
        return half3(1, 1, 1);
    }

    half3 light = _ArenaBase.rgb;
    int count = (int)_ArenaParams.x;

    [unroll]
    for (int i = 0; i < ARENA_MAX_LIGHTS; i++)
    {
        if (i < count)
        {
            float3 toLight = _ArenaLightPos[i].xyz - positionWS;
            float range = max(_ArenaLightPos[i].w, 0.001);
            float falloff = saturate(1.0 - dot(toLight, toLight) / (range * range));
            light += _ArenaLightColor[i].rgb * (falloff * falloff);
        }
    }

    // 도트 그림은 밝기가 몇 단계로 끊겨야 어울려서, 색은 그대로 두고 밝기만 계단으로 만든다
    float steps = _ArenaParams.y;
    if (steps > 0.5)
    {
        float lum = max(dot(light, half3(0.299, 0.587, 0.114)), 0.0001);
        float quantized = floor(lum * steps + 0.5) / steps;
        light *= quantized / lum;
    }

    return light;
}

#endif
