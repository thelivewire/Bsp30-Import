using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

using UnityEditor;


public class GenerateMapVis : MonoBehaviour
{
	public Texture2D missingtexture;//use the included missing.jpg
	public string mapName;
    public FilterMode filterMode = FilterMode.Point;
    public int model1tLeaf;
	public bool renderlights = true;
	private BSP30map map;
	private int faceCount = 0;
	private GameObject[][] leafRoots;
	public Transform player;
	private bool lockpvs = false;
	private int lastpvs = 0;
	public bool RenderAllFaces = false;

	void Start()
	{

		map = new BSP30map(mapName);
		if (map == null)
		{
			Debug.LogError("Problem Loading map!!!");

		}


		if (player == null)
		{
			Debug.LogError("player is null, cant get transform");

		}
		GenerateVisArrays();
		GenerateVisObjects();


	}

	void Update()
	{
		//model1tLeaf = WalkBSP (map.modelLump.models [1].node);


		if (!lockpvs)
		{
			int pvs = WalkBSP();
			if (pvs != lastpvs)
			{
				lastpvs = pvs;

				RenderPVS(pvs);

			}
		}
		if (RenderAllFaces)
			RenderPVS(0);
		// Pressing A will toggle locking the PVS
		if (Input.GetKeyDown(KeyCode.Z))
		{
			lockpvs = !lockpvs;
			Debug.Log("PVS lock: " + lockpvs.ToString());
		}

	}


	// This will retrieve and render the PVS for the leaf you pass it
	// Must run every frame/however often you want to update the pvs.
	// you can cease calling this to "lock" the pvs.
	private void RenderPVS(int leaf)
	{
		//Debug.Log("Rendering PVS for Leaf: " + leaf.ToString());
		for (int i = 0; i < leafRoots.Length; i++)
		{
			foreach (GameObject go in leafRoots[i])
			{
				go.GetComponent<Renderer>().enabled = false;
			}
		}

		if (leaf == 0)
		{
			for (int i = 0; i < leafRoots.Length; i++)
			{
				foreach (GameObject go in leafRoots[i])
				{

					go.GetComponent<Renderer>().enabled = true;
					if (go.GetComponent<Renderer>().material.mainTexture.name == "sky")
					{
						go.GetComponent<Renderer>().enabled = false;
					}
				}
			}
			return;
		}

		for (int j = 0; j < map.leafLump.leafs[leaf].pvs.Length; j++)
		{
			if (map.leafLump.leafs[leaf].pvs[j] == true)
			{
				foreach (GameObject go in leafRoots[j + 1])
				{ //+1 because leaf 0 is bullshit, trust me
					go.GetComponent<Renderer>().enabled = true;

					if (go.GetComponent<Renderer>().material.mainTexture.name == "sky")
					{
						go.GetComponent<Renderer>().enabled = false;
					}


				}
			}
		}

	}


	#region BSP Lookup
	// Tests a node's plane, and returns the child to be tested next, or the leaf the player is in.
	private int BSPlookup(int node)
	{
		int child;
		if (!map.planeLump.planes[map.nodeLump.nodes[node].planeNum].plane.GetSide(player.position))
		{
			child = map.nodeLump.nodes[node].children[0];
		}
		else
		{
			child = map.nodeLump.nodes[node].children[1];
		}
		return child;
	}

	// This uses the bsp lookup method to find the leaf
	// the camera is in, and returns it.
	// Calling this (just one time) will give you the leaf the player is in.
	private int WalkBSP(int headnode = 0)
	{
		int child = BSPlookup(headnode);
		while (child >= 0)
		{
			child = BSPlookup(child);
		}

		child = -(child + 1);
		return child;
	}



	#endregion

	#region Object array generation
	void GenerateVisArrays()
	{


		leafRoots = new GameObject[map.leafLump.numLeafs][];
		for (int i = 0; i < map.leafLump.numLeafs; i++)
		{
			leafRoots[i] = new GameObject[map.leafLump.leafs[i].NumMarkSurfaces];
		}

	}

	void GenerateVisObjects()
	{
		for (int i = 0; i < map.leafLump.numLeafs; i++)
		{
			for (int j = 0; j < map.leafLump.leafs[i].NumMarkSurfaces; j++)
			{
				leafRoots[i][j] = GenerateFaceObject(map.facesLump.faces[map.markSurfacesLump.markSurfaces[map.leafLump.leafs[i].FirstMarkSurface + j]]);
				faceCount++;
			}
		}
	}
	#endregion

	#region Face Object Generation

