using System;
using System.IO;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
namespace AclInteroption;

public static class Preprocessor
{
    public static byte[] PreprocessImage(string imagePath, int targetWidth = 640, int targetHeight = 640)
    {
        using var image = Image.Load<Rgb24>(imagePath);

        // resize
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(targetWidth, targetHeight),
            Mode = ResizeMode.Stretch
        }));

        float[] chw = new float[3 * targetHeight * targetWidth];

        // HWC → CHW
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < targetHeight; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < targetWidth; x++)
                {
                    Rgb24 pixel = row[x];

                    int offset = y * targetWidth + x;

                    chw[0 * targetHeight * targetWidth + offset] = pixel.R / 255f; // R 通道
                    chw[1 * targetHeight * targetWidth + offset] = pixel.G / 255f; // G 通道
                    chw[2 * targetHeight * targetWidth + offset] = pixel.B / 255f; // B 通道
                }
            }
        });

        // 转 byte[]，保证和 ACL 接口兼容
        byte[] bytes = new byte[chw.Length * sizeof(float)];
        Buffer.BlockCopy(chw, 0, bytes, 0, bytes.Length);

        return bytes;
    }

    /// <summary>
    /// 使用 OpenCVSharp4 预处理图片，返回 Mat（float32，形状 [1,3,H,W]）
    /// </summary>
    public static Mat PreprocessImageToMat(
        string imagePath,
        int targetWidth = 640,
        int targetHeight = 640,
        bool normalize = true,
        bool swapRB = true)
    {
        // 读取图像 (BGR)
        using var src = Cv2.ImRead(imagePath, ImreadModes.Color);
        if (src.Empty())
            throw new FileNotFoundException($"Failed to load image: {imagePath}");

        // scale factor: 如果要归一化 [0,1]，就用 1/255，否则用 1.0
        double scale = normalize ? 1.0 / 255.0 : 1.0;

        // OpenCV DNN 工具函数：会自动 resize + HWC→CHW + NCHW
        Mat blob = CvDnn.BlobFromImage(
            src,
            scaleFactor: scale,
            size: new OpenCvSharp.Size(targetWidth, targetHeight),
            mean: new Scalar(0, 0, 0), // 不做 mean 减法
            swapRB: swapRB,            // BGR → RGB
            crop: false
        );

        return blob; // 形状 [1,3,H,W]
    }

    /// <summary>
    /// 多张图像预处理（返回 [N,3,H,W]）
    /// </summary>
    public static Mat PreprocessImagesToBatchMat(
        IEnumerable<string> imagePaths,
        int targetWidth = 640,
        int targetHeight = 640,
        bool normalize = true,
        bool swapRB = true)
    {
        var mats = new List<Mat>();
        foreach (var path in imagePaths)
        {
            var src = Cv2.ImRead(path, ImreadModes.Color);
            if (src.Empty())
                throw new FileNotFoundException($"Failed to load image: {path}");

            mats.Add(src);
        }

        double scale = normalize ? 1.0 / 255.0 : 1.0;

        // OpenCV DNN 接口：一次处理多张图
        Mat batchBlob = CvDnn.BlobFromImages(
            mats,
            scale,
            new OpenCvSharp.Size(targetWidth, targetHeight),
            new Scalar(0, 0, 0),
            swapRB,
            false
        );

        // mats 里存的 Mat 要在这里手动释放
        foreach (var m in mats) m.Dispose();

        return batchBlob; // shape [N,3,H,W]
    }
}