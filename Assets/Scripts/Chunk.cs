using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour {

    public int X, Y, Size, Scale, SubDepth;
    public int[] Offsets;
    public float[,] Points;
    public GameObject Obj;
    public Terrain Terra;
    public Material[] Materials;

    public Dictionary<Vector2, Chunk> Subs = new Dictionary<Vector2, Chunk>();
    public int MaxSubDepth = 5;

    private bool updated = false;

    void Update () {
        /*int size = World.WORLD.width / 2;
        if (SubDepth == 1) { size = World.WORLD.width / 4; }
        else if (SubDepth == 2) { size = World.WORLD.width / 6; }
        else if (SubDepth == 3) { size = World.WORLD.width / 8; }
        else if (SubDepth == 4) { size = World.WORLD.width / 12; }

        if (SubDepth < MaxSubDepth && Vector3.Distance(transform.position, World.WORLD.player.position) < size) {
            if (!updated) {
                World.WORLD.updates.Add(this);
                updated = true;
            }
        }
        else if (updated) {
            foreach (Transform child in transform) {
                StartCoroutine(World.RemoveChunk(child.gameObject.GetComponent<Chunk>()));
            }
            renderer.enabled = true;
            if (collider) collider.enabled = true;
            updated = false;
        }*/
    }

    public Chunk Create(int x, int y, int size, int scale, int subDepth, GameObject obj, Terrain terra, Material[] materials) {
        X = x; Y = y; Size = size; Scale = scale; SubDepth = subDepth; Obj = obj;
        Points = new float[Size + 1, Size + 1];
        Offsets = new int[] { 234, 3255, 2342, 467 };
        Terra = terra;
        Materials = materials;
        return this;
    }

    public bool CreateData() {
        for (int x = 0; x < Size + 1; x++) {
            for (int y = 0; y < Size + 1; y++) {
                Points[x, y] = Terra.Get(X + (x * Scale), Y + (y * Scale));
            }
        }

        return true;
    }

    public bool CreateVisuals() {
        Mesh mesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int>[] tris = new List<int>[] {
            new List<int>(),
            new List<int>()
        };
        List<Vector2> uvs = new List<Vector2>();
        List<Color32> colors = new List<Color32>();

        for (int x = 0; x < Size; x++) {
            for (int y = 0; y < Size; y++) {
                CreateTile(x, y, verts, tris[0], uvs, colors);
                CreateWater(x, y, verts, tris[1], uvs, colors);

                /*if (x == 0 || y == 0)
                    CreateSide(x, y, verts, tris[0], uvs, colors, 0);*/
            }
        }

        mesh.vertices = verts.ToArray();
        mesh.subMeshCount = (tris[1].Count > 0) ? 2 : 1;
        if (tris[1].Count <= 0) GetComponent<Renderer>().sharedMaterials = new Material[] { GetComponent<Renderer>().sharedMaterials[0] };
        mesh.SetTriangles(tris[0].ToArray(), 0);
        if (tris[1].Count > 0) mesh.SetTriangles(tris[1].ToArray(), 1);
        mesh.uv = uvs.ToArray();
        mesh.colors32 = colors.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        Obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        Obj.AddComponent<MeshCollider>().sharedMesh = mesh;

        return true;
    }

    public IEnumerator CreateSubs() {
        try {
            int size = 1, scale = 128, width = 2, posScale = 128;
            if (SubDepth == 0) { posScale = 64; scale = 64; width = 2; }
            else if (SubDepth == 1) { posScale = 32; scale = 32; width = 2; }
            else if (SubDepth == 2) { size = 2; posScale = 16; scale = 16; width = 1; }
            else if (SubDepth == 3) { size = 4; posScale = 16; scale = 4; width = 2; }
            else if (SubDepth == 4) { size = 8; posScale = 8; scale = 2; width = 1; }

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < width; y++) {
                    GameObject obj = new GameObject("SubChunk[" + x + "," + y + "]");
                    obj.isStatic = true;
                    obj.transform.parent = transform;
                    obj.transform.localPosition = new Vector3((x * posScale), 0, (y * posScale));
                    Chunk chunk = obj.AddComponent<Chunk>().Create(X + (x * posScale), Y + (y * posScale), size, scale, SubDepth + 1, obj, Terra, Materials);
                    obj.GetComponent<Renderer>().sharedMaterials = Materials;
                    chunk.CreateData();
                    chunk.CreateVisuals();
                }
            }

            GetComponent<Renderer>().enabled = false;
            if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
        }
        catch { };

        yield return true;
    }

    public float GetPoint(int x, int y) {
        if (x > 0 && y > 0 && x < Size && y < Size)
            return Points[x, y];
        else
            return Terra.Get(X + (x * Scale), Y + (y * Scale));
    }

    public Color32 GetColor(int x, int y) {
        Color32 value = new Color32(109, (byte)Random.Range(150, 156), 0, 1);

        float point = Points[x, y];
        if (point < 20 + Random.Range(0, 4)) {
            value = new Color32(128, 128, (byte)Random.Range(100, 128), 1);
        }
        else if (point < 28 + Random.Range(0, 2)) {
            if (Random.Range(0, 100) == 1)
                value = new Color32(107, (byte)Random.Range(100, 109), 107, 1);
            else
                value = new Color32(210, 196, (byte)Random.Range(130, 139), 1);
        }
        else if ((
                Mathf.Abs(point - GetPoint(x - 2, y))
            + Mathf.Abs(point - GetPoint(x + 2, y))
            + Mathf.Abs(point - GetPoint(x, y - 2))
            + Mathf.Abs(point - GetPoint(x, y + 2))
            + Mathf.Abs(point - GetPoint(x - 1, y))
            + Mathf.Abs(point - GetPoint(x + 1, y))
            + Mathf.Abs(point - GetPoint(x, y - 1))
            + Mathf.Abs(point - GetPoint(x, y + 1))
        ) / 8 > 0.7f * Mathf.Clamp(Scale / 2, 1, 2052))
            value = new Color32(107, (byte)Random.Range(100, 109), 107, 1);

        return value;
    }

    public void CreateTile(int x, int y, List<Vector3> verts, List<int> tris, List<Vector2> uvs, List<Color32> colors) {
        int i = verts.Count;

        int pX = x * Scale, pY = y * Scale;

        if (!(Points[x, y] > Points[x + 1, y] || Points[x + 1, y + 1] > Points[x, y + 1] - 2)) {
            verts.AddRange(new Vector3[] {
                new Vector3(pX, Points[x, y], pY),
                new Vector3(pX + Scale, Points[x + 1, y], pY),
                new Vector3(pX + Scale, Points[x + 1, y + 1], pY + Scale),
                new Vector3(pX, Points[x, y], pY),
                new Vector3(pX, Points[x, y + 1], pY + Scale),
                new Vector3(pX + Scale, Points[x + 1, y + 1], pY + Scale)
            });
        }
        else {
            verts.AddRange(new Vector3[] {
                new Vector3(pX, Points[x, y + 1], pY + Scale),
                new Vector3(pX, Points[x, y], pY),
                new Vector3(pX + Scale, Points[x + 1, y], pY),
                new Vector3(pX, Points[x, y + 1], pY + Scale),
                new Vector3(pX + Scale, Points[x + 1, y + 1], pY + Scale),
                new Vector3(pX + Scale, Points[x + 1, y], pY)
            });
        }

        tris.AddRange(new int[] {
            i + 1, i + 0, i + 2,
            i + 4, i + 5, i + 3
        });

        uvs.AddRange(new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        });

        Color32 color = GetColor(x, y);
        colors.AddRange(new Color32[] {
            color, color, color,
            color, color, color
        });

    }

    public void CreateWater(int x, int y, List<Vector3> verts, List<int> tris, List<Vector2> uvs, List<Color32> colors) {
        int i = verts.Count;

        int pX = x * Scale, pY = y * Scale;

        int sea = 28;

        if (Points[x, y] < sea || Points[x + 1, y] < sea || Points[x, y + 1] < sea || Points[x + 1, y + 1] < sea) {
            Color32 water = new Color32(0, 148, 194, 1),
                foam = new Color32(255, 255, 255, 1);

            Color32 first, second;

            if (!(Points[x, y] > sea - 2 || Points[x + 1, y + 1] > sea - 2) && (Points[x + 1, y] > sea - 2 || Points[x, y + 1] > sea - 2)) {
                verts.AddRange(new Vector3[] {
                    new Vector3(pX, sea, pY),
                    new Vector3(pX + Scale, sea, pY),
                    new Vector3(pX + Scale, sea, pY + Scale),
                    new Vector3(pX, sea, pY),
                    new Vector3(pX, sea, pY + Scale),
                    new Vector3(pX + Scale, sea, pY + Scale)
                });

                first = (Points[x, y] > sea - 2 || Points[x + 1, y] > sea - 2 || Points[x + 1, y + 1] > sea - 2) ? foam : water;
                second = (Points[x, y] > sea - 2 || Points[x, y + 1] > sea - 2 || Points[x + 1, y + 1] > sea - 2) ? foam : water;
            }
            else {
                verts.AddRange(new Vector3[] {
                    new Vector3(pX, sea, pY + Scale),
                    new Vector3(pX, sea, pY),
                    new Vector3(pX + Scale, sea, pY),
                    new Vector3(pX, sea, pY + Scale),
                    new Vector3(pX + Scale, sea, pY + Scale),
                    new Vector3(pX + Scale, sea, pY)
                });

                first = (Points[x, y + 1] > sea - 2 || Points[x, y] > sea - 2 || Points[x + 1, y] > sea - 2) ? foam : water;
                second = (Points[x, y + 1] > sea - 2 || Points[x + 1, y + 1] > sea - 2 || Points[x + 1, y] > sea - 2) ? foam : water;
            }

            tris.AddRange(new int[] {
                i + 2, i + 1, i + 0,
                i + 3, i + 4, i + 5
            });

            uvs.AddRange(new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            });

            colors.AddRange(new Color32[] {
                first, first, first,
                second, second, second
            });
        }
    }

    public void CreateSide(int x, int y, List<Vector3> verts, List<int> tris, List<Vector2> uvs, List<Color32> colors, int side) {
        int i = verts.Count;

        int pX = x * Scale, pY = y * Scale;

        if (side == 0) {
            verts.AddRange(new Vector3[] {
                new Vector3(pX, Points[x, y], pY),
                new Vector3(pX + Scale, Points[x + 1, y] - Scale, pY),
                new Vector3(pX + Scale, Points[x + 1, y], pY),
                new Vector3(pX, Points[x, y] - Scale, pY),
                new Vector3(pX, Points[x, y], pY),
                new Vector3(pX + Scale, Points[x + 1, y] - Scale, pY)
            });
        }
        else if (side == 1) {
            verts.AddRange(new Vector3[] {
                new Vector3(pX, Points[x, y], pY),
                new Vector3(pX + Scale, Points[x + 1, y] - Scale, pY),
                new Vector3(pX + Scale, Points[x + 1, y], pY),
                new Vector3(pX, Points[x, y] - Scale, pY),
                new Vector3(pX, Points[x, y], pY),
                new Vector3(pX + Scale, Points[x + 1, y] - Scale, pY)
            });
        }

        tris.AddRange(new int[] {
            i + 1, i + 0, i + 2,
            i + 4, i + 5, i + 3
        });

        uvs.AddRange(new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        });

        Color32 color = GetColor(x, y);
        colors.AddRange(new Color32[] {
            color, color, color,
            color, color, color
        });

    }
}