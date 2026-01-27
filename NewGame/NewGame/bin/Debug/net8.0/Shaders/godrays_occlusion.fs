#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D sceneTex;      
uniform sampler2D occlusionTex;  
uniform vec2 lightPos;           
uniform float exposure;         
uniform float decay;            
uniform float density;         
uniform float weight;           
uniform int samples;             

void main()
{
  
    vec2 uv = fragTexCoord;

   
    vec2 delta = lightPos - uv;

   
    vec2 step = delta * (density / float(samples));

    vec3 color = vec3(0.0);
    float illuminationDecay = 1.0;

  
    float w = weight / float(samples);

    vec2 sampleUV = uv;
    for (int i = 0; i < samples; i++)
    {
        sampleUV += step;

       
        float occ = texture(occlusionTex, sampleUV).r;

        
        float transmission = 1.0 - occ;

      
        vec3 sampleCol = texture(sceneTex, sampleUV).rgb;

       
        color += sampleCol * transmission * illuminationDecay * w;

      
        illuminationDecay *= decay;
    }

    
    color *= exposure;

    finalColor = vec4(color, 1.0);
}
