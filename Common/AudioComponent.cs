using System;
using System.IO;
using System.Runtime.InteropServices;
using Silk.NET.OpenAL;
using OpenTK.Mathematics;

namespace FinalProject.Common
{
    public class AudioComponent : IDisposable
    {
        private static AL? _al;
        private static ALContext? _alc;
        private static unsafe Device* _device;
        private static unsafe Context* _context;
        private static bool _initialized = false;
        private static IntPtr _nativeLib;

        // Delegate types for OpenAL functions
        private delegate void AlListener3fDelegate(int param, float v1, float v2, float v3);
        private unsafe delegate void AlListenerfvDelegate(int param, float* values);
        private delegate void AlSource3fDelegate(uint source, int param, float v1, float v2, float v3);

        private static AlListener3fDelegate? _alListener3f;
        private static AlListenerfvDelegate? _alListenerfv;
        private static AlSource3fDelegate? _alSource3f;

        private const int AL_POSITION = 0x1004;
        private const int AL_ORIENTATION = 0x100F;

        private uint _buffer;
        private uint _source;
        private Vector3 _position;

        public static unsafe bool InitializeAudio()
        {
            if (_initialized) return true;

            try
            {
                _alc = ALContext.GetApi(true);
                _al = AL.GetApi(true);

                _device = _alc.OpenDevice(null);
                if (_device == null)
                {
                    Console.WriteLine("Failed to open OpenAL device");
                    return false;
                }

                _context = _alc.CreateContext(_device, null);
                if (_context == null)
                {
                    Console.WriteLine("Failed to create OpenAL context");
                    _alc.CloseDevice(_device);
                    return false;
                }

                if (!_alc.MakeContextCurrent(_context))
                {
                    Console.WriteLine("Failed to make OpenAL context current");
                    _alc.DestroyContext(_context);
                    _alc.CloseDevice(_device);
                    return false;
                }

                // Shared lib loading and function pointers
                string[] libNames = { "soft_oal", "OpenAL32", "openal", "libopenal.so.1", "libopenal.1.dylib" };
                foreach (var name in libNames)
                {
                    if (NativeLibrary.TryLoad(name, out _nativeLib))
                    {
                        Console.WriteLine($"Loaded OpenAL native library: {name}");
                        break;
                    }
                }

                if (_nativeLib != IntPtr.Zero)
                {
                    if (NativeLibrary.TryGetExport(_nativeLib, "alListener3f", out var listener3f))
                        _alListener3f = Marshal.GetDelegateForFunctionPointer<AlListener3fDelegate>(listener3f);
                    if (NativeLibrary.TryGetExport(_nativeLib, "alListenerfv", out var listenerfv))
                        _alListenerfv = Marshal.GetDelegateForFunctionPointer<AlListenerfvDelegate>(listenerfv);
                    if (NativeLibrary.TryGetExport(_nativeLib, "alSource3f", out var source3f))
                        _alSource3f = Marshal.GetDelegateForFunctionPointer<AlSource3fDelegate>(source3f);

                    Console.WriteLine($"3D Audio functions loaded: Listener3f={_alListener3f != null}, Listenerfv={_alListenerfv != null}, Source3f={_alSource3f != null}");
                }
                else
                {
                    Console.WriteLine("WARNING: Could not load OpenAL native library for 3D audio functions");
                }

                _al.DistanceModel(DistanceModel.InverseDistanceClamped);

                _initialized = true;
                Console.WriteLine("OpenAL initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAL initialization failed: {ex.Message}");
                return false;
            }
        }

        public static unsafe void ShutdownAudio()
        {
            if (!_initialized) return;

            _alc!.MakeContextCurrent(null);
            _alc.DestroyContext(_context);
            _alc.CloseDevice(_device);

            _al!.Dispose();
            _alc.Dispose();

            _initialized = false;
        }

        public static unsafe void UpdateUserAudioLocationFromCamera(Camera camera)
        {
            if (!_initialized) return;

            // Set listener position
            _alListener3f?.Invoke(AL_POSITION, camera.Position.X, camera.Position.Y, camera.Position.Z);

            // Set listener orientation (6 floats: forward vector, then up vector)
            if (_alListenerfv != null)
            {
                float* orientation = stackalloc float[6] // ??? LOL
                {
                    camera.Front.X, camera.Front.Y, camera.Front.Z,
                    camera.Up.X, camera.Up.Y, camera.Up.Z
                };
                _alListenerfv(AL_ORIENTATION, orientation);
            }
        }

