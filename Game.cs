using FinalProject.Common;
using FinalProject.Helpers;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FinalProject
{
    public class Game : GameWindow
    {
        private Shader _shader;
        private Camera _camera;

        private float _cameraSpeed = 2f;
        private readonly float _sensitivity = 0.2f;

        private bool _firstMove = true;
        private Vector2 _lastPos;
        
        private FlashlightObject _flashlightModel;

        List<WorldObject> _worldObjects;

        // Flashlight variables
        private bool _flashlightEnabled = false;
        private bool _fKeyPressed = false;

        public Game()
            : base(GameWindowSettings.Default, new NativeWindowSettings())
        {
            this.Size = new Vector2i(1920, 1080);

            // Open game in full screen mode
            this.WindowState = WindowState.Fullscreen;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // Hiding cursor from the screen
            CursorState = CursorState.Grabbed;

            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // getting shader from Assets/Shaders/*
            _shader = new Shader("Assets/Shaders/shader.vert", "Assets/Shaders/shader.frag");

            // Setting up our camera height (players height)
            _camera = new Camera(Vector3.UnitY * 1.6f, Size.X / (float)Size.Y);

            // Ground Mesh
            Mesh ground = new Mesh("Assets/Models/terrain.fbx", _shader, Texture.LoadFromFile("Assets/Textures/dirt.png"), _camera);

            Mesh sampleTree = new Mesh("Assets/Models/tree12.fbx", _shader, Texture.LoadFromFile("Assets/Textures/tree12.png"), _camera);

            Mesh car = new Mesh("Assets/Models/car.fbx", _shader, Texture.LoadFromFile("Assets/Textures/car.png"), _camera);
            
            Mesh monster = new Mesh("Assets/Models/monster.fbx", _shader, Texture.LoadFromFile("Assets/Textures/monster.png"), _camera);
            
            
            Mesh flashlightMesh = new Mesh(
                "Assets/Models/flashlight.fbx", 
                _shader, 
                Texture.LoadFromFile("Assets/Textures/flashlight.png"), 
                _camera
            );
            
            _flashlightModel = new FlashlightObject(flashlightMesh);
            
            _worldObjects = new List<WorldObject>();
            
            _worldObjects.Add(new WorldObject(car, new Vector3(0, 0, 10), new Vector3(0.7f), 0, true));
            
            _worldObjects.Add(new WorldObject(monster, new Vector3(5, 1.35f, 10), new Vector3(5f), 0, false));
            
            // Adding ground to our world objects
            _worldObjects.Add(new WorldObject(ground, new Vector3(0, 0, 0), new Vector3(2f), 0));

            // Adding sample tree
            _worldObjects.Add(new WorldObject(sampleTree, new Vector3(2, -0.8f, -5), new Vector3(0.008f), 0, true, new Vector3(0.5f, 10f, 0.5f)));

            GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Update shader with flashlight data
            _shader.Use();
            _shader.SetVector3("flashlight.position", _camera.Position);
            _shader.SetVector3("flashlight.direction", _camera.Front);
            _shader.SetInt("flashlight.enabled", _flashlightEnabled ? 1 : 0);

            foreach (var obj in _worldObjects)
            {
                obj.Draw();
            }
            
            if (_flashlightEnabled)
            {
                _flashlightModel.Draw();
            }

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!IsFocused)
            {
                return;
            }

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            // Flashlight toggle with F key
            if (input.IsKeyDown(Keys.F))
            {
                if (!_fKeyPressed)
                {
                    _flashlightEnabled = !_flashlightEnabled;
                    _fKeyPressed = true;
                }
            }
            else
            {
                _fKeyPressed = false;
            }

            float yaw = MathHelper.DegreesToRadians(_camera.Yaw);

            Vector3 forward = new Vector3(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
            forward = Vector3.Normalize(forward);

            if (input.IsKeyDown(Keys.LeftShift))
            {
                _cameraSpeed = 4f;
            }

            Vector3 oldPosition = _camera.Position;
            Vector3 newPosition = oldPosition;

            // Movement with collision detection
            if (input.IsKeyDown(Keys.W))
            {
                newPosition += forward * _cameraSpeed * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.S))
            {
                newPosition -= forward * _cameraSpeed * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.A))
            {
                newPosition -= _camera.Right * _cameraSpeed * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.D))
            {
                newPosition += _camera.Right * _cameraSpeed * (float)e.Time;
            }

            // Check collision before applying movement
            if (!CheckPlayerCollision(newPosition))
            {
                _camera.Position = newPosition;
            }
            else
            {
                Vector3 tryX = new Vector3(newPosition.X, oldPosition.Y, oldPosition.Z);
                Vector3 tryZ = new Vector3(oldPosition.X, oldPosition.Y, newPosition.Z);

                if (!CheckPlayerCollision(tryX))
                {
                    _camera.Position = tryX;
                }
                else if (!CheckPlayerCollision(tryZ))
                {
                    _camera.Position = tryZ;
                }
                // If both collide, stay at old position
            }


            var mouse = MouseState;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                _camera.Yaw += deltaX * _sensitivity;
                _camera.Pitch -= deltaY * _sensitivity;
            }
            
            Vector3 flashlightOffset = new Vector3(0.4f, -0.3f, 0.5f);
            _flashlightModel.UpdateFromCamera(_camera, flashlightOffset);
        }

        private bool CheckPlayerCollision(Vector3 position)
        {
            // Create player bounding box at the new position
            BoundingBox playerBox = BoundingBox.FromCenterAndSize(position, new Vector3(0.6f, 1.8f, 0.6f), Vector3.One);

            // Check against all world objects
            foreach (var obj in _worldObjects)
            {
                if (obj.CheckCollision(playerBox))
                {
                    return true; // Collision detected
                }
            }

            return false; // No collision
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
            base.OnResize(e);
        }
    }
}