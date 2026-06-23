using UnityEngine;

namespace YPipeline
{
    /// <summary>
    /// From: https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/
    /// </summary>
    public static class MortonCodeUtils
    {
        /// <summary>
        /// "Insert" a 0 bit after each of the 16 low bits of x
        /// </summary>
        private static uint Part1By1(uint x)
        {
            x &= 0x0000ffff;                  // x = ---- ---- ---- ---- fedc ba98 7654 3210
            x = (x ^ (x <<  8)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
            x = (x ^ (x <<  4)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
            x = (x ^ (x <<  2)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
            x = (x ^ (x <<  1)) & 0x55555555; // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
            return x;
        }

        /// <summary>
        /// "Insert" two 0 bits after each of the 10 low bits of x
        /// </summary>
        private static uint Part2By1(uint x)
        {
            x &= 0x000003ff;                  // x = ---- ---- ---- ---- ---- --98 7654 3210
            x = (x ^ (x << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
            x = (x ^ (x <<  8)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
            x = (x ^ (x <<  4)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
            x = (x ^ (x <<  2)) & 0x09249249; // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
            return x;
        }

        /// <summary>
        /// Inverse of Part1By1 - "delete" all odd-indexed bits
        /// </summary>
        private static uint Compact1By1(uint x)
        {
            x &= 0x55555555;                  // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
            x = (x ^ (x >>  1)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
            x = (x ^ (x >>  2)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
            x = (x ^ (x >>  4)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
            x = (x ^ (x >>  8)) & 0x0000ffff; // x = ---- ---- ---- ---- fedc ba98 7654 3210
            return x;
        }

        /// <summary>
        /// Inverse of Part2By1 - "delete" all bits not at positions divisible by 3
        /// </summary>
        private static uint Compact1By2(uint x)
        {
            x &= 0x09249249;                  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
            x = (x ^ (x >>  2)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
            x = (x ^ (x >>  4)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
            x = (x ^ (x >>  8)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
            x = (x ^ (x >> 16)) & 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
            return x;
        }

        private static uint DecodeMorton2X(uint code)
        {
            return Compact1By1(code >> 0);
        }

        private static uint DecodeMorton2Y(uint code)
        {
            return Compact1By1(code >> 1);
        }
        
        private static uint DecodeMorton3X(uint code)
        {
            return Compact1By2(code >> 0);
        }

        private static uint DecodeMorton3Y(uint code)
        {
            return Compact1By2(code >> 1);
        }

        private static uint DecodeMorton3Z(uint code)
        {
            return Compact1By2(code >> 2);
        }

        /// <summary>
        /// Encode 2D coordinates into a Morton code (Z-order curve).
        /// Max Coordinate value is 65535, for 16 bits
        /// </summary>
        public static uint EncodeMorton2D(Vector2Int coord)
        {
            return (Part1By1((uint) coord.y) << 1) | Part1By1((uint) coord.x);
        }

        /// <summary>
        /// Encode 3D coordinates into a Morton code (Z-order curve).
        /// Max Coordinate value is 1023, for 10 bits
        /// </summary>
        public static uint EncodeMorton3D(Vector3Int coord)
        {
            return (Part2By1((uint) coord.z) << 2) | (Part2By1((uint) coord.y) << 1) | Part2By1((uint) coord.x);
        }
        
        /// <summary>
        /// Decode a Morton code back into 2D coordinates.
        /// Max Coordinate value is 65535, for 16 bits
        /// </summary>
        public static Vector2Int DecodeMorton2D(uint code)
        {
            int x = (int) Compact1By1(code);
            int y = (int) Compact1By1(code >> 1);
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// Decode a Morton code back into 3D coordinates.
        /// Max Coordinate value is 1023, for 10 bits
        /// </summary>
        public static Vector3Int DecodeMorton3D(uint code)
        {
            int x = (int) Compact1By2(code);
            int y = (int) Compact1By2(code >> 1);
            int z = (int) Compact1By2(code >> 2);
            return new Vector3Int(x, y, z);
        }
    }
}