	GameObject GenerateFaceObject(BSPFace currentFace)
    {


        BSPTexInfo texInfo = map.texinfoLump.texinfo[currentFace.texinfo_id];
        Face_t tface = new Face_t();
        tface.Texture = (int)texInfo.miptex;
        tface.Flags = (int)texInfo.flags;
        GameObject faceObject = new GameObject("BSPface " + faceCount.ToString());//Used for debugging
        faceObject.transform.parent = gameObject.transform;
        Mesh faceMesh = new Mesh();
        faceMesh.name = "BSPmesh";
        float[] mins = { 999999f, 999999f };
        float[] maxs = { -999999, -999999 };

        CalcSurfaceExtent(currentFace, ref mins,ref maxs,  texInfo);

        int lw = 0, lh = 0;
        // Compute the lightmap if the face is not a special texture
        if ((texInfo.flags & 1) == 0)
        {
            ComputeLightmap(currentFace, tface, mins, maxs, ref lh, ref lw);
        }

        // Calculate the inverse of the texture width and height

        float iis = 1.0f / (float)map.miptexLump[texInfo.miptex].width;
        float iit = 1.0f / (float)map.miptexLump[texInfo.miptex].height;
        // List to store vertex data

        List<float[]> listOfVertex = new List<float[]>();

        // Loop through each edge of the face
        for (int g = 0; g < currentFace.numberEdges; g++)
        {
            float[] v = new float[7];

            //================================================================================================
            //
            //
            //  I swizzle the vertex here to match unity's coordinate system
            //  This should done while loading the vertex lump in BSP30map
            //
            // 
            //================================================================================================
            int eidx = map.edgeLump.SURFEDGES[currentFace.firstEdgeIndex + g];
            if (eidx < 0)
            {
                eidx = -eidx;
                BSPEdge e = map.edgeLump.edges[eidx];
                v[0] = -map.vertLump.verts[e.vert2].x;
                v[1] = map.vertLump.verts[e.vert2].z;
                v[2] = -map.vertLump.verts[e.vert2].y;
            }
            else
            {
                BSPEdge e = map.edgeLump.edges[eidx];
                v[0] = -map.vertLump.verts[e.vert1].x;
                v[1] = map.vertLump.verts[e.vert1].z;
                v[2] = -map.vertLump.verts[e.vert1].y;
            }
            // Calculate texture coordinates
           


            float s = Vector3.Dot(new Vector3(v[0], v[1], v[2]), texInfo.vec3st[0]) + texInfo.offst[0];
            float t = Vector3.Dot(new Vector3(v[0], v[1], v[2]), texInfo.vec3st[1]) + texInfo.offst[1];

            v[3] = s * iis;  // is is the inverse of the texture width
            v[4] = t * iit;  // it is the inverse of the texture height



            // Compute lightmap coordinates if the face is not a special texture

            if ((tface.Flags & 1) == 0)
            {


                // compute lightmap coords
                float mid_poly_s = (mins[0] + maxs[0]) / 2.0f;
                float mid_poly_t = (mins[1] + maxs[1]) / 2.0f;
                float mid_tex_s = (float)lw / 2.0f;
                float mid_tex_t = (float)lh / 2.0f;
                float ls = mid_tex_s + (s - mid_poly_s) / 16.0f;
                float lt = mid_tex_t + (t - mid_poly_t) / 16.0f;
                ls = ls / (float)lw;
                lt = lt / (float)lh;

                v[5] = ls;
                v[6] = lt;

                listOfVertex.Add(v);
            }

        }
        // Create triangles

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> uvs2 = new List<Vector2>();

        // Add vertex data to lists

        foreach (float[] v in listOfVertex)
        {
            vertices.Add(new Vector3(v[0], v[1], v[2]));
            uvs.Add(new Vector2(v[3], v[4]));
            uvs2.Add(new Vector2(v[5], v[6]));
        }
        // Create triangle indices


        int[] tris = new int[(currentFace.numberEdges - 2) * 3];
        if (vertices.Count == 0) return new GameObject("Face");




        int tristep = 1;
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            tris[tristep - 1] = 0;
            tris[tristep] = i;
            tris[tristep + 1] = i + 1;
            tristep += 3;
        }
        // Create the mesh and set its vertices, triangles, and UVs

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();



