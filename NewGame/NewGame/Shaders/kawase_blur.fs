#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D texture0;
uniform vec2 resolution;
uniform float offset;

void main()
{
    vec2 uv = vec2(fragTexCoord.x, fragTexCoord.y);

    vec2 texel = 1.0 / resolution;

    vec4 sum = vec4(0.0);

    sum += texture(texture0, uv + texel * vec2( offset,  offset));
    sum += texture(texture0, uv + texel * vec2(-offset,  offset));
    sum += texture(texture0, uv + texel * vec2( offset, -offset));
    sum += texture(texture0, uv + texel * vec2(-offset, -offset));

    finalColor = sum * 0.25;
}

