#ifndef YPIPELINE_RANDOM_LIBRARY_INCLUDED
#define YPIPELINE_RANDOM_LIBRARY_INCLUDED

// [0, 1)
static const float2 k_Halton[65] = {
    float2(0.00000000, 0.00000000),
    float2(0.50000000, 0.33333333), float2(0.25000000, 0.66666667), float2(0.75000000, 0.11111111), float2(0.12500000, 0.44444444),
    float2(0.62500000, 0.77777778), float2(0.37500000, 0.22222222), float2(0.87500000, 0.55555556), float2(0.06250000, 0.88888889),
    float2(0.56250000, 0.03703704), float2(0.31250000, 0.37037037), float2(0.81250000, 0.70370370), float2(0.18750000, 0.14814815),
    float2(0.68750000, 0.48148148), float2(0.43750000, 0.81481481), float2(0.93750000, 0.25925926), float2(0.03125000, 0.59259259),
    float2(0.53125000, 0.92592593), float2(0.28125000, 0.07407407), float2(0.78125000, 0.40740741), float2(0.15625000, 0.74074074),
    float2(0.65625000, 0.18518519), float2(0.40625000, 0.51851852), float2(0.90625000, 0.85185185), float2(0.09375000, 0.29629630),
    float2(0.59375000, 0.62962963), float2(0.34375000, 0.96296296), float2(0.84375000, 0.01234568), float2(0.21875000, 0.34567901),
    float2(0.71875000, 0.67901235), float2(0.46875000, 0.12345679), float2(0.96875000, 0.45679012), float2(0.01562500, 0.79012346),
    float2(0.51562500, 0.23456790), float2(0.26562500, 0.56790123), float2(0.76562500, 0.90123457), float2(0.14062500, 0.04938272),
    float2(0.64062500, 0.38271605), float2(0.39062500, 0.71604938), float2(0.89062500, 0.16049383), float2(0.07812500, 0.49382716),
    float2(0.57812500, 0.82716049), float2(0.32812500, 0.27160494), float2(0.82812500, 0.60493827), float2(0.20312500, 0.93827160),
    float2(0.70312500, 0.08641975), float2(0.45312500, 0.41975309), float2(0.95312500, 0.75308642), float2(0.04687500, 0.19753086),
    float2(0.54687500, 0.53086420), float2(0.29687500, 0.86419753), float2(0.79687500, 0.30864198), float2(0.17187500, 0.64197531),
    float2(0.67187500, 0.97530864), float2(0.42187500, 0.02469136), float2(0.92187500, 0.35802469), float2(0.10937500, 0.69135802),
    float2(0.60937500, 0.13580247), float2(0.35937500, 0.46913580), float2(0.85937500, 0.80246914), float2(0.23437500, 0.24691358),
    float2(0.73437500, 0.58024691), float2(0.48437500, 0.91358025), float2(0.98437500, 0.06172840), float2(0.00781250, 0.39506173)
};

// (-1, 1)
static const float2 k_HaltonDisk[65] = {
    float2(0.00000000, 0.00000000),
    float2(-0.35355339, 0.61237244), float2(-0.25000000, -0.43301270), float2(0.66341395, 0.55667040), float2(-0.33223151, 0.12092238),
    float2(0.13728094, -0.77855889), float2(0.10633736, 0.60306912), float2(-0.87900196, -0.31993055), float2(0.19151111, -0.16069690),
    float2(0.72978365, 0.17296190), float2(-0.38362074, 0.40661423), float2(-0.25852094, -0.86352008), float2(0.25857726, 0.34732953),
    float2(-0.82354974, 0.09625916), float2(0.26198214, -0.60734287), float2(-0.05629849, 0.96660772), float2(-0.14769477, -0.09714038),
    float2(0.65134112, -0.32711580), float2(0.47392027, 0.23801171), float2(-0.73847387, 0.48570191), float2(-0.02298376, -0.39461595),
    float2(0.32086128, 0.74384006), float2(-0.63306772, -0.07399500), float2(0.56847804, -0.76359853), float2(-0.08781520, 0.29332319),
    float2(-0.52878470, -0.56047903), float2(0.57049812, -0.13521054), float2(0.91579649, 0.07118133), float2(-0.26453839, 0.38570642),
    float2(-0.36572533, -0.76484965), float2(0.48879428, 0.47940605), float2(-0.94819874, 0.26394916), float2(0.03118014, -0.12104875),
    float2(0.06951701, 0.71469741), float2(-0.46919032, -0.21327317), float2(0.71185806, -0.50880556), float2(0.35709296, 0.11449725),
    float2(-0.59272441, 0.53786873), float2(-0.13231492, -0.61083366), float2(0.50320065, 0.79838218), float2(-0.27929829, 0.01083805),
    float2(0.35435401, -0.67272449), float2(-0.07752074, 0.56755223), float2(-0.71926819, -0.55747491), float2(0.41721815, -0.17045237),
    float2(0.71791777, 0.43326559), float2(-0.58937817, 0.32520512), float2(0.01893139, -0.97609764), float2(0.07009045, 0.20484708),
    float2(-0.72564809, -0.14251264), float2(0.35825865, -0.41051887), float2(-0.32152293, 0.83276528), float2(-0.26027716, -0.32269305),
    float2(0.80983534, -0.12665594), float2(0.64171823, 0.10036290), float2(-0.60278955, 0.74734179), float2(-0.11911759, -0.30852229),
    float2(0.51327745, 0.58815071), float2(-0.58824189, 0.11552694), float2(0.30010940, -0.87710282), float2(0.00938779, 0.48403189),
    float2(-0.75031560, -0.41400664), float2(0.59586694, -0.35960754), float2(0.91846328, 0.37523354), float2(-0.06986150, 0.05414675)
};