        public AudioComponent(string wavFilePath, Vector3 position, float distance)
        {
            if (!_initialized || _al == null)
                throw new InvalidOperationException("Call AudioComponent.InitializeAudio() before creating audio components.");

            _position = position;
            _buffer = LoadWav(wavFilePath);
            _source = _al.GenSource();

            _al.SetSourceProperty(_source, SourceInteger.Buffer, (int)_buffer);
            SetSourcePosition(_source, position);

            _al.SetSourceProperty(_source, SourceFloat.ReferenceDistance, 1.0f);
            _al.SetSourceProperty(_source, SourceFloat.MaxDistance, distance);
            _al.SetSourceProperty(_source, SourceFloat.RolloffFactor, 0.3f);
        }

        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                SetSourcePosition(_source, value);
            }
        }

        private static void SetSourcePosition(uint source, Vector3 position)
        {
            _alSource3f?.Invoke(source, AL_POSITION, position.X, position.Y, position.Z);
        }

        public void Play() => _al!.SourcePlay(_source);
        public void Stop() => _al!.SourceStop(_source);
        public void Pause() => _al!.SourcePause(_source);

        public bool IsPlaying
        {
            get
            {
                _al!.GetSourceProperty(_source, GetSourceInteger.SourceState, out int state);
                return state == (int)SourceState.Playing;
            }
        }

        public bool Looping
        {
            get
            {
                _al!.GetSourceProperty(_source, SourceBoolean.Looping, out bool looping);
                return looping;
            }
            set => _al!.SetSourceProperty(_source, SourceBoolean.Looping, value);
        }

        public float Volume
        {
            get
            {
                _al!.GetSourceProperty(_source, SourceFloat.Gain, out float gain);
                return gain;
            }
            set => _al!.SetSourceProperty(_source, SourceFloat.Gain, Math.Clamp(value, 0f, 10f));
        }

        public float Pitch
        {
            get
            {
                _al!.GetSourceProperty(_source, SourceFloat.Pitch, out float pitch);
                return pitch;
            }
            set => _al!.SetSourceProperty(_source, SourceFloat.Pitch, Math.Clamp(value, 0.5f, 2.0f));
        }

        private uint LoadWav(string filePath)
        {

            uint buffer = _al!.GenBuffer();

            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            string riff = new string(reader.ReadChars(4));
            if (riff != "RIFF")
                throw new InvalidDataException("Not a valid WAV file (missing RIFF header)");

            reader.ReadInt32();
            string wave = new string(reader.ReadChars(4));
            if (wave != "WAVE")
                throw new InvalidDataException("Not a valid WAV file (missing WAVE header)");

            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;

            while (stream.Position < stream.Length)
            {
                string chunkId = new string(reader.ReadChars(4));
                int chunkSize = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    reader.ReadInt16();
                    channels = reader.ReadInt16();
                    sampleRate = reader.ReadInt32();
                    reader.ReadInt32(); 
                    reader.ReadInt16(); 
                    bitsPerSample = reader.ReadInt16();

                    if (chunkSize > 16)
                        reader.ReadBytes(chunkSize - 16);
                }
                else if (chunkId == "data")
                {
                    byte[] audioData = reader.ReadBytes(chunkSize);

                    BufferFormat format = (channels, bitsPerSample) switch
                    {
                        (1, 8) => BufferFormat.Mono8,
                        (1, 16) => BufferFormat.Mono16,
                        (2, 8) => BufferFormat.Stereo8,
                        (2, 16) => BufferFormat.Stereo16,
                        _ => throw new NotSupportedException($"Unsupported WAV format: {channels} channels, {bitsPerSample} bits")
                    };

                    _al.BufferData(buffer, format, audioData, sampleRate);
                    break;
                }
                else
                {
                    reader.ReadBytes(chunkSize);
                }
            }

            return buffer;
        }

        public void Dispose()
        {
            _al!.SourceStop(_source);
            _al.DeleteSource(_source);
            _al.DeleteBuffer(_buffer);
        }
    }
}
