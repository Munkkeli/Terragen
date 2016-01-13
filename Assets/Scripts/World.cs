using UnityEngine;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

public class World : MonoBehaviour {

    public static World WORLD;
    public static Dictionary<Vector2, Chunk> CHUNKS = new Dictionary<Vector2, Chunk>();
    public static int SEED = 1241356;

    public int width = 4, size = 8, speed = 1;
    public Material[] materials;
    public Transform player;
    public List<Chunk> updates = new List<Chunk>();

    Terrain terrain = new Terrain();

    private List<Vector2> create = new List<Vector2>();
    private List<Chunk> pool = new List<Chunk>();

	// Use this for initialization
	void Awake() {
        // Set the target framerate
        //Application.targetFrameRate = 120;

        Random.seed = SEED;

        // Set world to current world
        WORLD = this;

        // Set generator offsets
        /*Generator.OFFSET = new Vector3[] { 
            new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000),
            new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000),
            new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000),
        };*/

        float scale = 4f;

        terrain.SetBase(32, new Terrain.Field(new Terrain.Noise[] {
            new Terrain.Noise(scale, 5, 0, Terrain.Math.Add, Terrain.Generator.Perlin),
            new Terrain.Noise(scale * 10f, 25, 6445, Terrain.Math.Add, Terrain.Generator.Perlin),
            new Terrain.Noise(scale * 20f, 50, 9621, Terrain.Math.Add, Terrain.Generator.Perlin),
            new Terrain.Noise(scale * 80f, 120, 2135, Terrain.Math.Add, Terrain.Generator.Perlin)
        }));

        StartCoroutine(ChunkLoop());
        StartCoroutine(PoolLoop());
        StartCoroutine(UpdateLoop());
        StartCoroutine(ChunkRemoveLoop());
	}

    // Main chunk loop
    private IEnumerator ChunkLoop() {
        while (true) {
            // Enmpty the create array
            create.Clear();

            // Set the current pos to a variable
            Vector3 position = player.position;

            // Get all towers in the view radius
            for (float x = position.x - width; x < position.x + width; x += size) {
                for (float z = position.z - width; z < position.z + width; z += size) {
                    // Get the current chunk pos
                    Vector2 pos = new Vector2(x, z);
                    pos.x = Mathf.Floor(pos.x / (float)size) * size;
                    pos.y = Mathf.Floor(pos.y / (float)size) * size;

                    // Check if chunk needs to be created
                    if (Vector2.Distance(new Vector2(position.x, position.z), pos) > width) continue;
                    if (CHUNKS.ContainsKey(new Vector2(pos.x, pos.y))) continue;

                    // Create the chunk
                    create.Add(new Vector2(pos.x, pos.y));
                }
            }

            yield return true;

            // Sort the array
            create = create.OrderBy(c => Vector2.Distance(new Vector2(player.position.x, player.position.z), c)).ToList();

            // Create the needed towers
            for (int i = 0; i < create.Count; i++) {
                // Check if player has moved
                if (Vector2.Distance(new Vector2(position.x, position.z), new Vector2(player.position.x, player.position.z)) > size)
                    break;

                if (i == create.Count - 1) {
                    yield return StartCoroutine(CreateChunk((int)create[i].x, 0, (int)create[i].y));
                }
                else if (pool.Count > speed) {
                    yield return StartCoroutine(WaitForSlot(speed));
                    yield return StartCoroutine(CreateChunk((int)create[i].x, 0, (int)create[i].y));
                }
                else {
                    yield return StartCoroutine(CreateChunk((int)create[i].x, 0, (int)create[i].y));
                }
            }

            yield return new WaitForSeconds(0);
        }
    }

    // Main pool loop
    private IEnumerator PoolLoop() {
        while (true) {
            Chunk[] Chunks = pool.ToArray();
            foreach (Chunk chunk in Chunks) {
                chunk.CreateData();
                chunk.CreateVisuals();
                pool.Remove(chunk);
            }
            
            yield return true;
        }
    }

    // Main update loop
    private IEnumerator UpdateLoop() {
        while (true) {
            Chunk[] Chunks = updates.ToArray();
            foreach (Chunk chunk in Chunks) {
                if (chunk != null) {
                    yield return StartCoroutine(chunk.CreateSubs());
                    updates.Remove(chunk);
                }
            }

            yield return true;
        }
    }

    private IEnumerator WaitForSlot(int slot) {
        while (pool.Count > slot) {
            yield return new WaitForSeconds(0);
        }
    }

    // Chunk remove loop
    private IEnumerator ChunkRemoveLoop() {
        while (true) {
            // Set the current pos to a variable
            Vector3 position = player.position;

            // Remove unwanted chunks
            List<Chunk> remove = new List<Chunk>();
            foreach (KeyValuePair<Vector2, Chunk> chunk in CHUNKS) {
                if (Vector2.Distance(new Vector2(position.x, position.z), chunk.Key) > (width + size)) {
                    remove.Add(chunk.Value);
                }
            }
            for (int i = 0; i < remove.Count; i++) {
                StartCoroutine(RemoveChunk(remove[i]));
                CHUNKS.Remove(new Vector2(remove[i].X, remove[i].Y));
            }

            // Wait before looping again
            yield return new WaitForSeconds(0);
        }
    }

    // Create a tower
    private IEnumerator CreateChunk(int x, int y, int z) {
        GameObject obj = new GameObject("Chunk[" + x + "," + y + "]");
        obj.isStatic = true;
        obj.transform.position = new Vector3(x, y, z);
        Chunk chunk = obj.AddComponent<Chunk>().Create(x, z, 4, size / 4, 0, obj, terrain, materials);
        obj.GetComponent<Renderer>().sharedMaterials = materials;

        pool.Add(chunk);
        CHUNKS.Add(new Vector2(x, z), chunk);

        yield return true;
    }

    // Remove a chunk
    public static IEnumerator RemoveChunk(Chunk chunk) {
        if (chunk != null) {
            GameObject obj = chunk.gameObject;
            DestroyImmediate(obj.GetComponent<MeshFilter>().sharedMesh, true);
            Destroy(chunk);
            Destroy(obj);
        }

        yield return true;
    }

    // Start a coroutine
    public void NewCoroutine(IEnumerator coroutine) {
        StartCoroutine(coroutine);
    }
}
