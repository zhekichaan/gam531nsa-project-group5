using FinalProject.Backends;
using FinalProject.Common;
using FinalProject.Helpers;
using ImGuiNET;
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

        private float _cameraSpeed = 2f; // walking speed
        private readonly float _sensitivity = 0.2f; // mouse look sensitivity

        private bool _firstMove = true;
        private Vector2 _lastPos;

        private FlashlightObject _flashlightModel;
        List<WorldObject> _worldObjects;

        private Vector3 _carPosition = new Vector3(0, 0, 10);
        private float _carTriggerRadius = 4.0f; // how close you must be

        private bool _nearCar = false;
        private bool _eKeyPressed = false; // debounce for E

        // Flashlight system
        private bool _flashlightEnabled = false;
        private bool _fKeyPressed = false; // debounce toggle

        // Game state
        private bool _gameStarted;
        private bool _gameEnded;

        // Battery system
        private float _batteryPercentage;
        private readonly float _batteryDrainPerSecond = 5f / 60f; // drains 5% per minute

        public Game()
            : base(GameWindowSettings.Default, new NativeWindowSettings())
        {
            this.Size = new Vector2i(1920, 1080);
            this.WindowState = WindowState.Fullscreen; // always fullscreen
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.DepthTest); // 3D depth
            GL.Enable(EnableCap.Blend); // transparency
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Load shader and camera
            _shader = new Shader("Assets/Shaders/shader.vert", "Assets/Shaders/shader.frag");
            _camera = new Camera(Vector3.UnitY * 1.6f, Size.X / (float)Size.Y); // player height view

            // Load world objects
            Mesh ground = new Mesh("Assets/Models/terrain.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/dirt.png"), _camera);
            Mesh sampleTree = new Mesh("Assets/Models/tree12.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/tree12.png"), _camera);
            Mesh car = new Mesh("Assets/Models/car.fbx", _shader, Texture.LoadFromFile("Assets/Textures/car.png"),
                _camera);
            Mesh monster = new Mesh("Assets/Models/monster.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/monster.png"), _camera);
            Mesh flashlightMesh = new Mesh("Assets/Models/flashlight.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/flashlight.png"), _camera);

            _flashlightModel = new FlashlightObject(flashlightMesh);

            _worldObjects = new List<WorldObject>();
            _worldObjects.Add(new WorldObject(car, _carPosition, new Vector3(0.7f), 0, true)); // Escape target
            _worldObjects.Add(new WorldObject(monster, new Vector3(5, 1.35f, 10), new Vector3(5f), 0, false)); // Enemy
            _worldObjects.Add(new WorldObject(ground, new Vector3(0, 0, 0), new Vector3(2f), 0)); // floor
            _worldObjects.Add(new WorldObject(sampleTree, new Vector3(2, -0.8f, -5), new Vector3(0.008f), 0, true,
                new Vector3(0.5f, 10f, 0.5f))); // tree w/ collision

            // UI setup
            ImGui.CreateContext();
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            ImGui.StyleColorsDark();
            ImguiImplOpenTK4.Init(this);
            ImguiImplOpenGL3.Init();

            _batteryPercentage = 100f; // start full
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            // Begin new UI frame
            ImguiImplOpenGL3.NewFrame();
            ImguiImplOpenTK4.NewFrame();
            ImGui.NewFrame();

            // Draw correct UI depending on state
            if (!_gameStarted)
                BuildMainMenuUI();
            else if (_gameEnded)
                BuildEndScreenUI();
            else
                BuildInGameUI();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Update flashlight data to shader
            _shader.Use();
            _shader.SetVector3("flashlight.position", _camera.Position);
            _shader.SetVector3("flashlight.direction", _camera.Front);
            _shader.SetInt("flashlight.enabled", _flashlightEnabled ? 1 : 0);

            // Draw world
            foreach (var obj in _worldObjects)
                obj.Draw();

            // Draw flashlight mesh in view
            if (_flashlightEnabled)
                _flashlightModel.Draw();

            // Render ImGui HUD
            ImGui.Render();
            GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
            ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!IsFocused) return;
            if (!_gameStarted || _gameEnded) return;

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
                Close(); // quit

            // Flashlight toggle + battery check
            if (input.IsKeyDown(Keys.F))
            {
                if (!_fKeyPressed)
                {
                    if (!_flashlightEnabled && _batteryPercentage > 0f)
                        _flashlightEnabled = true;
                    else
                        _flashlightEnabled = false;

                    _fKeyPressed = true;
                }
            }
            else _fKeyPressed = false;

            // Battery drains while light is ON
            if (_flashlightEnabled && _batteryPercentage > 0f)
            {
                _batteryPercentage -= _batteryDrainPerSecond * (float)e.Time * 100f;
                if (_batteryPercentage <= 0f)
                {
                    _batteryPercentage = 0f;
                    _flashlightEnabled = false; // auto shutoff
                }
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


            // Mouse camera look
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

            float distanceToCar = Vector3.Distance(_camera.Position, _carPosition);
            _nearCar = distanceToCar <= _carTriggerRadius;

            // If near and E pressed, enter car -> show end screen
            if (_nearCar)
            {
                if (input.IsKeyDown(Keys.E))
                {
                    if (!_eKeyPressed)
                    {
                        _gameEnded = true;
                        CursorState = CursorState.Normal; // unlock cursor for end menu
                        _eKeyPressed = true;
                    }
                }
                else
                {
                    _eKeyPressed = false;
                }
            }

            // Attach flashlight model to camera
            Vector3 flashlightOffset = new Vector3(0.4f, -0.3f, 0.5f);
            _flashlightModel.UpdateFromCamera(_camera, flashlightOffset);
        }

        private bool CheckPlayerCollision(Vector3 position)
        {
            // Player collision box check against world objects
            BoundingBox playerBox = BoundingBox.FromCenterAndSize(position, new Vector3(0.6f, 1.8f, 0.6f), Vector3.One);
            foreach (var obj in _worldObjects)
                if (obj.CheckCollision(playerBox))
                    return true;
            return false;
        }

        private void BuildMainMenuUI()
        {
            CursorState = CursorState.Normal;
            
            // Centered title + START + EXIT
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X * 0.5f, Size.Y * 0.5f), ImGuiCond.Always,
                new System.Numerics.Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowBgAlpha(0.0f);
            var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.AlwaysAutoResize;
            ImGui.Begin("MainMenu_Frameless", flags);

            ImGui.SetWindowFontScale(2.2f);
            ImGuiHelpers.TextCentered("Final Project");
            ImGui.SetWindowFontScale(1.0f);

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.SetWindowFontScale(1.5f);
            if (ImGui.Button("Start", new System.Numerics.Vector2(340, 80)))
            {
                _gameStarted = true;
                CursorState = CursorState.Grabbed; // lock cursor for FPS controls
            }

            ImGui.Spacing();
            if (ImGui.Button("Exit", new System.Numerics.Vector2(340, 80)))
                Close();
            ImGui.SetWindowFontScale(1.0f);

            ImGui.End();
        }

        private static class ImGuiHelpers
        {
            public static void TextCentered(string text)
            {
                var windowWidth = ImGui.GetWindowWidth();
                var textWidth = ImGui.CalcTextSize(text).X;
                ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                ImGui.Text(text);
            }
        }

        private void BuildInGameUI()
        {
            CursorState = CursorState.Normal;
            
            // Battery HUD top-left corner
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(12, 12), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.0f);
            var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.AlwaysAutoResize;

            ImGui.Begin("BatteryHUD_Frameless", flags);

            ImGui.SetWindowFontScale(1.6f);
            ImGui.Text($"Battery left: {(int)MathF.Max(0, _batteryPercentage)}%%"); // HUD text
            ImGui.SetWindowFontScale(1.0f);

            // Big center prompt when close to the car
            if (_nearCar && !_gameEnded)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X * 0.5f, Size.Y * 0.75f), ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowBgAlpha(0.0f);
                flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize;
                ImGui.Begin("EnterPrompt_Frameless", flags);
                ImGui.SetWindowFontScale(1.8f);
                ImGui.Text("Press E to enter car");
                ImGui.SetWindowFontScale(1.0f);
                ImGui.End();
            }
            
            ImGui.End();
        }

        private void BuildEndScreenUI()
        {
            // Final message + Main Menu/Exit
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X * 0.5f, Size.Y * 0.5f), ImGuiCond.Always,
                new System.Numerics.Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowBgAlpha(0.0f);
            var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.AlwaysAutoResize;
            ImGui.Begin("End_Frameless", flags);

            ImGui.SetWindowFontScale(2.6f);
            ImGui.TextWrapped("Good job, you escaped");
            ImGui.SetWindowFontScale(1.0f);

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.SetWindowFontScale(1.4f);
            if (ImGui.Button("Main Menu", new System.Numerics.Vector2(220, 60)))
            {
                // Reset game state
                _gameEnded = false;
                _gameStarted = false;
                CursorState = CursorState.Normal;
                _batteryPercentage = 100f;
                _camera.Position = new Vector3(0, 1.6f, 0);
            }

            ImGui.SameLine();
            if (ImGui.Button("Exit", new System.Numerics.Vector2(120, 60)))
                Close();
            ImGui.SetWindowFontScale(1.0f);

            ImGui.End();
        }
    }
}
