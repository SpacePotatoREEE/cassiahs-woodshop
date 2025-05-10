// ──────────────────────────────────────────────────────────────
//  GPUParticleSystem.cs – GPU particle field (lifetime & size ranges)
//  Unity 6 · URP
//  v3.0 – life [min,max], size [min,max], Live Forever toggle
// ──────────────────────────────────────────────────────────────
using System;
using System.Buffers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class GPUParticleSystem : MonoBehaviour
{
    // ───────── Inspector ─────────
    [Header("General")]
    [SerializeField] int   maxParticles  = 200_000;

    [Header("Lifetime (seconds)")]
    [SerializeField] float lifeMin = 10f;
    [SerializeField] float lifeMax = 20f;

    [Header("Size (multiplier of _BaseSize in material)")]
    [SerializeField] float sizeMin = 0.6f;
    [SerializeField] float sizeMax = 1.4f;

    [Header("Spawn")]
    [SerializeField] Vector3 boxSize        = new(80, 1, 80);
    [SerializeField] float   spawnPerSecond = 20_000f;
    [SerializeField] bool    liveForever    = false;

    [Header("Motion")]
    [SerializeField] Vector3 constantAcceleration = new(0, -0.4f, 0);
    [SerializeField, Range(0,1)] float drag = 0.985f;

    [Header("Rendering")]
    [SerializeField] Mesh  billboardMesh;
    [SerializeField] Material renderMaterial;
    [SerializeField] bool  colourBySpeed = true;
    [SerializeField] Gradient speedGradient = null;

    [Header("Compute")]
    [SerializeField] ComputeShader particleCompute;

    // ───────── Buffers ─────────
    ComputeBuffer particleBuffer;
    ComputeBuffer argsBuffer;
    ComputeBuffer volumePosRad;
    ComputeBuffer volumeForce;
    
    MaterialPropertyBlock mpb;

    const int MAX_VOLUMES = 32;

    // ───────── IDs ─────────
    static readonly int ID_Particles     = Shader.PropertyToID("_Particles");
    static readonly int ID_Accel         = Shader.PropertyToID("_Accel");
    static readonly int ID_Drag          = Shader.PropertyToID("_Drag");
    static readonly int ID_DeltaTime     = Shader.PropertyToID("_DeltaTime");
    static readonly int ID_SpawnCount    = Shader.PropertyToID("_SpawnCount");
    static readonly int ID_BoxCenter     = Shader.PropertyToID("_BoxCenter");
    static readonly int ID_BoxExtents    = Shader.PropertyToID("_BoxExtents");
    static readonly int ID_Volumes       = Shader.PropertyToID("_Volumes");
    static readonly int ID_Forces        = Shader.PropertyToID("_Forces");
    static readonly int ID_VolumeCount   = Shader.PropertyToID("_VolumeCount");
    static readonly int ID_MaxParticles  = Shader.PropertyToID("_MaxParticles");
    static readonly int ID_FrameSeed     = Shader.PropertyToID("_FrameSeed");
    static readonly int ID_SpawnOffset   = Shader.PropertyToID("_SpawnOffset");
    static readonly int ID_LifeMin       = Shader.PropertyToID("_LifeMin");
    static readonly int ID_LifeMax       = Shader.PropertyToID("_LifeMax");
    static readonly int ID_SizeMin       = Shader.PropertyToID("_SizeMin");
    static readonly int ID_SizeMax       = Shader.PropertyToID("_SizeMax");
    static readonly int ID_LiveForever   = Shader.PropertyToID("_LiveForever");

    int      kernelSimulate;
    Camera   mainCam;
    Bounds   alwaysVisibleBounds;
    uint     spawnCursor = 0;
    bool     firstFillDone = false;

    void OnEnable()
    {
        if (particleCompute == null)
            particleCompute = Resources.Load<ComputeShader>("ParticlesSim");

        kernelSimulate = particleCompute.FindKernel("Simulate");

        // pos(3)+vel(3)+life(1)+size(1) = 8 floats
        particleBuffer = new ComputeBuffer(maxParticles, sizeof(float) * 8);

        argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] { billboardMesh ? billboardMesh.GetIndexCount(0) : 6u, 0, 0, 0, 0 });

        volumePosRad = new ComputeBuffer(MAX_VOLUMES, sizeof(float) * 4);
        volumeForce  = new ComputeBuffer(MAX_VOLUMES, sizeof(float) * 4);
        particleCompute.SetBuffer(kernelSimulate, ID_Volumes, volumePosRad);
        particleCompute.SetBuffer(kernelSimulate, ID_Forces,  volumeForce);

        mainCam = Camera.main;
        alwaysVisibleBounds = new Bounds(Vector3.zero, Vector3.one * 9e9f);

        if (!billboardMesh) billboardMesh = BuildQuad();
        renderMaterial.enableInstancing = true;
        renderMaterial.SetFloat("_LifeMax", lifeMax);
        
        mpb = new MaterialPropertyBlock();
        if (colourBySpeed && speedGradient != null)
        {
            var gradTex = BakeGradient(speedGradient);   // already HDR-compatible
            mpb.SetTexture("_SpeedTex", gradTex);
        }
    }

    void OnDisable()
    {
        particleBuffer?.Release();
        argsBuffer?.Release();
        volumePosRad?.Release();
        volumeForce?.Release();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;

        // ── gather external volumes ──
        var vols = GPUParticleVolume.ActiveVolumes;
        int volCount = Mathf.Min(vols.Count, MAX_VOLUMES);

        var posRadArr = ArrayPool<Vector4>.Shared.Rent(MAX_VOLUMES);
        var forceArr  = ArrayPool<Vector4>.Shared.Rent(MAX_VOLUMES);

        for (int i = 0; i < volCount; ++i)
        {
            var v = vols[i];
            var p = v.transform.position;
            posRadArr[i] = new Vector4(p.x, p.y, p.z, v.Radius);

            var f = v.ForceVector;
            forceArr[i]  = new Vector4(f.x, f.y, f.z, 0);
        }
        volumePosRad.SetData(posRadArr);
        volumeForce .SetData(forceArr);
        ArrayPool<Vector4>.Shared.Return(posRadArr);
        ArrayPool<Vector4>.Shared.Return(forceArr);
        particleCompute.SetInt(ID_VolumeCount, volCount);

        // ── spawn counts ──
        int spawn;
        if (liveForever)
        {
            spawn = firstFillDone ? 0 : maxParticles;
            firstFillDone = true;
        }
        else
        {
            spawn = Mathf.CeilToInt(spawnPerSecond * Time.deltaTime);
            spawnCursor = (spawnCursor + (uint)spawn) % (uint)maxParticles;
            particleCompute.SetInt(ID_SpawnOffset, (int)spawnCursor);
        }

        // ── push uniforms ──
        particleCompute.SetInt   (ID_SpawnCount, spawn);
        particleCompute.SetFloat (ID_DeltaTime , Time.deltaTime);
        particleCompute.SetVector(ID_Accel     , constantAcceleration);
        particleCompute.SetFloat (ID_Drag      , drag);
        particleCompute.SetVector(ID_BoxCenter , transform.position);
        particleCompute.SetVector(ID_BoxExtents, boxSize * 0.5f);

        particleCompute.SetFloat(ID_LifeMin,    lifeMin);
        particleCompute.SetFloat(ID_LifeMax,    lifeMax);
        particleCompute.SetFloat(ID_SizeMin,    sizeMin);
        particleCompute.SetFloat(ID_SizeMax,    sizeMax);
        particleCompute.SetInt  (ID_LiveForever, liveForever ? 1 : 0);

        particleCompute.SetInt  (ID_MaxParticles, maxParticles);
        particleCompute.SetInt  (ID_FrameSeed, Time.frameCount * 9781);

        particleCompute.SetBuffer(kernelSimulate, ID_Particles,   particleBuffer);
        particleCompute.SetBuffer(kernelSimulate, "indirectArgs", argsBuffer);

        particleCompute.GetKernelThreadGroupSizes(kernelSimulate, out uint tg, out _, out _);
        int groups = Mathf.CeilToInt(maxParticles / (float)tg);
        particleCompute.Dispatch(kernelSimulate, groups, 1, 1);

        // ── draw ──
        renderMaterial.SetBuffer(ID_Particles, particleBuffer);
        
        if (mpb == null && colourBySpeed && speedGradient != null)
        {
            // hot-reload support when you tweak the gradient during Play
            var gradTex = BakeGradient(speedGradient);
            mpb = new MaterialPropertyBlock();
            mpb.SetTexture("_SpeedTex", gradTex);
        }
        
        Graphics.DrawMeshInstancedIndirect(
            billboardMesh, 0, renderMaterial,
            alwaysVisibleBounds, argsBuffer, 0,               // args
            mpb,                                             // ← NEW
            ShadowCastingMode.Off, false,
            gameObject.layer, mainCam);
        
        renderMaterial.SetFloat("_LifeMax", lifeMax);
    }

    // ───────── helpers ─────────
    static Mesh BuildQuad()
    {
        var m = new Mesh();
        m.vertices = new[]{
            new Vector3(-.5f,-.5f,0), new Vector3(-.5f,.5f,0),
            new Vector3(.5f,.5f,0),  new Vector3(.5f,-.5f,0) };
        m.uv        = new[]{ Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        m.triangles = new[]{ 0,1,2, 2,3,0 };
        m.RecalculateBounds();
        return m;
    }
//  ──────────────────────────────────────────────────────────────
//  GPUParticleSystem.cs  (excerpt showing only the changed method)
//  v3.1 – speed-gradient baked as HDR (RGBAHalf) texture
//  ──────────────────────────────────────────────────────────────
    /* … the rest of the file is identical to v3.0 … */

    /// <summary>Bake the user-supplied Gradient into a 1-D lookup texture.
    /// Now uses RGBAHalf so HDR colours (>1) are preserved.</summary>
    Texture2D BakeGradient(Gradient g)
    {
        const int W = 128;

        // RGBAHalf  = 16-bit float per channel, linear colour space
        var tex = new Texture2D(W, 1, TextureFormat.RGBAHalf, /*mip*/false, /*linear*/ true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int x = 0; x < W; ++x)
        {
            // Gradient already gives linear-space colours; just write them.
            tex.SetPixel(x, 0, g.Evaluate(x / (float)(W - 1)));
        }
        tex.Apply();
        return tex;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()                  // draws only when GO is selected
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, boxSize);   // spawn box
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.5f);        // exact box centre
    }
#endif

}

    

