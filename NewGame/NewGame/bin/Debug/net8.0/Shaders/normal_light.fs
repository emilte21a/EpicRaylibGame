#version 330

uniform sampler2D texture0;     // diffuse tile texture
uniform sampler2D normalMap;    // corresponding normal map
uniform vec2 lightPos;          // screen space position of the light
uniform vec3 lightColor;        // RGB color of the light
uniform float lightIntensity;   // light strength

in vec2 fragTexCoord;
out vec4 fragColor;

void main()
{
    // Sample textures
    vec3 diffuse = texture(texture0, fragTexCoord).rgb;
    vec3 normal = texture(normalMap, fragTexCoord).rgb;
    
    // Convert from [0,1] to [-1,1]
    normal = normalize(normal * 2.0 - 1.0);
    
    // Light vector
    vec2 fragPos = gl_FragCoord.xy;
    vec3 lightDir = normalize(vec3(lightPos - fragPos, 100.0)); // Z distance
    
    // Lambertian diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    
    vec3 result = diffuse * lightColor * diff * lightIntensity;
    fragColor = vec4(result, 1.0);
}