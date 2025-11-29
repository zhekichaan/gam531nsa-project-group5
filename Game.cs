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
        private Skybox _skybox;

        private float _cameraSpeed = 2f;
        private readonly float _sensitivity = 0.2f;

        private bool _firstMove = true;
        private Vector2 _lastPos;

        private FlashlightObject _flashlightModel;
        List<WorldObject> _worldObjects;

        private Vector3 _carPosition = new Vector3(0, 0, 10);
        private float _carTriggerRadius = 4.0f;

        private bool _nearCar = false;
        private bool _eKeyPressed = false;

        // Monster AI
        private WorldObject _monsterObject;
        private MonsterAI _monsterAI;

        // Flashlight system
        private bool _flashlightEnabled = false;
        private bool _fKeyPressed = false;

        // Game state
        private bool _gameStarted;
        private bool _gameEnded;

        // Battery system
        private float _batteryPercentage;
        private readonly float _batteryDrainPerSecond = 5f / 60f;

        // Collectible batteries
        private List<CollectibleBattery> _collectibleBatteries;
        private CollectibleBattery _nearbyBattery = null;

        public Game()
            : base(GameWindowSettings.Default, new NativeWindowSettings())
        {
            this.Size = new Vector2i(1920, 1080);
            //this.WindowState = WindowState.Fullscreen;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shader = new Shader("Assets/Shaders/shader.vert", "Assets/Shaders/shader.frag");
            _camera = new Camera(Vector3.UnitY * 1.6f, Size.X / (float)Size.Y);

            _skybox = new Skybox(
                "Assets/Textures/Skybox/sky.png",
                "Assets/Shaders/skybox_vertex.glsl",
                "Assets/Shaders/skybox_fragment.glsl"
            );

            // Load meshes
            Mesh ground = new Mesh("Assets/Models/terrain.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/dirt.png"), _camera);
            Mesh car = new Mesh("Assets/Models/car.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/car.png"), _camera);
            Mesh monster = new Mesh("Assets/Models/monster.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/monster.png"), _camera);
            Mesh flashlightMesh = new Mesh("Assets/Models/flashlight.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/flashlight.png"), _camera);
            Mesh batteryMesh = new Mesh("Assets/Models/battery.fbx", _shader,
                Texture.LoadFromFile("Assets/Textures/battery.png"), _camera);

            // Load trees and bushes (only if files exist)
            List<Mesh> treeMeshes = new List<Mesh>();
            List<Mesh> bushMeshes = new List<Mesh>();

            // Try loading each tree type
            var treeFiles = new[] {
                ("Assets/Models/tree.fbx", "Assets/Textures/tree.png"),
                ("Assets/Models/tree01.fbx", "Assets/Textures/tree01.png"),
                ("Assets/Models/tree02.fbx", "Assets/Textures/tree02.png"),
                ("Assets/Models/tree03.fbx", "Assets/Textures/tree03.png"),
                ("Assets/Models/tree12.fbx", "Assets/Textures/tree12.png")
            };

            foreach (var (model, texture) in treeFiles)
            {
                if (File.Exists(model) && File.Exists(texture))
                {
                    treeMeshes.Add(new Mesh(model, _shader, Texture.LoadFromFile(texture), _camera));
                }
            }

            // Try loading each bush type
            var bushFiles = new[] {
                ("Assets/Models/bush01.fbx", "Assets/Textures/bush01.png"),
                ("Assets/Models/bush02.fbx", "Assets/Textures/bush02.png")
            };

            foreach (var (model, texture) in bushFiles)
            {
                if (File.Exists(model) && File.Exists(texture))
                {
                    bushMeshes.Add(new Mesh(model, _shader, Texture.LoadFromFile(texture), _camera));
                }
            }

            // Fallback: if no trees loaded, use a default
            if (treeMeshes.Count == 0)
            {
                throw new Exception("No tree models found!");
            }

            _flashlightModel = new FlashlightObject(flashlightMesh);
            _worldObjects = new List<WorldObject>();

            // Add ground (no collision)
            _worldObjects.Add(new WorldObject(ground, new Vector3(0, 0, 0), new Vector3(2f), 0, false));

            // Add car
            _worldObjects.Add(new WorldObject(car, _carPosition, new Vector3(0.7f), 0, true));

            // Create monster
            Vector3 monsterSpawn = new Vector3(5, 1.35f, 10);
            _monsterObject = new WorldObject(monster, monsterSpawn, new Vector3(5f), 0, true);
            _monsterAI = new MonsterAI(_monsterObject);
            _worldObjects.Add(_monsterObject);

            // PROCEDURAL WORLD GENERATION
            WorldGenerator worldGen = new WorldGenerator(seed: 12345);

            // Define exclusion zones
            List<Vector3> exclusionZones = new List<Vector3>
            {
                Vector3.Zero,      // Player spawn
                _carPosition,      // Car location
                monsterSpawn       // Monster spawn
            };

            // Generate forest
            worldGen.GenerateForest(
                _worldObjects,
                treeMeshes,
                bushMeshes,
                exclusionZones,
                exclusionRadius: 6.5f,
                worldSize: 60f
            );

            // Generate batteries
            _collectibleBatteries = new List<CollectibleBattery>();
            worldGen.GenerateBatteries(
                _collectibleBatteries,
                batteryMesh,
                count: 50,
                exclusionZones,
                exclusionRadius: 6.5f,
                worldSize: 60f
            );

            // UI setup
            ImGui.CreateContext();
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            ImGui.StyleColorsDark();
            ImguiImplOpenTK4.Init(this);
            ImguiImplOpenGL3.Init();

            _batteryPercentage = 100f;
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            ImguiImplOpenGL3.NewFrame();
            ImguiImplOpenTK4.NewFrame();
            ImGui.NewFrame();

            if (!_gameStarted)
                BuildMainMenuUI();
            else if (_gameEnded)
                BuildEndScreenUI();
            else
                BuildInGameUI();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 projection = _camera.GetProjectionMatrix();
            _skybox.Draw(view, projection);

            _shader.Use();
            _shader.SetVector3("flashlight.position", _camera.Position);
            _shader.SetVector3("flashlight.direction", _camera.Front);
            _shader.SetInt("flashlight.enabled", _flashlightEnabled ? 1 : 0);

            foreach (var obj in _worldObjects)
                obj.Draw();

            foreach (var battery in _collectibleBatteries)
            {
                if (!battery.IsCollected)
                    battery.Draw();
            }

            if (_flashlightEnabled)
                _flashlightModel.Draw();

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
                Close();

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

            if (_flashlightEnabled && _batteryPercentage > 0f)
            {
                _batteryPercentage -= _batteryDrainPerSecond * (float)e.Time * 100f;
                if (_batteryPercentage <= 0f)
                {
                    _batteryPercentage = 0f;
                    _flashlightEnabled = false;
                }
            }

            float yaw = MathHelper.DegreesToRadians(_camera.Yaw);
            Vector3 forward = new Vector3(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
            forward = Vector3.Normalize(forward);

            // Sprint speed
            if (input.IsKeyDown(Keys.LeftShift))
                _cameraSpeed = 5f;
            else
                _cameraSpeed = 3f;

            Vector3 oldPosition = _camera.Position;
            Vector3 newPosition = oldPosition;

            if (input.IsKeyDown(Keys.W))
                newPosition += forward * _cameraSpeed * (float)e.Time;
            if (input.IsKeyDown(Keys.S))
                newPosition -= forward * _cameraSpeed * (float)e.Time;
            if (input.IsKeyDown(Keys.A))
                newPosition -= _camera.Right * _cameraSpeed * (float)e.Time;
            if (input.IsKeyDown(Keys.D))
                newPosition += _camera.Right * _cameraSpeed * (float)e.Time;

            if (!CheckPlayerCollision(newPosition))
            {
                _camera.Position = newPosition;
            }
            else
            {
                Vector3 tryX = new Vector3(newPosition.X, oldPosition.Y, oldPosition.Z);
                Vector3 tryZ = new Vector3(oldPosition.X, oldPosition.Y, newPosition.Z);

                if (!CheckPlayerCollision(tryX))
                    _camera.Position = tryX;
                else if (!CheckPlayerCollision(tryZ))
                    _camera.Position = tryZ;
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

            float distanceToCar = Vector3.Distance(_camera.Position, _carPosition);
            _nearCar = distanceToCar <= _carTriggerRadius;

            _nearbyBattery = null;
            foreach (var battery in _collectibleBatteries)
            {
                battery.Update((float)e.Time);
                if (battery.IsPlayerNearby(_camera.Position))
                {
                    _nearbyBattery = battery;
                    break;
                }
            }

            if (input.IsKeyDown(Keys.E))
            {
                if (!_eKeyPressed)
                {
                    if (_nearbyBattery != null)
                    {
                        _batteryPercentage = MathF.Min(100f, _batteryPercentage + _nearbyBattery.BatteryRechargeAmount);
                        _nearbyBattery.Collect();
                        _nearbyBattery = null;
                    }
                    else if (_nearCar)
                    {
                        _gameEnded = true;
                        CursorState = CursorState.Normal;
                    }
                    _eKeyPressed = true;
                }
            }
            else
                _eKeyPressed = false;

            Vector3 flashlightOffset = new Vector3(0.4f, -0.3f, 0.5f);
            _flashlightModel.UpdateFromCamera(_camera, flashlightOffset);

            _monsterAI.Update((float)e.Time, _camera.Position, _flashlightEnabled);
        }

        private bool CheckPlayerCollision(Vector3 position)
        {
            BoundingBox playerBox = BoundingBox.FromCenterAndSize(position, new Vector3(0.6f, 1.8f, 0.6f), Vector3.One);
            foreach (var obj in _worldObjects)
                if (obj.CheckCollision(playerBox))
                    return true;
            return false;
        }

        private void BuildMainMenuUI()
        {
            CursorState = CursorState.Normal;

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
                CursorState = CursorState.Grabbed;
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
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(12, 12), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.0f);
            var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.AlwaysAutoResize;

            ImGui.Begin("BatteryHUD_Frameless", flags);
            ImGui.SetWindowFontScale(1.6f);
            ImGui.Text($"Battery left: {(int)MathF.Max(0, _batteryPercentage)}%%");
            ImGui.SetWindowFontScale(1.0f);
            ImGui.End();

            if (_nearbyBattery != null)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X * 0.5f, Size.Y * 0.75f), ImGuiCond.Always,
                    new System.Numerics.Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowBgAlpha(0.0f);
                flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.AlwaysAutoResize;
                ImGui.Begin("BatteryPrompt_Frameless", flags);
                ImGui.SetWindowFontScale(1.8f);
                ImGui.Text($"Press E to pick up battery (+{(int)_nearbyBattery.BatteryRechargeAmount}%)");
                ImGui.SetWindowFontScale(1.0f);
                ImGui.End();
            }
            else if (_nearCar && !_gameEnded)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X * 0.5f, Size.Y * 0.75f), ImGuiCond.Always,
                    new System.Numerics.Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowBgAlpha(0.0f);
                flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.AlwaysAutoResize;
                ImGui.Begin("EnterPrompt_Frameless", flags);
                ImGui.SetWindowFontScale(1.8f);
                ImGui.Text("Press E to enter car");
                ImGui.SetWindowFontScale(1.0f);
                ImGui.End();
            }
        }

        private void BuildEndScreenUI()
        {
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
                _gameEnded = false;
                _gameStarted = false;
                CursorState = CursorState.Normal;
                _batteryPercentage = 100f;
                _camera.Position = new Vector3(0, 1.6f, 0);

                foreach (var battery in _collectibleBatteries)
                {
                    battery.Reset();
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Exit", new System.Numerics.Vector2(120, 60)))
                Close();
            ImGui.SetWindowFontScale(1.0f);

            ImGui.End();
        }
    }
}