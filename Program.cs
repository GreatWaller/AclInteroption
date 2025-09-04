// See https://aka.ms/new-console-template for more information
using AclInteroption;
using System.Runtime.InteropServices;

internal class Program
{
    private static void Main(string[] args)
    {
        string aclConfigPath = "./acl.json";
        string modelPath = "./model/yolo11n.om";
        string[] inputFiles = { "./data/input_640.bin" };
        string inputDataFile = "./data/input_640.bin";

        using var acl = new AclWrapper();

        Console.WriteLine(">>> Init resources...");
        acl.Init(0, aclConfigPath, modelPath);

        Console.WriteLine(">>> Run inference...");
        // acl.Run(inputFiles);
        byte[] inputData = System.IO.File.ReadAllBytes(inputDataFile);
        acl.Run(inputData);

        Console.WriteLine(">>> Fetch outputs...");
        var outputs = acl.GetOutput();

        for (int i = 0; i < outputs.Length; i++)
        {
            Console.WriteLine($"Output[{i}] size={outputs[i].Length} bytes");

            // 转 float 数组
            float[] floats = AclWrapper.BytesToFloatArray(outputs[i]);

            Console.WriteLine($"  As float[] -> length={floats.Length}");
            Console.WriteLine($"  First 10 values: {string.Join(", ", floats.AsSpan(0, Math.Min(10, floats.Length)).ToArray())}");
        }

        Console.WriteLine(">>> Done!");
    }
}