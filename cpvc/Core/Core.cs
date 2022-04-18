using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CPvC
{
    /// <summary>
    /// Class that wraps the CoreCLR class.
    /// </summary>
    public sealed class Core : IDisposable
    {
        // Core versions.
        public const int LatestVersion = 1;
        private class Corev1 : CoreCLR, ICore { }

        public enum Type
        {
            CPC6128
        }

        private ICore _coreCLR;
        private int _version;

        public int Version
        {
            get
            {
                return _version;
            }
        }

        public Core(int version, Type type)
        {
            Create(version, type);
        }

        static private ICore CreateVersionedCore(int version)
        {
            switch (version)
            {
                case 1:
                    return new Corev1();
                default:
                    throw new ArgumentException(String.Format("Cannot instantiate CLR core version {0}.", version), nameof(version));
            }
        }

        public void Close()
        {
            _coreCLR?.Dispose();
            _coreCLR = null;
        }

        public void Dispose()
        {
            Close();
        }

        public bool KeyPressSync(byte keycode, bool down)
        {
            return _coreCLR.KeyPress(keycode, down);
        }

        public void ResetSync()
        {
            _coreCLR.Reset();
        }

        public void LoadDiscSync(byte drive, byte[] discImage)
        {
            _coreCLR.LoadDisc(drive, discImage);
        }

        public void LoadTapeSync(byte[] tapeImage)
        {
            _coreCLR.LoadTape(tapeImage);
        }

        /// <summary>
        /// Sets width, height, and pitch of the screen.
        /// </summary>
        public void SetScreen()
        {
            _coreCLR.SetScreen(Display.Pitch, Display.Height, Display.Width);
        }

        public void CopyScreen(IntPtr screenBuffer, UInt64 size)
        {
            _coreCLR?.CopyScreen(screenBuffer, size);
        }

        /// <summary>
        /// Indicates the number of ticks that have elapsed since the core was started. Note that each tick is exactly 0.25 microseconds.
        /// </summary>
        public UInt64 Ticks
        {
            get
            {
                return _coreCLR?.Ticks() ?? 0;
            }
        }

        /// <summary>
        /// Serializes the core to a byte array.
        /// </summary>
        /// <returns>A byte array containing the serialized core.</returns>
        public byte[] GetState()
        {
            return _coreCLR.GetState();
        }

        /// <summary>
        /// Deserializes the core from a byte array.
        /// </summary>
        /// <param name="state">A byte array created by <c>GetState</c>.</param>
        public void LoadState(byte[] state)
        {
            _coreCLR.LoadState(state);
        }

        public void CreateFromBookmark(int version, byte[] state)
        {
            Create(version, Type.CPC6128);

            _coreCLR.LoadState(state);
        }

        public void ProcessCoreVersion(int version)
        {
            byte[] state = GetState();

            ICore newCore = Core.CreateVersionedCore(version);
            newCore.LoadState(state);
            newCore.SetScreen(Display.Pitch, Display.Height, Display.Width);

            ICore oldCore = _coreCLR;
            _coreCLR = newCore;
            oldCore.Dispose();
        }

        public void Create(int version, Type type)
        {
            switch (type)
            {
                case Type.CPC6128:
                    {
                        _version = version;
                        _coreCLR = CreateVersionedCore(version);

                        SetScreen();

                        SetLowerROM(Resources.OS6128);
                        SetUpperROM(0, Resources.Basic6128);
                        SetUpperROM(7, Resources.Amsdos6128);
                    }
                    break;
                default:
                    throw new ArgumentException(String.Format("Unknown core type {0}", type));
            }
        }

        public void SetLowerROM(byte[] lowerROM)
        {
            _coreCLR.LoadLowerROM(lowerROM);
        }

        public void SetUpperROM(byte slot, byte[] upperROM)
        {
            _coreCLR.LoadUpperROM(slot, upperROM);
        }

        /// <summary>
        /// Executes instruction cycles until the core's clock is equal to or greater than <c>ticks</c>.
        /// </summary>
        /// <param name="ticks">Clock value to run core until.</param>
        /// <param name="stopReason">Bitmask specifying what conditions will force execution to stop prior to the clock reaching <c>ticks</c>.</param>
        /// <returns>A bitmask specifying why the execution stopped. See <c>StopReasons</c> for a list of values.</returns>
        public byte RunUntil(UInt64 ticks, byte stopReason, List<UInt16> audioSamples)
        {
            return _coreCLR.RunUntil(ticks, stopReason, audioSamples);
        }

        public void CreateSnapshotSync(int id)
        {
            _coreCLR.CreateSnapshot(id);
        }

        public bool DeleteSnapshotSync(int id)
        {
            return _coreCLR.DeleteSnapshot(id);
        }

        public bool RevertToSnapshotSync(int id)
        {
            return _coreCLR.RevertToSnapshot(id);
        }
    }
}
