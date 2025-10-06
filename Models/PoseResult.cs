namespace SimplePoseDotNet.Models;

public class PoseResult
{
    public BoundingBox Box { get; set; } = new();
    public float Confidence { get; set; }
    public Keypoint[] Keypoints { get; set; } = Array.Empty<Keypoint>();
}
