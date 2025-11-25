#version 330 core
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
    vec3 ambient = light.ambient * texColor.rgb;

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
            float flashAttenuation = 1.0 / (1.0 + 0.3 * flashDistance + 0.15 * flashDistance * flashDistance);
            
            // Smooth edge (soft spotlight effect)
            float epsilon = cutOff - outerCutOff;
            float intensity = clamp((theta - outerCutOff) / epsilon, 0.0, 1.0);
            
            flashlightContribution = vec3(1.2, 1.2, 1.1) * flashDiff * flashAttenuation * intensity * texColor.rgb * 1.5;
        }
    }

    vec3 result = ambient + diffuse + flashlightContribution;

    FragColor = vec4(result, texColor.a);
}