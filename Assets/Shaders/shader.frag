#version 330 core
struct Material {
    sampler2D diffuse;
};

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
};

uniform Light light;
uniform Material material;

out vec4 FragColor;

in vec3 Normal;
in vec3 FragPos;
in vec2 TexCoords;

void main()
{
    vec4 texColor = texture(material.diffuse, TexCoords);

    // for leaf tuxtures
    if (texColor.a < 0.1)
    discard;

    // Ambient
    vec3 ambient = light.ambient * texColor.rgb;

    // Diffuse
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(light.position - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);

    float distance = length(light.position - FragPos);
    float attenuation = 1.0 / (distance * distance);

    vec3 diffuse = light.diffuse * diff * attenuation * vec3(texture(material.diffuse, TexCoords));

    vec3 result = ambient + diffuse;

    FragColor = vec4(result, texColor.a);
}