// [0, 1)
static const float2 k_Sobol[65] = {
    float2(0.00000000, 0.00000000),
    float2(0.50000000, 0.50000000), float2(0.25000000, 0.75000000), float2(0.75000000, 0.25000000), float2(0.12500000, 0.62500000),
    float2(0.62500000, 0.12500000), float2(0.37500000, 0.37500000), float2(0.87500000, 0.87500000), float2(0.06250000, 0.93750000),
    float2(0.56250000, 0.43750000), float2(0.31250000, 0.18750000), float2(0.81250000, 0.68750000), float2(0.18750000, 0.31250000),
    float2(0.68750000, 0.81250000), float2(0.43750000, 0.56250000), float2(0.93750000, 0.06250000), float2(0.03125000, 0.53125000),
    float2(0.53125000, 0.03125000), float2(0.28125000, 0.28125000), float2(0.78125000, 0.78125000), float2(0.15625000, 0.15625000),
    float2(0.65625000, 0.65625000), float2(0.40625000, 0.90625000), float2(0.90625000, 0.40625000), float2(0.09375000, 0.46875000),
    float2(0.59375000, 0.96875000), float2(0.34375000, 0.71875000), float2(0.84375000, 0.21875000), float2(0.21875000, 0.84375000),
    float2(0.71875000, 0.34375000), float2(0.46875000, 0.09375000), float2(0.96875000, 0.59375000), float2(0.01562500, 0.79687500),
    float2(0.51562500, 0.29687500), float2(0.26562500, 0.04687500), float2(0.76562500, 0.54687500), float2(0.14062500, 0.42187500),
    float2(0.64062500, 0.92187500), float2(0.39062500, 0.67187500), float2(0.89062500, 0.17187500), float2(0.07812500, 0.23437500),
    float2(0.57812500, 0.73437500), float2(0.32812500, 0.98437500), float2(0.82812500, 0.48437500), float2(0.20312500, 0.60937500),
    float2(0.70312500, 0.10937500), float2(0.45312500, 0.35937500), float2(0.95312500, 0.85937500), float2(0.04687500, 0.26562500),
    float2(0.54687500, 0.76562500), float2(0.29687500, 0.51562500), float2(0.79687500, 0.01562500), float2(0.17187500, 0.89062500),
    float2(0.67187500, 0.39062500), float2(0.42187500, 0.14062500), float2(0.92187500, 0.64062500), float2(0.10937500, 0.70312500),
    float2(0.60937500, 0.20312500), float2(0.35937500, 0.45312500), float2(0.85937500, 0.95312500), float2(0.23437500, 0.07812500),
    float2(0.73437500, 0.57812500), float2(0.48437500, 0.82812500), float2(0.98437500, 0.32812500), float2(0.00781250, 0.66406250)
};

