#version 330 core

uniform vec3 cameraPos;
uniform float fogDensity;

struct Material {
    sampler2D diffuse;
};

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
};

struct Flashlight {
    vec3 position;
    vec3 direction;
    int enabled;
};

uniform Light light;
uniform Material material;
uniform Flashlight flashlight;

out vec4 FragColor;

in vec3 Normal;
in vec3 FragPos;
in vec2 TexCoords;

void main()
{
    vec4 texColor = texture(material.diffuse, TexCoords);

    // for leaf textures
    if (texColor.a < 0.1)
        discard;

    // Ambient
    vec3 ambient = light.ambient * texColor.rgb * 0.15; 

    // Diffuse from main light
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(light.position - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);

    float distance = length(light.position - FragPos);
    float attenuation = 1.0 / (distance * distance);

    vec3 diffuse = light.diffuse * diff * attenuation * vec3(texture(material.diffuse, TexCoords));

    // Flashlight calculation
    vec3 flashlightContribution = vec3(0.0);
    if (flashlight.enabled == 1) {
        vec3 flashlightDir = normalize(flashlight.position - FragPos);
        
        // Calculate spotlight effect
        float theta = dot(flashlightDir, normalize(-flashlight.direction));
        float cutOff = cos(radians(25.0)); // Inner cone angle (wider)
        float outerCutOff = cos(radians(32.0)); // Outer cone angle (wider)
        
        if (theta > outerCutOff) {
            // Diffuse
            float flashDiff = max(dot(norm, flashlightDir), 0.0);
            
            // Attenuation (stronger falloff for shorter distance)
            float flashDistance = length(flashlight.position - FragPos);
            float flashAttenuation = 1.0 / (1.0 + 0.09 * flashDistance + 0.02 * flashDistance * flashDistance);
            
            // Smooth edge (soft spotlight effect)
            float epsilon = cutOff - outerCutOff;
            float intensity = clamp((theta - outerCutOff) / epsilon, 0.0, 1.0);
            
            flashlightContribution = vec3(2.5, 2.5, 2.3) 
                        * flashDiff 
                        * flashAttenuation 
                        * intensity 
                        * texColor.rgb 
                        * 1;
        }
    }

    vec3 result = ambient + diffuse + flashlightContribution;

    // Fog
    // Distance from camera
    float dist = length(FragPos - cameraPos);

    // Fog factor (exp2)
    float fogFactor = exp(-pow(dist * fogDensity, 2.0));
    fogFactor = clamp(fogFactor, 0.0, 1.0);

    // Fog color (near-black)
    vec3 fogColor = vec3(0.03, 0.03, 0.03);

    // Let flashlight reduce fog when shining at object
    if (flashlight.enabled == 1) {
        vec3 toPixel = normalize(FragPos - flashlight.position);
        float beam = dot(toPixel, normalize(-flashlight.direction));

        if (beam > 0.7) {
            fogFactor = clamp(fogFactor + beam * 1.2, 0.0, 1.0);
        }
    }

    // Combine fog with scene
    vec3 finalColor = mix(fogColor, result, fogFactor);

    FragColor = vec4(finalColor, texColor.a);
}