using System.Numerics;
using UnityEngine;

public class Face_t
{
    // Constants
    public const int MAXLIGHTMAPS = 4;

    // Member variables
    public int Type { get; set; }
    public int Flags { get; set; }
    public int Texture { get; set; }  // Index into texture
    public Texture Lightmap { get; set; }  // Current lightmap
    public Texture[] Lightmaps { get; set; } // Array of lightmaps
    public int[] Styles { get; set; }  // Array of styles for lightmaps

    public BSPPlane Plane { get; set; }  // dplane_t (assuming PlaneD is a C# equivalent)
                                         // public BSPPlane P { get; set; }  // plane_t (assuming Plane is a C# equivalent)
                                         // public int Side { get; set; }

    public int First { get; set; }
    public int Count { get; set; }

    public Face_t Next { get; set; }  // Linked list of faces

    // Constructor
    public Face_t()
    {
        Texture = -1;
        Lightmap = null;
        Next = null;
        Lightmaps = new Texture[MAXLIGHTMAPS];
        Styles = new int[MAXLIGHTMAPS];

        for (int i = 0; i < MAXLIGHTMAPS; i++)
        {
            Styles[i] = -1;
            Lightmaps[i] = null;
        }
    }

}