// (-1, 1)
static const float2 k_SobolDisk[65] = {
    float2(0.00000000, 0.00000000),
    float2(-0.70710678, 0.00000000), float2(-0.00000000, -0.50000000), float2(0.00000000, 0.86602540), float2(-0.25000000, -0.25000000),
    float2(0.55901699, 0.55901699), float2(-0.43301270, 0.43301270), float2(0.66143783, -0.66143783), float2(0.23096988, -0.09567086),
    float2(-0.69290965, 0.28701257), float2(0.21392654, 0.51646436), float2(-0.34494618, -0.83277376), float2(-0.16570679, 0.40005157),
    float2(0.31730434, -0.76604044), float2(-0.61108887, -0.25312130), float2(0.89454251, 0.37053164), float2(-0.17337998, -0.03448742),
    float2(0.71486397, 0.14219529), float2(-0.10346227, 0.52013994), float2(0.17243711, -0.86689990), float2(0.21960842, 0.32866722),
    float2(-0.45006333, -0.67356737), float2(0.52995997, -0.35410793), float2(-0.79153549, 0.52888710), float2(-0.30030294, 0.05973397),
    float2(0.75574581, -0.15032719), float2(-0.11438184, -0.57503634), float2(0.17920190, 0.90090881), float2(0.25984418, -0.38888430),
    float2(-0.47100758, 0.70491266), float2(0.56926833, 0.38037294), float2(-0.81837478, -0.54682055), float2(0.03628558, -0.11961754),
    float2(-0.20844481, 0.68715046), float2(0.49319576, 0.14960930), float2(-0.83732279, -0.25399909), float2(-0.33072047, 0.17677378),
    float2(0.70588143, -0.37730148), float2(-0.29462296, -0.55120079), float2(0.44487091, 0.83229494), float2(0.02739662, 0.27816259),
    float2(-0.07452687, -0.75668405), float2(0.57006367, -0.05614637), float2(-0.90563177, 0.08919694), float2(-0.34839110, -0.28591719),
    float2(0.64818897, 0.53195494), float2(-0.42703905, 0.52034859), float2(0.61934624, -0.75467558), float2(-0.02122133, 0.21546381),
    float2(0.07248465, -0.73594903), float2(-0.54223871, -0.05340585), float2(0.88838006, 0.08749780), float2(0.32047320, -0.26300556),
    float2(-0.63362107, 0.51999937), float2(0.41205053, 0.50208502), float2(-0.60910841, -0.74220074), float2(-0.09600263, -0.31647827),
    float2(0.22660340, 0.74701131), float2(-0.57366558, 0.17401955), float2(0.88710743, -0.26910110), float2(0.42695830, 0.22821396),
    float2(-0.75576845, -0.40396665), float2(0.32807824, -0.61379122), float2(-0.46769945, 0.87500413), float2(-0.04544069, -0.07581321)
};

// ----------------------------------------------------------------------------------------------------
// Low-discrepancy sequence - Radical Inverse Functions(Van der Corput sequence)
// ----------------------------------------------------------------------------------------------------

// From https://www.pbr-book.org/3ed-2018/Sampling_and_Reconstruction/The_Halton_Sampler or https://pbr-book.org/4ed/Sampling_and_Reconstruction/Halton_Sampler
uint RadicalInverseVdC_Bits(uint a) // Base-2 radical inverse; a = 0, 1, 2...
{
    a = (a << 16u) | (a >> 16u);
    a = ((a & 0x55555555u) << 1u) | ((a & 0xAAAAAAAAu) >> 1u);
    a = ((a & 0x33333333u) << 2u) | ((a & 0xCCCCCCCCu) >> 2u);
    a = ((a & 0x0F0F0F0Fu) << 4u) | ((a & 0xF0F0F0F0u) >> 4u);
    a = ((a & 0x00FF00FFu) << 8u) | ((a & 0xFF00FF00u) >> 8u);
    return a;
}

float RadicalInverseVdc_Specialized(uint base, uint a) // Use prime number as base; a = 0, 1, 2...
{
    float invBase = 1.0 / (float) base;
    float invBaseM = 1.0;
    uint reversedDigits = 0;

    while (a)
    {
        uint next = a / base;
        uint digit = a - next * base;
        reversedDigits = reversedDigits * base + digit;
        invBaseM *= invBase;
        a = next;
    }
    return reversedDigits * invBaseM;
}

// ----------------------------------------------------------------------------------------------------
// Low-discrepancy sequence - Hammersley sequence
// ----------------------------------------------------------------------------------------------------

float2 Hammersley_Bits(uint index, uint sampleNumber)
{
    return float2(float(index) / float(sampleNumber), float(RadicalInverseVdC_Bits(index)) * 2.3283064365386963e-10); // /0x100000000
}

