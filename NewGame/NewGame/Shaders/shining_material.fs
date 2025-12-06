#version 330

// Inputs from raylib
in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform sampler2D texture0;   // sprite texture
uniform float time;           // passed from C#
uniform vec2 resolution;      // screen or sprite size
uniform float shineSpeed;     // e.g. 1.5
uniform float shineWidth;     // e.g. 0.25

void main() {
    vec4 texColor = texture(texture0, fragTexCoord);

    // ---------------------------------
    // 1. Basic texture
    // ---------------------------------
    vec3 color = texColor.rgb;

    // ---------------------------------
    // 2. Fresnel-like rim (cheap version)
    // ---------------------------------
    // distance from center for 2D sprites
    vec2 uv = fragTexCoord - 0.5;
    float dist = length(uv) * 2.0;
    float rim = smoothstep(0.7, 1.0, dist);
    color += rim * 0.25;  // subtle rim highlight

    // ---------------------------------
    // 3. Moving shine sweep
    // ---------------------------------
    float sweep = (fragTexCoord.x + fragTexCoord.y) * 0.7;  
    float anim = sin((sweep * (1.0 / shineWidth)) + time * shineSpeed);
    float shine = smoothstep(0.6, 0.9, anim);  // thin bright line
    color += shine * 0.6;

    finalColor = vec4(color, texColor.a);
}
