using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using SimplePoseDotNet.Models;

namespace SimplePoseDotNet.Services;

public class YoloPoseProcessor : IDisposable
{
    private readonly InferenceSession _session;
    private readonly int _inputWidth = 640;
    private readonly int _inputHeight = 640;
    private readonly float _confidenceThreshold = 0.25f;
    private readonly float _iouThreshold = 0.45f;

    public YoloPoseProcessor(string modelPath)
    {
        var options = new SessionOptions();
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        
        _session = new InferenceSession(modelPath, options);
        
        Console.WriteLine("YOLO Pose Model loaded successfully!");
        Console.WriteLine($"Input: {_session.InputMetadata.First().Key}");
        Console.WriteLine($"Output: {string.Join(", ", _session.OutputMetadata.Keys)}");
        Console.WriteLine();
    }

    public List<PoseResult> ProcessImage(string imagePath)
    {
        // Load and preprocess image
        using var bitmap = SKBitmap.Decode(imagePath);
        int originalWidth = bitmap.Width;
        int originalHeight = bitmap.Height;
        
        Console.WriteLine($"Processing image: {originalWidth}x{originalHeight}");
        
        // Calculate scaling and padding for letterboxing
        float scaleX = (float)_inputWidth / originalWidth;
        float scaleY = (float)_inputHeight / originalHeight;
        float scale = Math.Min(scaleX, scaleY);
        
        int scaledWidth = (int)(originalWidth * scale);
        int scaledHeight = (int)(originalHeight * scale);
        int padX = (_inputWidth - scaledWidth) / 2;
        int padY = (_inputHeight - scaledHeight) / 2;
        
        // Create letterboxed input tensor
        var inputTensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });
        
        // Resize image to scaled size
        var resizeInfo = new SKImageInfo(scaledWidth, scaledHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var resizedBitmap = bitmap.Resize(resizeInfo, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        
        // Convert to tensor with letterboxing (normalized to 0-1)
        unsafe
        {
            byte* pixels = (byte*)resizedBitmap.GetPixels().ToPointer();
            int bytesPerPixel = 4; // RGBA
            
            for (int y = 0; y < scaledHeight; y++)
            {
                for (int x = 0; x < scaledWidth; x++)
                {
                    int pixelOffset = (y * scaledWidth + x) * bytesPerPixel;
                    byte r = pixels[pixelOffset + 0];
                    byte g = pixels[pixelOffset + 1];
                    byte b = pixels[pixelOffset + 2];
                    
                    inputTensor[0, 0, y + padY, x + padX] = r / 255f;
                    inputTensor[0, 1, y + padY, x + padX] = g / 255f;
                    inputTensor[0, 2, y + padY, x + padX] = b / 255f;
                }
            }
        }
        
        Console.WriteLine("Running inference...");
        
        // Run inference
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_session.InputMetadata.First().Key, inputTensor)
        };
        
        using var results = _session.Run(inputs);
        
        // Process outputs
        var output = results.First().AsEnumerable<float>().ToArray();
        var poses = ParseOutput(output, scale, padX, padY, originalWidth, originalHeight);
        
        Console.WriteLine($"Detected {poses.Count} person(s)");
        
        return poses;
    }

    private List<PoseResult> ParseOutput(float[] output, float scale, int padX, int padY, int originalWidth, int originalHeight)
    {
        // YOLO11 pose output format: [batch, 56, 8400]
        // 56 = 4 (bbox) + 1 (confidence) + 51 (17 keypoints * 3: x, y, confidence)
        
        var poses = new List<PoseResult>();
        int numDetections = 8400;
        int numChannels = 56;
        
        // Transpose output from [1, 56, 8400] to [8400, 56] for easier processing
        var detections = new float[numDetections][];
        for (int i = 0; i < numDetections; i++)
        {
            detections[i] = new float[numChannels];
            for (int j = 0; j < numChannels; j++)
            {
                detections[i][j] = output[j * numDetections + i];
            }
        }
        
        // Filter by confidence and apply NMS
        var candidates = new List<(PoseResult pose, int index)>();
        
        for (int i = 0; i < numDetections; i++)
        {
            var detection = detections[i];
            float confidence = detection[4];
            
            if (confidence < _confidenceThreshold)
                continue;
            
            // Parse bounding box (center_x, center_y, width, height)
            float cx = detection[0];
            float cy = detection[1];
            float w = detection[2];
            float h = detection[3];
            
            // Convert from letterboxed coordinates to original image coordinates
            float width = w / scale;
            float height = h / scale;
            
            // Convert from center coordinates to top-left corner
            float x = ((cx - padX) / scale) - (width / 2);
            float y = ((cy - padY) / scale) - (height / 2);
            
            // Clamp to image boundaries
            x = Math.Max(0, Math.Min(x, originalWidth - width));
            y = Math.Max(0, Math.Min(y, originalHeight - height));
            
            // Parse keypoints (17 keypoints with x, y, confidence)
            var keypoints = new Keypoint[17];
            for (int kp = 0; kp < 17; kp++)
            {
                int baseIdx = 5 + kp * 3;
                float kpX = (detection[baseIdx] - padX) / scale;
                float kpY = (detection[baseIdx + 1] - padY) / scale;
                float kpConf = detection[baseIdx + 2];
                
                keypoints[kp] = new Keypoint
                {
                    X = Math.Max(0, Math.Min(kpX, originalWidth)),
                    Y = Math.Max(0, Math.Min(kpY, originalHeight)),
                    Confidence = kpConf
                };
            }
            
            var pose = new PoseResult
            {
                Box = new BoundingBox { X = x, Y = y, Width = width, Height = height },
                Confidence = confidence,
                Keypoints = keypoints
            };
            
            candidates.Add((pose, i));
        }
        
        // Apply Non-Maximum Suppression
        var selected = ApplyNMS(candidates);
        
        return selected;
    }

    private List<PoseResult> ApplyNMS(List<(PoseResult pose, int index)> candidates)
    {
        if (candidates.Count == 0)
            return new List<PoseResult>();
        
        // Sort by confidence descending
        candidates.Sort((a, b) => b.pose.Confidence.CompareTo(a.pose.Confidence));
        
        var results = new List<PoseResult>();
        var suppressed = new bool[candidates.Count];
        
        for (int i = 0; i < candidates.Count; i++)
        {
            if (suppressed[i])
                continue;
            
            results.Add(candidates[i].pose);
            
            // Suppress overlapping boxes
            for (int j = i + 1; j < candidates.Count; j++)
            {
                if (suppressed[j])
                    continue;
                
                float iou = CalculateIoU(candidates[i].pose.Box, candidates[j].pose.Box);
                if (iou > _iouThreshold)
                {
                    suppressed[j] = true;
                }
            }
        }
        
        return results;
    }

    private float CalculateIoU(BoundingBox box1, BoundingBox box2)
    {
        float x1 = Math.Max(box1.X, box2.X);
        float y1 = Math.Max(box1.Y, box2.Y);
        float x2 = Math.Min(box1.X + box1.Width, box2.X + box2.Width);
        float y2 = Math.Min(box1.Y + box1.Height, box2.Y + box2.Height);
        
        float intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        float area1 = box1.Width * box1.Height;
        float area2 = box2.Width * box2.Height;
        float union = area1 + area2 - intersection;
        
        return union > 0 ? intersection / union : 0;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