float2 Hammersley_Float(uint index, uint sampleNumber, uint primeBase = 2)
{
    return float2(float(index) / float(sampleNumber), RadicalInverseVdc_Specialized(primeBase,index));
}

// ----------------------------------------------------------------------------------------------------
// Low-discrepancy sequence - Halton sequence
// ----------------------------------------------------------------------------------------------------

float2 Halton_Float(uint index, uint primeBase1 = 2, uint primeBase2 = 3)
{
    return float2(RadicalInverseVdc_Specialized(primeBase1,index), RadicalInverseVdc_Specialized(primeBase2,index));
}

// ----------------------------------------------------------------------------------------------------
// Low-discrepancy sequence - Sobol sequence
// ----------------------------------------------------------------------------------------------------

// From https://www.shadertoy.com/view/sd2Xzm and https://www.jcgt.org/published/0009/04/01/
uint2 SobolGenerator(uint index)
{
    uint2 p = uint2(0u, 0u);
    uint2 d = uint2(0x80000000u, 0x80000000u);

    UNITY_UNROLLX(32)
    for(; index != 0u; index >>= 1u)
    {
        if((index & 1u) != 0u) p ^= d;
        d.x >>= 1u; // 1st dimension Sobol matrix, is same as base 2 Van der Corput
        d.y ^= d.y >> 1u; // 2nd dimension Sobol matrix
    }
    return p;
}

float2 Sobol_Bits(uint index)
{
    return float2(SobolGenerator(index)) * 2.3283064365386963e-10;
}

// From https://psychopath.io/post/2021_01_30_building_a_better_lk_hash
uint Hash_Owen(uint x, uint seed) // works best with random seeds
{
    x ^= x * 0x3d20adeau;
    x += seed;
    x *= (seed >> 16) | 1u;
    x ^= x * 0x05526c56u;
    x ^= x * 0x53a22864u;
    return x;
}

uint OwenScramble(uint x, uint seed)
{
    x = RadicalInverseVdC_Bits(x);
    x = Hash_Owen(x, seed);
    return RadicalInverseVdC_Bits(x);
}

float2 Sobol_Scrambled(uint index, uint seed1, uint seed2) // works best with random seeds
{
    uint2 sobol = SobolGenerator(index);
    sobol.x = OwenScramble(sobol.x, seed1);
    sobol.y = OwenScramble(sobol.y, seed2);
    return float2(sobol) * 2.3283064365386963e-10;
}

// ----------------------------------------------------------------------------------------------------
// Bob Jenkins' One-At-A-Time hashing algorithm: https://en.wikipedia.org/wiki/Jenkins_hash_function
// ----------------------------------------------------------------------------------------------------

uint Hash_Jenkins(uint x)
{
    x += ( x << 10u );
    x ^= ( x >>  6u );
    x += ( x <<  3u );
    x ^= ( x >> 11u );
    x += ( x << 15u );
    return x;
}

uint Hash_Jenkins(uint2 v) { return Hash_Jenkins(v.x ^ Hash_Jenkins(v.y)); }
uint Hash_Jenkins(uint3 v) { return Hash_Jenkins(v.x ^ Hash_Jenkins(v.y) ^ Hash_Jenkins(v.z)); }
uint Hash_Jenkins(uint4 v) { return Hash_Jenkins(v.x ^ Hash_Jenkins(v.y) ^ Hash_Jenkins(v.z) ^ Hash_Jenkins(v.w)); }

// ----------------------------------------------------------------------------------------------------
// Generate Pseudo-random value in [0, 1) using Bob Jenkins' One-At-A-Time hashing algorithm
// From https://stackoverflow.com/a/17479300
// ----------------------------------------------------------------------------------------------------

// Construct a float with half-open range [0, 1) using low 23 bits.
// All zeroes yields 0.0, all ones yields the next smallest representable value below 1.0.
float floatConstruct(uint m)
{
    const uint ieeeMantissa = 0x007FFFFFu; // binary32 mantissa bitmask
    const uint ieeeOne      = 0x3F800000u; // 1.0 in IEEE binary32

    m &= ieeeMantissa;                     // Keep only mantissa bits (fractional part)
    m |= ieeeOne;                          // Add fractional part to 1.0

    float f = asfloat(m);                 // Range [1, 2)
    return f - 1.0;                        // Range [0, 1)
}

