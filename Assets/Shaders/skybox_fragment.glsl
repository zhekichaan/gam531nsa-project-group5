#version 330 core
out vec4 FragColor;
in vec3 TexCoords;

uniform sampler2D skyTexture;

void main()
{
    // Convert direction to 2D spherical UV mapping
    vec3 d = normalize(TexCoords);
    float u = atan(d.z, d.x) / (2.0 * 3.14159265359) + 0.5;
    float v = asin(clamp(d.y, -1.0, 1.0)) / 3.14159265359 + 0.5;
    
    vec4 skyColor = texture(skyTexture, vec2(u, v));
    
    FragColor = vec4(skyColor.rgb * 0.4, skyColor.a);
}