using System;

namespace AclInteroption;

public class Detection
{
    public float X1 { get; set; } // 左上角
    public float Y1 { get; set; }
    public float X2 { get; set; } // 右下角
    public float Y2 { get; set; }
    public float Confidence { get; set; }
    public int ClassId { get; set; }
}

public static class YoloPostProcessor
{
    public static List<Detection> PostProcess1(
        byte[] output,
        int numClasses,
        int inputW = 640,
        int inputH = 640,
        float confThreshold = 0.25f,
        float nmsThreshold = 0.45f)
    {
        // 将 byte[] 转成 float[]
        int floatsCount = output.Length / sizeof(float);
        float[] preds = new float[floatsCount];
        Buffer.BlockCopy(output, 0, preds, 0, output.Length);

        int numAttrs = 4 + numClasses;   // box(4) + 类别概率
        int numPreds = floatsCount / numAttrs;

        var detections = new List<Detection>();

        for (int i = 0; i < numPreds; i++)
        {
            int offset = i * numAttrs;

            float cx = preds[offset + 0];
            float cy = preds[offset + 1];
            float w  = preds[offset + 2];
            float h  = preds[offset + 3];

            // 类别概率
            float maxProb = 0f;
            int classId = -1;

            for (int c = 0; c < numClasses; c++)
            {
                float prob = preds[offset + 4 + c];
                if (prob > maxProb)
                {
                    System.Console.WriteLine($"line: {i}, prob: {prob}, classid:{c}");
                    maxProb = prob;
                    classId = c;
                }
            }

            if (maxProb < confThreshold) continue;
            // System.Console.WriteLine($"line: {i}, maxProb: {maxProb}, classid:{classId}");
            float x1 = cx - w / 2;
            float y1 = cy - h / 2;
            float x2 = cx + w / 2;
            float y2 = cy + h / 2;

            detections.Add(new Detection
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Confidence = maxProb,
                ClassId = classId
            });
        }

        // NMS 去重
        return NonMaxSuppression(detections, nmsThreshold);
    }

    public static List<Detection> PostProcess(
        byte[] output,
        int numClasses,
        int inputW = 640,
        int inputH = 640,
        float confThreshold = 0.25f,
        float nmsThreshold = 0.45f)
    {
        int floatsCount = output.Length / sizeof(float);
        float[] preds = new float[floatsCount];
        Buffer.BlockCopy(output, 0, preds, 0, output.Length);

        // 自动计算 numPreds
        int numPreds = floatsCount / (4 + numClasses);

        var detections = new List<Detection>();

        for (int i = 0; i < numPreds; i++)
        {
            // 属性优先排列
            float cx = preds[i];                       // cx
            float cy = preds[i + numPreds];            // cy
            float w  = preds[i + numPreds * 2];        // w
            float h  = preds[i + numPreds * 3];        // h

            // 找最大类别概率
            float maxProb = 0f;
            int classId = -1;
            for (int c = 0; c < numClasses; c++)
            {
                float prob = preds[i + numPreds * (4 + c)];
                if (prob > maxProb)
                {
                    // System.Console.WriteLine($"line: {i}, prob: {prob}, classid:{c}");

                    maxProb = prob;
                    classId = c;
                }
            }

            if (maxProb < confThreshold) continue;

            float x1 = cx - w / 2;
            float y1 = cy - h / 2;
            float x2 = cx + w / 2;
            float y2 = cy + h / 2;

            detections.Add(new Detection
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Confidence = maxProb,
                ClassId = classId
            });
        }

        return NonMaxSuppression(detections, nmsThreshold);
    }

    private static List<Detection> NonMaxSuppression(List<Detection> detections, float iouThreshold)
    {
        var results = new List<Detection>();
        var sorted = detections.OrderByDescending(d => d.Confidence).ToList();

        while (sorted.Count > 0)
        {
            var best = sorted[0];
            results.Add(best);
            sorted.RemoveAt(0);

            sorted = sorted.Where(det => IoU(best, det) < iouThreshold).ToList();
        }
        return results;
    }

    private static float IoU(Detection a, Detection b)
    {
        float interX1 = Math.Max(a.X1, b.X1);
        float interY1 = Math.Max(a.Y1, b.Y1);
        float interX2 = Math.Min(a.X2, b.X2);
        float interY2 = Math.Min(a.Y2, b.Y2);

        float interArea = Math.Max(0, interX2 - interX1) * Math.Max(0, interY2 - interY1);
        float unionArea = (a.X2 - a.X1) * (a.Y2 - a.Y1) + (b.X2 - b.X1) * (b.Y2 - b.Y1) - interArea;

        return unionArea > 0 ? interArea / unionArea : 0f;
    }
}