using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class RandomUtility
    {
        // ----------------------------------------------------------------------------------------------------
        // Low-discrepancy sequence - Radical Inverse Functions(Van der Corput sequence)
        // From "Physically Based Rendering: From Theory To Implementation"
        // ----------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// The base-2 radical inverse computed by a series of logical bit operations.
        /// </summary>
        /// <param name="a"> 0,1,2,3,4... </param>
        /// <returns>the base-2 radical inverse</returns>
        public static float RadicalInverseVdC_Bits(uint a)
        {
            a = (a << 16) | (a >> 16);
            a = ((a & 0x00ff00ff) << 8) | ((a & 0xff00ff00) >> 8);
            a = ((a & 0x0f0f0f0f) << 4) | ((a & 0xf0f0f0f0) >> 4);
            a = ((a & 0x33333333) << 2) | ((a & 0xcccccccc) >> 2);
            a = ((a & 0x55555555) << 1) | ((a & 0xaaaaaaaa) >> 1);
            return a * (float) 2.3283064365386963e-10;
        }

        /// <summary>
        /// The radical inverse computed by an integer arithmetic loop (support any base)
        /// </summary>
        /// <param name="baseNum">use prime number as the base</param>
        /// <param name="a"> 0,1,2,3,4... </param>
        /// <returns></returns>
        public static float RadicalInverseVdC_Specialized(int baseNum, ulong a)
        {
            float invBase = 1.0f / baseNum;
            ulong reversedDigits = 0;
            float invBaseN = 1.0f;
            
            while (a != 0)
            {
                ulong next = a / (ulong) baseNum;
                ulong digit = a - next * (ulong) baseNum;
                reversedDigits = reversedDigits * (ulong) baseNum + digit;
                invBaseN *= invBase;
                a = next;
            }
            return reversedDigits * invBaseN;
        }

        /// <summary>
        /// The radical inverse computed by the reversed integer digits (support any base)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="aDigits"></param>
        /// <returns></returns>
        public static float RadicalInverseVdC_Inverse(int baseNum, ulong a)
        {
            if (a == 0) return 0.0f;
            
            int aDigits = (int) Mathf.Floor(Mathf.Log(a, 2) + 1);
            float basePower = Mathf.Pow(baseNum, aDigits);
            
            ulong index = 0;
            for (int i = 0; i < aDigits; ++i)
            {
                ulong digit = a % (ulong) baseNum;
                a /= (ulong) baseNum;
                index = index * (ulong) baseNum + digit;
            }
            
            return index / basePower;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Low-discrepancy sequence - Hammersley sequence
        // ----------------------------------------------------------------------------------------------------
        
        public static Vector2 Hammersley_Bits(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Bits(index));
        }

        public static Vector2 Hammersley_Specialized(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Specialized(2, index));
        }
        
        public static Vector2 Hammersley_Inverse(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Inverse(2, index));
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Low-discrepancy sequence - Halton sequence
        // ----------------------------------------------------------------------------------------------------
        
        // [0, 1)
        public static readonly Vector2[] k_Halton = new Vector2[65]
        {
            new Vector2(0.00000000f, 0.00000000f), 
            new Vector2(0.50000000f, 0.33333333f), new Vector2(0.25000000f, 0.66666667f), new Vector2(0.75000000f, 0.11111111f), new Vector2(0.12500000f, 0.44444444f),
            new Vector2(0.62500000f, 0.77777778f), new Vector2(0.37500000f, 0.22222222f), new Vector2(0.87500000f, 0.55555556f), new Vector2(0.06250000f, 0.88888889f),
            new Vector2(0.56250000f, 0.03703704f), new Vector2(0.31250000f, 0.37037037f), new Vector2(0.81250000f, 0.70370370f), new Vector2(0.18750000f, 0.14814815f),
            new Vector2(0.68750000f, 0.48148148f), new Vector2(0.43750000f, 0.81481481f), new Vector2(0.93750000f, 0.25925926f), new Vector2(0.03125000f, 0.59259259f),
            new Vector2(0.53125000f, 0.92592593f), new Vector2(0.28125000f, 0.07407407f), new Vector2(0.78125000f, 0.40740741f), new Vector2(0.15625000f, 0.74074074f),
            new Vector2(0.65625000f, 0.18518519f), new Vector2(0.40625000f, 0.51851852f), new Vector2(0.90625000f, 0.85185185f), new Vector2(0.09375000f, 0.29629630f),
            new Vector2(0.59375000f, 0.62962963f), new Vector2(0.34375000f, 0.96296296f), new Vector2(0.84375000f, 0.01234568f), new Vector2(0.21875000f, 0.34567901f),
            new Vector2(0.71875000f, 0.67901235f), new Vector2(0.46875000f, 0.12345679f), new Vector2(0.96875000f, 0.45679012f), new Vector2(0.01562500f, 0.79012346f),
            new Vector2(0.51562500f, 0.23456790f), new Vector2(0.26562500f, 0.56790123f), new Vector2(0.76562500f, 0.90123457f), new Vector2(0.14062500f, 0.04938272f),
            new Vector2(0.64062500f, 0.38271605f), new Vector2(0.39062500f, 0.71604938f), new Vector2(0.89062500f, 0.16049383f), new Vector2(0.07812500f, 0.49382716f),
            new Vector2(0.57812500f, 0.82716049f), new Vector2(0.32812500f, 0.27160494f), new Vector2(0.82812500f, 0.60493827f), new Vector2(0.20312500f, 0.93827160f),
            new Vector2(0.70312500f, 0.08641975f), new Vector2(0.45312500f, 0.41975309f), new Vector2(0.95312500f, 0.75308642f), new Vector2(0.04687500f, 0.19753086f),
            new Vector2(0.54687500f, 0.53086420f), new Vector2(0.29687500f, 0.86419753f), new Vector2(0.79687500f, 0.30864198f), new Vector2(0.17187500f, 0.64197531f),
            new Vector2(0.67187500f, 0.97530864f), new Vector2(0.42187500f, 0.02469136f), new Vector2(0.92187500f, 0.35802469f), new Vector2(0.10937500f, 0.69135802f),
            new Vector2(0.60937500f, 0.13580247f), new Vector2(0.35937500f, 0.46913580f), new Vector2(0.85937500f, 0.80246914f), new Vector2(0.23437500f, 0.24691358f),
            new Vector2(0.73437500f, 0.58024691f), new Vector2(0.48437500f, 0.91358025f), new Vector2(0.98437500f, 0.06172840f), new Vector2(0.00781250f, 0.39506173f)
        };
        
        // (-1, 1)
        public static readonly Vector2[] k_HaltonDisk = new Vector2[65]
        {
            new Vector2(0.00000000f, 0.00000000f), 
            new Vector2(-0.35355339f, 0.61237244f), new Vector2(-0.25000000f, -0.43301270f), new Vector2(0.66341395f, 0.55667040f), new Vector2(-0.33223151f, 0.12092238f),
            new Vector2(0.13728094f, -0.77855889f), new Vector2(0.10633736f, 0.60306912f), new Vector2(-0.87900196f, -0.31993055f), new Vector2(0.19151111f, -0.16069690f),
            new Vector2(0.72978365f, 0.17296190f), new Vector2(-0.38362074f, 0.40661423f), new Vector2(-0.25852094f, -0.86352008f), new Vector2(0.25857726f, 0.34732953f),
            new Vector2(-0.82354974f, 0.09625916f), new Vector2(0.26198214f, -0.60734287f), new Vector2(-0.05629849f, 0.96660772f), new Vector2(-0.14769477f, -0.09714038f),
            new Vector2(0.65134112f, -0.32711580f), new Vector2(0.47392027f, 0.23801171f), new Vector2(-0.73847387f, 0.48570191f), new Vector2(-0.02298376f, -0.39461595f),
            new Vector2(0.32086128f, 0.74384006f), new Vector2(-0.63306772f, -0.07399500f), new Vector2(0.56847804f, -0.76359853f), new Vector2(-0.08781520f, 0.29332319f),
            new Vector2(-0.52878470f, -0.56047903f), new Vector2(0.57049812f, -0.13521054f), new Vector2(0.91579649f, 0.07118133f), new Vector2(-0.26453839f, 0.38570642f),
            new Vector2(-0.36572533f, -0.76484965f), new Vector2(0.48879428f, 0.47940605f), new Vector2(-0.94819874f, 0.26394916f), new Vector2(0.03118014f, -0.12104875f),
            new Vector2(0.06951701f, 0.71469741f), new Vector2(-0.46919032f, -0.21327317f), new Vector2(0.71185806f, -0.50880556f), new Vector2(0.35709296f, 0.11449725f),
            new Vector2(-0.59272441f, 0.53786873f), new Vector2(-0.13231492f, -0.61083366f), new Vector2(0.50320065f, 0.79838218f), new Vector2(-0.27929829f, 0.01083805f),
            new Vector2(0.35435401f, -0.67272449f), new Vector2(-0.07752074f, 0.56755223f), new Vector2(-0.71926819f, -0.55747491f), new Vector2(0.41721815f, -0.17045237f),
            new Vector2(0.71791777f, 0.43326559f), new Vector2(-0.58937817f, 0.32520512f), new Vector2(0.01893139f, -0.97609764f), new Vector2(0.07009045f, 0.20484708f),
            new Vector2(-0.72564809f, -0.14251264f), new Vector2(0.35825865f, -0.41051887f), new Vector2(-0.32152293f, 0.83276528f), new Vector2(-0.26027716f, -0.32269305f),
            new Vector2(0.80983534f, -0.12665594f), new Vector2(0.64171823f, 0.10036290f), new Vector2(-0.60278955f, 0.74734179f), new Vector2(-0.11911759f, -0.30852229f),
            new Vector2(0.51327745f, 0.58815071f), new Vector2(-0.58824189f, 0.11552694f), new Vector2(0.30010940f, -0.87710282f), new Vector2(0.00938779f, 0.48403189f),
            new Vector2(-0.75031560f, -0.41400664f), new Vector2(0.59586694f, -0.35960754f), new Vector2(0.91846328f, 0.37523354f), new Vector2(-0.06986150f, 0.05414675f)
        };

        public static Vector2 Halton_Specialized(uint index)
        {
            return new Vector2(RadicalInverseVdC_Bits(index), RadicalInverseVdC_Specialized(3, index));
        }
        
        public static Vector2 Halton_Inverse(uint index)
        {
            return new Vector2(RadicalInverseVdC_Bits(index), RadicalInverseVdC_Inverse(3, index));
        }
    }
}