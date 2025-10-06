using SimplePoseDotNet.Services;
using SimplePoseDotNet.Constants;

namespace SimplePoseDotNet;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Simple Pose Estimation with ONNX Runtime");
        Console.WriteLine("==========================================\n");

        if (args.Length < 2)
        {
            ShowUsage();
            return;
        }

        string modelPath = args[0];
        string imagePath = args[1];
        string outputPath = args.Length > 2 ? args[2] : "output_skeleton.png";

        if (!File.Exists(modelPath))
        {
            Console.WriteLine($"Error: Model file not found: {modelPath}");
            return;
        }

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Error: Image file not found: {imagePath}");
            return;
        }

        try
        {
            // Process image
            using var processor = new YoloPoseProcessor(modelPath);
            var poses = processor.ProcessImage(imagePath);

            if (poses.Count == 0)
            {
                Console.WriteLine("No person detected in the image.");
                return;
            }

            // Print detection results
            Console.WriteLine($"\nDetection Results:");
            Console.WriteLine("==================");
            
            for (int i = 0; i < poses.Count; i++)
            {
                var pose = poses[i];
                Console.WriteLine($"\nPerson {i + 1}:");
                Console.WriteLine($"  Confidence: {pose.Confidence:F3}");
                Console.WriteLine($"  Bounding Box: X={pose.Box.X:F1}, Y={pose.Box.Y:F1}, W={pose.Box.Width:F1}, H={pose.Box.Height:F1}");
                Console.WriteLine($"  Keypoints:");
                
                for (int j = 0; j < pose.Keypoints.Length; j++)
                {
                    var kp = pose.Keypoints[j];
                    Console.WriteLine($"    {KeypointNames.CocoKeypoints[j],-15}: ({kp.X:F1}, {kp.Y:F1}) - Confidence: {kp.Confidence:F3}");
                }
            }

            // Draw skeleton visualization
            Console.WriteLine($"\nDrawing skeleton visualization...");
            var drawer = new SkeletonDrawer();
            drawer.DrawSkeleton(imagePath, poses, outputPath);
            
            Console.WriteLine("\nDone!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("Usage: SimplePoseDotNet <model-path> <image-path> [output-path]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  model-path   : Path to the YOLO11 pose ONNX model file");
        Console.WriteLine("  image-path   : Path to the input image file");
        Console.WriteLine("  output-path  : (Optional) Path for the output skeleton image (default: output_skeleton.png)");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  SimplePoseDotNet yolo11x-pose.onnx person.jpg result.png");
    }
}
