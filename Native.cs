using System;
using System.Runtime.InteropServices;

namespace AclInteroption;

[StructLayout(LayoutKind.Sequential)]
internal struct AclOutputData
{
    public IntPtr data;
    public UIntPtr size;
}

internal static class Native
{
#if WINDOWS
        private const string LIB = "aclwrapper.dll";
#else
    private const string LIB = "libaclwrapper.so";
#endif

    // C 接口：句柄风格
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_Create")]
    internal static extern IntPtr Acl_Create();

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_Destroy")]
    internal static extern void Acl_Destroy(IntPtr obj);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_InitResource", CharSet = CharSet.Ansi)]
    internal static extern int Acl_InitResource(IntPtr obj, int deviceId, string aclConfigPath);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_InitModel", CharSet = CharSet.Ansi)]
    internal static extern int Acl_InitModel(IntPtr obj, string modelPath);

    // 在 Unix 上，CharSet.Ansi -> UTF-8；string[] 会按 char** 传递
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_Process", CharSet = CharSet.Ansi)]
    internal static extern int Acl_Process(
        IntPtr obj,
        [In] string[] inputFiles,
        int fileCount
    );
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_ProcessMemory")]
        internal static extern int Acl_ProcessMemory(IntPtr obj, IntPtr inputData, UIntPtr size);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_DestroyResource")]
    internal static extern void Acl_DestroyResource(IntPtr obj);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_GetOutput")]
    internal static extern int Acl_GetOutput(IntPtr obj, out IntPtr outputs, out int count);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Acl_FreeOutput")]
    internal static extern void Acl_FreeOutput(IntPtr outputs, int count);
}
