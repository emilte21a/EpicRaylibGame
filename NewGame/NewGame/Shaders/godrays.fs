#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D scene;

// NEW
uniform vec2 sunPos;   // screen-space sun position
uniform vec2 dir;      // normalized direction vector

uniform float density;
uniform float weight;
uniform int samples;

void main()
{
    vec2 uv = fragTexCoord;

    // direction is already normalized and passed from C#
    vec2 delta = dir * density;
    
    float w = weight / float(samples);

    vec4 sum = vec4(0.0);

    for (int i = 0; i < samples; i++)
    {
        uv -= delta;
        sum += texture(scene, uv) * w;
    }

    finalColor = sum;
}
