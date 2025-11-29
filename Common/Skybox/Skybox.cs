using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace FinalProject.Common
{
    public class Skybox : IDisposable
    {
        private readonly int _vao;
        private readonly int _vbo;
        private readonly int _texture;
        private readonly Shader _shader;

        public Skybox(string texturePath, string vertexShaderPath, string fragmentShaderPath)
        {
            // Skybox cube vertices
            float[] skyVertices = {
                -1f,  1f, -1f,
                -1f, -1f, -1f,
                 1f, -1f, -1f,
                 1f, -1f, -1f,
                 1f,  1f, -1f,
                -1f,  1f, -1f,

                -1f, -1f,  1f,
                -1f, -1f, -1f,
                -1f,  1f, -1f,
                -1f,  1f, -1f,
                -1f,  1f,  1f,
                -1f, -1f,  1f,

                 1f, -1f, -1f,
                 1f, -1f,  1f,
                 1f,  1f,  1f,
                 1f,  1f,  1f,
                 1f,  1f, -1f,
                 1f, -1f, -1f,

                -1f, -1f,  1f,
                -1f,  1f,  1f,
                 1f,  1f,  1f,
                 1f,  1f,  1f,
                 1f, -1f,  1f,
                -1f, -1f,  1f,

                -1f,  1f, -1f,
                 1f,  1f, -1f,
                 1f,  1f,  1f,
                 1f,  1f,  1f,
                -1f,  1f,  1f,
                -1f,  1f, -1f,

                -1f, -1f, -1f,
                -1f, -1f,  1f,
                 1f, -1f, -1f,
                 1f, -1f, -1f,
                -1f, -1f,  1f,
                 1f, -1f,  1f
            };

            // Setup VAO and VBO
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, skyVertices.Length * sizeof(float), skyVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            // Load texture
            _texture = TextureLoader.Load(texturePath);

            // Load shaders
            _shader = new Shader(vertexShaderPath, fragmentShaderPath);
            _shader.Use();
            _shader.SetInt("skyTexture", 0);
        }

        public void Draw(Matrix4 view, Matrix4 projection)
        {
            // Disable depth writing for skybox
            GL.DepthMask(false);
            GL.DepthFunc(DepthFunction.Lequal);

            _shader.Use();

            // Extract the 3x3 rotation matrix and rebuild as 4x4
            Matrix3 rotationOnly = new Matrix3(view);
            Matrix4 viewNoTranslation = new Matrix4(rotationOnly);

            _shader.SetMatrix4("view", viewNoTranslation);
            _shader.SetMatrix4("projection", projection);

            GL.BindVertexArray(_vao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            // Restore normal depth settings
            GL.DepthFunc(DepthFunction.Less);
            GL.DepthMask(true);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteTexture(_texture);
        }
    }

    public static class TextureLoader
    {
        public static int Load(string path)
        {
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            // Using StbImageSharp 
            StbImageSharp.StbImage.stbi_set_flip_vertically_on_load(1);

            using (System.IO.Stream stream = System.IO.File.OpenRead(path))
            {
                StbImageSharp.ImageResult image = StbImageSharp.ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                    image.Width, image.Height, 0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte, image.Data);
            }

            // Use Linear filtering for smoother appearance
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // Enable anisotropic filtering
            float maxAniso;
            GL.GetFloat((GetPName)0x84FF, out maxAniso);
            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)0x84FE, Math.Min(4.0f, maxAniso));

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            return tex;
        }
    }
}