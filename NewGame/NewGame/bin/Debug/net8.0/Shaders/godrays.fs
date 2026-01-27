#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D scene;


uniform vec2 sunPos;   
uniform vec2 dir;      

uniform float density;
uniform float weight;
uniform int samples;

void main()
{
    vec2 uv = fragTexCoord;

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
