#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D sceneTex;      // the bright/lighting scene (RGB)
uniform sampler2D occlusionTex;  // occluders: white = blocks, black = empty
uniform vec2 lightPos;           // light position in screen space [0..1]
uniform float exposure;          // overall exposure / intensity
uniform float decay;             // decay per sample (0..1)
uniform float density;           // density (controls step length)
uniform float weight;            // weight before normalization
uniform int samples;             // number of samples

void main()
{
    // uv of current pixel
    vec2 uv = fragTexCoord;

    // vector from pixel TO light origin (we move toward the light)
    vec2 delta = lightPos - uv;

    // step per sample (scaled by density) — march toward the light
    vec2 step = delta * (density / float(samples));

    vec3 color = vec3(0.0);
    float illuminationDecay = 1.0;

    // Normalized weight to avoid blowout: ensures sum scales with `weight`
    float w = weight / float(samples);

    vec2 sampleUV = uv;
    for (int i = 0; i < samples; i++)
    {
        sampleUV += step;

        // sample occlusion (use red channel — occlusionTex should be greyscale)
        float occ = texture(occlusionTex, sampleUV).r; // 0 = transparent (light passes), 1 = blocked

        // transmission is how much light passes at this sample
        float transmission = 1.0 - occ;

        // sample the scene brightness/color
        vec3 sampleCol = texture(sceneTex, sampleUV).rgb;

        // accumulate, modulated by transmission and decaying illumination
        color += sampleCol * transmission * illuminationDecay * w;

        // decay illumination each step so farther samples contribute less
        illuminationDecay *= decay;
    }

    // exposure to control final intensity
    color *= exposure;

    finalColor = vec4(color, 1.0);
}
