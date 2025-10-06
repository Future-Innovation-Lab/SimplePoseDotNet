using SkiaSharp;
using SimplePoseDotNet.Models;
using SimplePoseDotNet.Constants;

namespace SimplePoseDotNet.Services;

public class SkeletonDrawer
{
    private const int KeypointRadius = 5;
    private const int LineThickness = 3;
    private const float MinConfidence = 0.3f;

    public void DrawSkeleton(string inputImagePath, List<PoseResult> poses, string outputImagePath)
    {
        // Load the original image
        using var originalBitmap = SKBitmap.Decode(inputImagePath);
        
        // Create a surface to draw on
        using var surface = SKSurface.Create(new SKImageInfo(originalBitmap.Width, originalBitmap.Height));
        var canvas = surface.Canvas;
        
        // Draw the original image
        canvas.DrawBitmap(originalBitmap, 0, 0);
        
        // Draw each detected person
        foreach (var pose in poses)
        {
            DrawPerson(canvas, pose);
        }
        
        // Save the result
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputImagePath);
        data.SaveTo(stream);
        
        Console.WriteLine($"Skeleton visualization saved to: {outputImagePath}");
    }

    private void DrawPerson(SKCanvas canvas, PoseResult pose)
    {
        // Draw bounding box
        DrawBoundingBox(canvas, pose.Box, pose.Confidence);
        
        // Draw skeleton connections
        DrawConnections(canvas, pose.Keypoints);
        
        // Draw keypoints on top
        DrawKeypoints(canvas, pose.Keypoints);
    }

    private void DrawBoundingBox(SKCanvas canvas, BoundingBox box, float confidence)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(0, 255, 0),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        var rect = new SKRect(box.X, box.Y, box.X + box.Width, box.Y + box.Height);
        canvas.DrawRect(rect, paint);
        
        // Draw confidence label
        using var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 20);
        using var textPaint = new SKPaint
        {
            Color = new SKColor(0, 255, 0),
            IsAntialias = true
        };
        
        string label = $"Person {confidence:F2}";
        canvas.DrawText(label, box.X, box.Y - 5, font, textPaint);
    }

    private void DrawConnections(SKCanvas canvas, Keypoint[] keypoints)
    {
        for (int i = 0; i < CocoSkeleton.Connections.Length; i++)
        {
            var (start, end) = CocoSkeleton.Connections[i];
            
            if (start >= keypoints.Length || end >= keypoints.Length)
                continue;
            
            var kp1 = keypoints[start];
            var kp2 = keypoints[end];
            
            // Only draw if both keypoints have sufficient confidence
            if (kp1.Confidence < MinConfidence || kp2.Confidence < MinConfidence)
                continue;
            
            var color = CocoSkeleton.ConnectionColors[i];
            
            using var paint = new SKPaint
            {
                Color = new SKColor(color.R, color.G, color.B),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = LineThickness,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
            
            canvas.DrawLine(kp1.X, kp1.Y, kp2.X, kp2.Y, paint);
        }
    }

    private void DrawKeypoints(SKCanvas canvas, Keypoint[] keypoints)
    {
        for (int i = 0; i < keypoints.Length; i++)
        {
            var kp = keypoints[i];
            
            if (kp.Confidence < MinConfidence)
                continue;
            
            // Draw outer circle (white border)
            using var borderPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(kp.X, kp.Y, KeypointRadius + 1, borderPaint);
            
            // Draw inner circle (colored by confidence)
            byte intensity = (byte)(kp.Confidence * 255);
            using var fillPaint = new SKPaint
            {
                Color = new SKColor(255, intensity, 0), // Orange to red gradient based on confidence
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(kp.X, kp.Y, KeypointRadius, fillPaint);
        }
    }
}
