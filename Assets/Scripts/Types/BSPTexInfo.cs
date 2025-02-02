using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BSPTexInfo
{
    public Vector3[] vec3st; //holds s and t
   
    public float[] offst; // Texture shift in s and t direction
  
    public UInt32 miptex; // Index into textures array
    public UInt32 flags; // Texture flags

    public BSPTexInfo(Vector3 vs, float os, Vector3 vt, float ot, UInt32 miptex, UInt32 flags)
    {

        vec3st = new Vector3[2] { vs, vt };
        this.miptex = miptex;
        this.flags = flags;
        offst = new float[2] { os, ot };

        Swizzle();
    }

 

    // Maybe should scale the offsets too?
    private void Swizzle()
    {
        //vec3s.Scale(new Vector3(0.03f, 0.03f, 0.03f));
        //vec3t.Scale(new Vector3(0.03f, 0.03f, 0.03f));

        float tempx = -vec3st[0].x;
        float tempy = vec3st[0].z;
        float tempz = -vec3st[0].y;

        vec3st[0] = new Vector3(tempx, tempy, tempz);

        tempx = -vec3st[1].x;
        tempy = vec3st[1].z;
        tempz = -vec3st[1].y;

        vec3st[1] = new Vector3(tempx, tempy, tempz);

      //  offs = offs * 0.03f;
     //   offt = offt * 0.03f;
    }
}