float Random_Mantissa(float x) { return floatConstruct(Hash_Jenkins(asuint(x))); }
float Random_Mantissa(float2 v) { return floatConstruct(Hash_Jenkins(asuint(v))); }
float Random_Mantissa(float3 v) { return floatConstruct(Hash_Jenkins(asuint(v))); }
float Random_Mantissa(float4 v) { return floatConstruct(Hash_Jenkins(asuint(v))); }

// ----------------------------------------------------------------------------------------------------
// Generate Pseudo-random value in [0, 1), this method produces pattern when using large values, recommend to use a small range in [0, 1]
// ----------------------------------------------------------------------------------------------------

// From "The Book of Shaders" by Patricio Gonzalez Vivo and Jen Lowe
// https://thebookofshaders.com/10/ or https://github.com/patriciogonzalezvivo/lygia
float Random_Sine(float x) { return frac(sin(x) * 43758.5453123); }
float Random_Sine(float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123); }
float Random_Sine(float3 pos) { return frac(sin(dot(pos, float3(70.9898, 78.233, 32.4355))) * 43758.5453123); }
float Random_Sine(float4 pos) { return frac(sin(dot(pos, float4(12.9898, 78.233, 45.164, 94.673))) * 43758.5453123); }

// ----------------------------------------------------------------------------------------------------
// Generate Pseudo-random value in [0, 1) without sine
// ----------------------------------------------------------------------------------------------------

// From https://www.shadertoy.com/view/4djSRW or https://github.com/patriciogonzalezvivo/lygia
// smallRange = true: input with a small range in [0, 1] ; smallRange = false: input with a big range over 0 and 1
float Random_NoSine(float x, bool smallRange = true)
{
    x *= lerp(1, 43758.5453123, smallRange);
    x = frac(x * .1031);
    x *= x + 33.33;
    x *= x + x;
    return frac(x);
}

float Random_NoSine(float2 uv, bool smallRange = true)
{
    uv *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(uv.xyx * float3(.1031, .1030, .0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float Random_NoSine(float3 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float3(.1031, .1030, .0973));
    pos += dot(pos, pos.zyx + 31.32);
    return frac((pos.x + pos.y) * pos.z);
}

float Random_NoSine(float4 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float4(.1031, .1030, .0973, .1099));
    pos += dot(pos, pos.wzxy + 33.33);
    return frac((pos.x + pos.y) * (pos.z + pos.w));
}

float2 Random_NoSine2(float x, bool smallRange = true)
{
    x *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(float3(x, x, x) * float3(.1031, .1030, .0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.xx + p.yz) * p.zy);
}

float2 Random_NoSine2(float2 uv, bool smallRange = true)
{
    uv *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(uv.xyx * float3(.1031, .1030, .0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.xx + p.yz) * p.zy);
}

float2 Random_NoSine2(float3 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float3(.1031, .1030, .0973));
    pos += dot(pos, pos.yzx + 33.33);
    return frac((pos.xx + pos.yz) * pos.zy);
}

float3 Random_NoSine3(float x, bool smallRange = true)
{
    x *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(float3(x, x, x) * float3(.1031, .1030, .0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.xxy + p.yzz) * p.zyx); 
}

float3 Random_NoSine3(float2 uv, bool smallRange = true)
{
    uv *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(uv.xyx * float3(.1031, .1030, .0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yzz) * p.zyx);
}

float3 Random_NoSine3(float3 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float3(.1031, .1030, .0973));
    pos += dot(pos, pos.yxz + 33.33);
    return frac((pos.xxy + pos.yxx) * pos.zyx);
}

float4 Random_NoSine4(float x, bool smallRange = true)
{
    x *= lerp(1, 43758.5453123, smallRange);
    float4 p = frac(float4(x, x, x, x) * float4(.1031, .1030, .0973, .1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

float4 Random_NoSine4(float2 uv, bool smallRange = true)
{
    uv *= lerp(1, 43758.5453123, smallRange);
    float4 p = frac(uv.xyxy * float4(.1031, .1030, .0973, .1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

float4 Random_NoSine4(float3 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    float4 p = frac(pos.xyzx * float4(.1031, .1030, .0973, .1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

float4 Random_NoSine4(float4 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float4(.1031, .1030, .0973, .1099));
    pos += dot(pos, pos.wzxy + 33.33);
    return frac((pos.xxyz + pos.yzzw) * pos.zywx);
}

#endif