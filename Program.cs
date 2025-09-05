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

        string imagePath     = "./data/dog1_1024_683.jpg";  // 输入 jpg/png
        // 多张图路径
        var paths = new List<string> { "./data/dog1_1024_683.jpg", "./data/dog2_1024_683.jpg"};


        using var acl = new AclWrapper();

        Console.WriteLine(">>> Init resources...");
        acl.Init(0, aclConfigPath, modelPath);

        Console.WriteLine(">>> Run inference...");
        // acl.Run(inputFiles);
        // byte[] inputData = System.IO.File.ReadAllBytes(inputDataFile);
        // 预处理图像 → byte[]
        // var inputData = Preprocessor.PreprocessImage(imagePath, 640, 640);
        // acl.Run(inputData);

        var mat = Preprocessor.PreprocessImageToMat(imagePath, 640, 640);
        Console.WriteLine($"Mat: {mat.Dims}, Type={mat.Type()}, Size={mat.Size()}, Total={mat.Total()}, ElemSize={mat.ElemSize()}");
        acl.Run(mat.Data, (int)mat.Total() * mat.ElemSize());

        // 批量预处理，返回 [N,3,H,W]
        // var batchInput = Preprocessor.PreprocessImagesToBatchMat(paths, 640, 640);
        // Console.WriteLine($"Batch Mat: {batchInput.Dims}, Type={batchInput.Type()}, Size={batchInput.Size()}, Total={batchInput.Total()}, ElemSize={batchInput.ElemSize()}");
        // acl.Run(batchInput.Data, (int)batchInput.Total() * batchInput.ElemSize());

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

        // 假设 YOLOv11n 输出在 outputs[0]，类别数 80（COCO）
        var detections = YoloPostProcessor.PostProcess(outputs[0], numClasses: 80);

        foreach (var det in detections)
        {
            Console.WriteLine($"Class {det.ClassId}, Conf {det.Confidence:F2}, Box=({det.X1:F1},{det.Y1:F1},{det.X2:F1},{det.Y2:F1})");
        }

        Console.WriteLine(">>> Done!");
    }
}