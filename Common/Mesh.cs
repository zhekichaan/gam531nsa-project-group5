using Assimp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace FinalProject.Common;

public class Mesh
{
    private readonly float[] _vertices;
    private readonly int _verticesLength;
    
    private readonly int _vao;
    private readonly int _vbo;
    
    private readonly Texture _texture;
    private readonly Shader _shader;
    private readonly Camera _camera;

    // Mesh position
    public Matrix4 Transform = Matrix4.Identity;

    // Original bounds of the mesh (before any transformation)
    public Vector3 OriginalMin { get; private set; }
    public Vector3 OriginalMax { get; private set; }
    
    /// <param name="fbxPath">The path to the fbx model</param>
    /// <param name="shader">The shader used by that Mesh</param>
    /// <param name="texture">The texture of that Mesh</param>
    /// <param name="camera">The camera used in Draw() for projection and view</param>
    public Mesh(string fbxPath, Shader shader, Texture texture, Camera camera)
    {
        _shader = shader;
        _texture = texture;
        _camera = camera;

        // Loading vertices from .fbx object
        _vertices = LoadFbx(fbxPath);
        _verticesLength = _vertices.Length;

        // Calculate bounds from vertices for collision
        CalculateBounds();

        // Generate VBO
        {
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, this._vertices.Length * sizeof(float), this._vertices,
                BufferUsageHint.StaticDraw); 
        }
        
        // Generate VAO
        {
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            var positionLocation = _shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            var normalLocation = _shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float),
                3 * sizeof(float));

            var texCoordLocation = _shader.GetAttribLocation("aTexCoords");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float),
                6 * sizeof(float));
        }
    }

    /// <param name="path">The path to the fbx model</param>
    private float[] LoadFbx(string path)
    {
        var importer = new AssimpContext();
        var scene = importer.ImportFile(path,
            PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.GenerateNormals);

        if (scene.MeshCount == 0)
            throw new Exception("FBX contains no meshes!");

        var mesh = scene.Meshes[0];
        float[] vertices = new float[mesh.VertexCount * 8];

        for (int i = 0; i < mesh.VertexCount; i++)
        {
            var v = mesh.Vertices[i];
            var n = mesh.Normals[i];
            var t = mesh.TextureCoordinateChannels[0].Count > 0
                ? mesh.TextureCoordinateChannels[0][i]
                : new Vector3D(0, 0, 0);

            vertices[i * 8 + 0] = v.X;
            vertices[i * 8 + 1] = v.Y;
            vertices[i * 8 + 2] = v.Z;

            vertices[i * 8 + 3] = n.X;
            vertices[i * 8 + 4] = n.Y;
            vertices[i * 8 + 5] = n.Z;

            vertices[i * 8 + 6] = t.X;
            vertices[i * 8 + 7] = 1 - t.Y;
        }

        return vertices;
    }

    private void CalculateBounds()
    {
        if (_vertices.Length == 0) return;

        Vector3 min = new Vector3(float.MaxValue);
        Vector3 max = new Vector3(float.MinValue);

        // Iterate through vertices (stride of 8 floats per vertex)
        for (int i = 0; i < _vertices.Length; i += 8)
        {
            Vector3 pos = new Vector3(_vertices[i], _vertices[i + 1], _vertices[i + 2]);
            min = Vector3.ComponentMin(min, pos);
            max = Vector3.ComponentMax(max, pos);
        }

        OriginalMin = min;
        OriginalMax = max;
    }

    public void Draw()
    {
        GL.BindVertexArray(_vao);

        _texture.Use(TextureUnit.Texture0);

        _shader.Use();

        _shader.SetMatrix4("model", Transform);
        _shader.SetMatrix4("view", _camera.GetViewMatrix());
        _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());

        _shader.SetInt("material.diffuse", 0);

        _shader.SetVector3("light.position", new Vector3(-2f, 2.5f, -8f));
        _shader.SetVector3("light.ambient", new Vector3(0.2f));
        _shader.SetVector3("light.diffuse", Vector3.Zero);

        GL.DrawArrays(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, 0, _verticesLength / 8);
    }
}