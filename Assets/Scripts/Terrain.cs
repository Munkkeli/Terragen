using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LibNoise.Unity.Generator;
using System;

public class Terrain {

    public Field Base;
    public float BaseHeight = 0;
    public Dictionary<string, Area> Areas = new Dictionary<string, Area>();

    public delegate float Calculate (float a, float b);

    public static class Math {
        public static float Times(float a, float b) { return a * b; }
        public static float Divide(float a, float b) { return a / b; }
        public static float Add(float a, float b) { return a + b; }
        public static float Subtract(float a, float b) { return a - b; }
        public static float Power(float a, float b) { return Mathf.Pow(a, b); }
    }

    public enum Generator {
        Vonoroi, Perlin, Billow
    }

    public static class Generators {
        public static Voronoi Vonoroi(double s, float o) { return new Voronoi(s, 1.5, UnityEngine.Random.seed, true); }
        public static Perlin Perlin(double s, float o) { return new Perlin(s, 1.5, 0.6, 20, UnityEngine.Random.seed, LibNoise.Unity.QualityMode.Low); }
        public static Billow Billow(double s, float o) { return new Billow(s, 1.5, 0.6, 20, UnityEngine.Random.seed, LibNoise.Unity.QualityMode.Low); }
    }

    public class Noise {
        public float scale, power, offset;
        public Calculate calc;
        public Action<float, float> function;
        public Generator generator;
        public Voronoi vonoroi;
        public Perlin perlin;
        public Billow billow;

        public Noise(float s, float p, float o, Calculate c, Generator g) {
            scale = s; power = p; offset = o; calc = c;
            if (g == Generator.Vonoroi)
                vonoroi = Generators.Vonoroi(0.1, offset);
            else if (g == Generator.Perlin)
                perlin = Generators.Perlin(0.1, offset);
            else if (g == Generator.Billow)
                billow = Generators.Billow(0.1, offset);
            generator = g;
        }

        public float Get(int x, int y) {
            float v = 0;
            if (generator == Generator.Vonoroi)
                v = (float)vonoroi.GetValue((x + offset) / scale, 0, (y + offset) / scale) * power;
            else if (generator == Generator.Perlin)
                v = (float)perlin.GetValue((x + offset) / scale, 0, (y + offset) / scale) * power;
            else if (generator == Generator.Billow)
                v = (float)billow.GetValue((x + offset) / scale, 0, (y + offset) / scale) * power;
            return v;
        }

        public float Calc(int x, int y, float v) {
            return calc(this.Get(x, y), v);
        }
    }

    public class Field {
        public List<Noise> noises = new List<Noise>();

        public Field(Noise[] n = null) {
            if (n != null)
                noises.AddRange(n);
        }

        public float Get(int x, int y) {
            float v = 1;
            for (int i = 0; i < noises.Count; i++) {
                v = noises[i].Calc(x, y, v);
            }
            return v;
        }

        public void Add(Noise n) {
            noises.Add(n);
        }
    }

    public class Mask {
        public float start, fade;
        public Field noises;

        public Mask(float s, float f, Field n) {
            start = s; fade = f; noises = n;
        }

        public float Get(int x, int y) {
            float v = noises.Get(x, y);
            /*v = Mathf.Clamp(Mathf.Clamp(v, start, start + fade) - start, 0, fade);
            v *= ((v - start) / fade);*/
            return v;
        }
    }

    public class Area {
        public Field noises;
        public Mask mask;

        public Area(Field n, Mask m) {
            noises = n; mask = m;
        }

        public float Get(int x, int y) {
            return mask.Get(x, y) * noises.Get(x, y);
        }
    }

    public Terrain() {
        
    }

    public float Get(int x, int y) {
        float v = BaseHeight;
        if(Base != null)
            v += Base.Get(x, y);
        foreach(KeyValuePair<string, Area> area in Areas) {
            v += area.Value.Get(x, y);
        }
        return v;
    }

    public void SetBase(float h, Field n = null) {
        BaseHeight = h;
        Base = n;
    }

    public void AddArea(string n, Area a) {
        Areas.Add(n, a);
    }
}
