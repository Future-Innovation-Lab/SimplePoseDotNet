namespace SimplePoseDotNet.Constants;

public static class CocoSkeleton
{
    // COCO skeleton connections (pairs of keypoint indices to connect)
    public static readonly (int, int)[] Connections = new[]
    {
        // Face
        (0, 1),   // Nose to Left Eye
        (0, 2),   // Nose to Right Eye
        (1, 3),   // Left Eye to Left Ear
        (2, 4),   // Right Eye to Right Ear
        
        // Upper body
        (5, 6),   // Left Shoulder to Right Shoulder
        (5, 7),   // Left Shoulder to Left Elbow
        (7, 9),   // Left Elbow to Left Wrist
        (6, 8),   // Right Shoulder to Right Elbow
        (8, 10),  // Right Elbow to Right Wrist
        
        // Torso
        (5, 11),  // Left Shoulder to Left Hip
        (6, 12),  // Right Shoulder to Right Hip
        (11, 12), // Left Hip to Right Hip
        
        // Lower body
        (11, 13), // Left Hip to Left Knee
        (13, 15), // Left Knee to Left Ankle
        (12, 14), // Right Hip to Right Knee
        (14, 16)  // Right Knee to Right Ankle
    };

    // Colors for different body parts (RGB)
    public static readonly (byte R, byte G, byte B)[] ConnectionColors = new (byte, byte, byte)[]
    {
        ((byte)255, (byte)0, (byte)0),     // Face - Red
        ((byte)255, (byte)0, (byte)0),     // Face - Red
        ((byte)255, (byte)0, (byte)0),     // Face - Red
        ((byte)255, (byte)0, (byte)0),     // Face - Red
        
        ((byte)0, (byte)255, (byte)0),     // Shoulders - Green
        ((byte)0, (byte)255, (byte)255),   // Left arm - Cyan
        ((byte)0, (byte)255, (byte)255),   // Left arm - Cyan
        ((byte)255, (byte)255, (byte)0),   // Right arm - Yellow
        ((byte)255, (byte)255, (byte)0),   // Right arm - Yellow
        
        ((byte)0, (byte)255, (byte)0),     // Torso - Green
        ((byte)0, (byte)255, (byte)0),     // Torso - Green
        ((byte)0, (byte)255, (byte)0),     // Torso - Green
        
        ((byte)255, (byte)0, (byte)255),   // Left leg - Magenta
        ((byte)255, (byte)0, (byte)255),   // Left leg - Magenta
        ((byte)0, (byte)0, (byte)255),     // Right leg - Blue
        ((byte)0, (byte)0, (byte)255)      // Right leg - Blue
    };
}
