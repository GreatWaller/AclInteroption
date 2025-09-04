using System;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
namespace AclInteroption;

public class AclWrapper : IDisposable
{
    private IntPtr _handle = IntPtr.Zero;
        private bool _disposed;

        public AclWrapper()
        {
            _handle = Native.Acl_Create();
            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException("Acl_Create returned null handle.");
        }

        public void Init(int deviceId, string aclConfigPath, string modelPath)
        {
            EnsureNotDisposed();

            int ret = Native.Acl_InitResource(_handle, deviceId, aclConfigPath);
            if (ret != 0) throw new InvalidOperationException($"Acl_InitResource failed, ret={ret}");

            ret = Native.Acl_InitModel(_handle, modelPath);
            if (ret != 0) throw new InvalidOperationException($"Acl_InitModel failed, ret={ret}");
        }

        public void Run(params string[] inputFiles)
        {
            EnsureNotDisposed();
            if (inputFiles == null || inputFiles.Length == 0)
                throw new ArgumentException("inputFiles is null or empty.");

            int ret = Native.Acl_Process(_handle, inputFiles, inputFiles.Length);
            if (ret != 0) throw new InvalidOperationException($"Acl_Process failed, ret={ret}");
        }
        
        public void Run(byte[] inputData)
        {
            GCHandle pinned = GCHandle.Alloc(inputData, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = pinned.AddrOfPinnedObject();
                if (Native.Acl_ProcessMemory(_handle, ptr, (UIntPtr)inputData.Length) != 0)
                    throw new InvalidOperationException("Acl_ProcessMemory failed");
            }
            finally
            {
                pinned.Free();
            }
        }

        public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (_handle != IntPtr.Zero)
            {
                // 释放底层资源（ACL、模型、缓冲）
                Native.Acl_DestroyResource(_handle);
                Native.Acl_Destroy(_handle);
            }
        }
        finally
        {
            _handle = IntPtr.Zero;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
        /// <summary>
        /// 获取推理输出（按 byte[] 返回）
        /// </summary>
        public byte[][] GetOutput()
        {
            if (_handle == IntPtr.Zero) throw new ObjectDisposedException(nameof(AclWrapper));

            IntPtr outputsPtr;
            int count;
            int ret = Native.Acl_GetOutput(_handle, out outputsPtr, out count);
            if (ret != 0) throw new InvalidOperationException($"Acl_GetOutput failed: {ret}");

            var result = new byte[count][];
            int structSize = Marshal.SizeOf(typeof(AclOutputData));

            for (int i = 0; i < count; i++)
            {
                IntPtr structPtr = IntPtr.Add(outputsPtr, i * structSize);
                var output = Marshal.PtrToStructure<AclOutputData>(structPtr)!;

                int size = (int)output.size;
                byte[] buffer = new byte[size];
                Marshal.Copy(output.data, buffer, 0, size);

                result[i] = buffer;
            }

            Native.Acl_FreeOutput(outputsPtr, count);
            return result;
        }

        /// <summary>
        /// 将 byte[] 转换为 float[] （假设模型输出是 float32）
        /// </summary>
        public static float[] BytesToFloatArray(byte[] bytes)
        {
            if (bytes.Length % 4 != 0)
                throw new ArgumentException("Byte array length is not a multiple of 4 (float32 size).");

            int count = bytes.Length / 4;
            float[] result = new float[count];

            for (int i = 0; i < count; i++)
            {
                // Ascend 输出是小端序（LE），和 x86 一致
                int intVal = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(i * 4, 4));
                result[i] = BitConverter.Int32BitsToSingle(intVal);
            }

            return result;
        }
        ~AclWrapper() => Dispose();

        private void EnsureNotDisposed()
        {
            if (_disposed || _handle == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(AclWrapper));
        }
}

