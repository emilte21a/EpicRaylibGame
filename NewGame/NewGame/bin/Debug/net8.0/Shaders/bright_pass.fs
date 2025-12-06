#version 330

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D texture0;
uniform float threshold;   // brightness threshold 0–1

void main() {
    vec3 col = texture(texture0, fragTexCoord).rgb;
    float brightness = max(max(col.r, col.g), col.b);

    if (brightness > threshold)
        fragColor = vec4(1.0, 1.0, 1.0, 1.0);
    else
        fragColor = vec4(0.0, 0.0, 0.0, 1.0);
}