        mesh.triangles = tris;
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, uvs2);
        mesh.RecalculateNormals();

        // Create the GameObject
       // GameObject faceObject = new GameObject("Face");
        MeshFilter meshFilter = faceObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = faceObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;

        // Set the material
        Material material = new Material(Shader.Find("Legacy Shaders/Lightmapped/Diffuse"));
        if (map.miptexLump[texInfo.miptex].texture == null)
        {
            material.mainTexture = missingtexture;
        }
        else
        {
            material.mainTexture = map.miptexLump[texInfo.miptex].texture;

        }
        material.mainTexture.filterMode = filterMode;
        meshRenderer.material = material;



        tface.Lightmap.filterMode = filterMode;
        tface.Lightmap.wrapMode = TextureWrapMode.Clamp;
        material.SetTexture("_LightMap", tface.Lightmap);
        return faceObject;

    }

    private void CalcSurfaceExtent(BSPFace currentFace,ref float[] mins, ref float[] maxs, BSPTexInfo texInfo)
    {
        float val;
        int  j, e = 0;






        Vector3 vert;
        for (int edgestep = 0; edgestep < currentFace.numberEdges; edgestep++)

        {



            e = map.edgeLump.SURFEDGES[currentFace.firstEdgeIndex + edgestep];
            if (e >= 0)
             
             vert = map.vertLump.ConvertScaleVertex(map.vertLump.verts[map.edgeLump.edges[e].vert1]);

            else

                vert = map.vertLump.ConvertScaleVertex(map.vertLump.verts[map.edgeLump.edges[-e].vert2]);
           



            for (j = 0; j < 2; j++)
            {
                val = vert.x * texInfo.vec3st[j].x +
                      vert.y * texInfo.vec3st[j].y +
                      vert.z * texInfo.vec3st[j].z +
                      texInfo.offst[j];
                 if (val < mins[j])
                    mins[j] = val;
                if (val > maxs[j])
                    maxs[j] = val;
            }
        }

       
    }
    #endregion




    void ComputeLightmap(BSPFace dface, Face_t face, float[] mins, float[] maxs, ref int lh, ref int lw)
    {
        int c, i;
        int width, height;
        /// <summary>
        /// This hold the bytes of the lightmap copied from the lightmap data lump
        /// </summary>
        byte[] data;

        // compute lightmap size
        int[] size = new int[2];
        for (c = 0; c < 2; c++)
        {
            float tmin = Mathf.Floor(mins[c] / 16.0f);

            float tmax = Mathf.Ceil(maxs[c] / 16.0f);

            size[c] = (int)(tmax - tmin);
        }

        width = size[0] + 1;
        height = size[1] + 1;

        lw = width;
        lh = height;

        int lsz = width * height * 3;  // RGB buffer size

        // generate lightmaps texture
        for (c = 0; c < 1; c++)
        {
            if (dface.Styles[c] == 255)
                break;

            dface.Styles[c] = dface.Styles[c];

            data = new byte[lsz];

            Array.Copy(map.lightlump, dface.lightmapOffset + (lsz * c), data, 0, lsz);

            float f, light_adjust;
            int inf;

            light_adjust = 1.0f;

            // scale lightmap value...
            for (i = 0; i < lsz; i++)
            {
                f = Mathf.Pow((data[i] + 1) / 256.0f, light_adjust);
                inf = (int)(f * 255.0f + 0.5f);

                if (inf < 0)
                    inf = 0;
                if (inf > 255)
                    inf = 255;
                data[i] = (byte)inf;
            }

            face.Lightmaps[c] = GenerateLightmapTexture(data, width, height);

            data = null;
        }

        face.Lightmap = face.Lightmaps[0];





    }



    // < summary >
    // Generates a Texture2D from raw lightmap data stored as continuous RGB triples.
    // </summary>
    // <param name="lightmapData">The raw byte array containing RGB lightmap data.</param>
    // <param name="width">The width of the lightmap texture.</param>
    // <param name="height">The height of the lightmap texture.</param>
    // <returns>A Texture2D containing the lightmap.</returns>
    public Texture2D GenerateLightmapTexture(byte[] lightmapData, int width, int height)
{
    // Validate inputs
    if (lightmapData == null || lightmapData.Length == 0)
    {
        Debug.LogError("Lightmap data is null or empty.");
        return null;
    }

    int totalBytesNeeded = width * height * 3; // RGB format (3 bytes per pixel)

    // Check if there are enough bytes in the lightmap data
    if (lightmapData.Length < totalBytesNeeded)
    {
        Debug.LogError($"Not enough lightmap data. Expected {totalBytesNeeded} bytes, but found {lightmapData.Length} bytes.");
        return null;
    }

    // Create the texture
    Texture2D lightmapTexture = new Texture2D(width, height, TextureFormat.RGB24, false);

    // Load the raw texture data directly into the texture
    lightmapTexture.LoadRawTextureData(lightmapData);

    // Apply the changes
    lightmapTexture.Apply();

    return lightmapTexture;
}


}