using System;
using System.Collections;
using UnityEngine;

namespace YPipeline
{
    /// <summary>
    /// Unity's .NET Standard 2.1 doesn't support the BitArray.HasAnySet() method. Temporarily using this extension method for now.
    /// TODO：Delete after Unity 6.8 upgrade to .NET 10.
    /// </summary>
    public static class BitArrayExtensions
    {
        public static bool IsAnyTrue(this BitArray bitArray)
        {
            for (int i = 0; i < bitArray.Count; i++)
            {
                if (bitArray[i]) return true;
            }
            return false;
        }
    }
    
    /// <summary>
    /// BuddyAtlasPacker is a utility class for packing power-of-two textures into a power-of-two atlas (e.g., shadow/reflection probe atlas).
    /// In rare cases, it may leave empty cells in the atlas, even if the total area is sufficient.
    /// This algorithm is inspired by the Buddy memory allocation.
    /// Time complexity is difficult to estimate precisely: roughly O(n · l), where n is the number of textures and l is the number of levels in the atlas.
    /// <remarks>
    /// TODO：In my own tests, it’s slower than PenAtlasPacker, waiting for further verification and testing after Unity 6.8 upgrade to .NET 10.
    /// TODO：先尝试使用 CollectionsMarshal.AsBytes(bitArray) 获取 Span 并使用位运算来替换 BitArray 的遍历来修改 GetFirstFreeIndex 函数，看看能否提升性能。
    /// TODO：若性能还是不满意，改 BitArray 为 ulong[] / NativeArray 来存储位数据。
    /// </remarks>
    /// </summary>
    public sealed class BuddyAtlasPacker : IDisposable
    {
        private int m_MinSize;
        private int m_AtlasSize;
        private int m_MaxLevel;
        private BitArray[] m_FreeMask; // FreeMask[level][index], morton order 
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BuddyAtlasPacker"/> class.
        /// Better make sure atlasSize / minSquareSize less than or equal to 16384.
        /// </summary>
        /// <param name="atlasSize">the size of the atlas, must be power-of-two.</param>
        /// <param name="minSquareSize">the minimum size of each square, must be power-of-two.</param>
        public BuddyAtlasPacker(int atlasSize, int minSquareSize)
        {
            m_MinSize = minSquareSize;
            m_AtlasSize = atlasSize;
            m_MaxLevel = Mathf.RoundToInt(Mathf.Log(m_AtlasSize / m_MinSize, 2));
            m_FreeMask = new BitArray[m_MaxLevel + 1];
            
            for (int i = 0; i <= m_MaxLevel; i++)
            {
                int squareNum = 1 << (2 * i); // 4^level
                m_FreeMask[m_MaxLevel - i] = new BitArray(squareNum); // 默认都是 false，即不空闲（被占用）
            }
            
            m_FreeMask[m_MaxLevel].Set(0, true); // 最高级(只有一块)，设置为空闲 free
        }
        
        /// <summary>
        /// Allocate a square into the atlas.
        /// </summary>
        /// <param name="size">size of the square, must be power-of-two.</param>
        /// <param name="position">the output position of the allocated square.</param>
        /// <returns>true if the allocation succeeds, false otherwise.</returns>
        public bool Allocate(int size, out Vector2Int position)
        {
            // if (size > m_AtlasSize)
            // {
            //     position = Vector2Int.zero;
            //     return false;
            // }
            
            // 找到第一个有空闲块的级别
            int needLevel = Mathf.RoundToInt(Mathf.Log(size / m_MinSize, 2));
            int foundLevel = needLevel;
            while (foundLevel <= m_MaxLevel && !m_FreeMask[foundLevel].IsAnyTrue()) // TODO：Unity 6.8 后改为 HasAnySet()
                foundLevel++;
            
            if (foundLevel > m_MaxLevel)
            {
                position = Vector2Int.zero;
                return false;
            }
            
            // 取出该级别的一个空闲块索引
            int idx = GetFirstFreeIndex(foundLevel);
            m_FreeMask[foundLevel].Set(idx, false); // 标记为不空闲（被占用）
            
            // 分裂到 needLevel
            for (int i = foundLevel - 1; i >= needLevel; i--)
            {
                // 标记其他子块为空闲
                idx <<= 2; // idx * 4
                m_FreeMask[i].Set(idx ^ 1, true); // 标记水平伙伴为空闲
                m_FreeMask[i].Set(idx ^ 2, true); // 标记垂直伙伴为空闲
                m_FreeMask[i].Set(idx ^ 3, true); // 标记对角伙伴为空闲
            }
            
            // 计算坐标
            int blockSize = m_MinSize * (1 << needLevel);
            Vector2Int coord = MortonCodeUtils.DecodeMorton2D((uint) idx);
            position = coord * blockSize;
            return true;
        }

        /// <summary>
        /// Release a square from the atlas.
        /// </summary>
        /// <param name="size">size of the square, must be power-of-two.</param>
        /// <param name="position">the position of the square to release.</param>
        public void Free(int size, Vector2Int position)
        {
            // 标记当前块为空闲
            int needLevel = Mathf.RoundToInt(Mathf.Log(size / m_MinSize, 2));
            int blockSize = m_MinSize * (1 << needLevel);
            Vector2Int coord = position / blockSize;
            int idx = (int) MortonCodeUtils.EncodeMorton2D(coord);
            m_FreeMask[needLevel].Set(idx, true);
            
            // 尝试向上合并
            for (int l = needLevel; l < m_MaxLevel; l++)
            {
                // 检查 3 个伙伴是否都空闲
                bool canMerge = true;
                for (int i = 1; i <= 3; i++)
                {
                    if (!m_FreeMask[l].Get(idx ^ i)) // 获取水平、垂直、对角伙伴索引
                    {
                        canMerge = false;
                        break;
                    }
                }
                
                if (canMerge)
                {
                    // 合并到上一级
                    for (int j = 0; j <= 3; j++)
                    {
                        m_FreeMask[l].Set(idx ^ j, false); // 标记当前块与伙伴块为不空闲（被占用）
                    }
                    idx >>= 2; // idx / 4
                    m_FreeMask[l + 1].Set(idx, true); // 标记上一级块为空闲
                }
                else
                {
                    break; // 无法合并，退出循环
                }
            }
        }
        
        /// <summary>
        /// Pack power-of-two squares into the atlas.
        /// </summary>
        /// <param name="squareParams">xy: the output coordinates (bottom-left corner) of each square, z: the size of each square, w: pack failed = -1</param>
        /// <param name="squareCount">the number of squares to pack</param>
        /// <param name="xMultiplier">because the size of octahedral map is (1.5, 1), this multiplier is used to scale the x-coordinate</param>
        public void Pack(ref Vector4[] squareParams, int squareCount, float xMultiplier = 1.0f)
        {
            if (squareCount == 0) return;
            
            // 重置
            for (int i = 0; i < m_MaxLevel; i++)
            {
                m_FreeMask[i].SetAll(false);
            }
            m_FreeMask[m_MaxLevel].SetAll(true);
            
            // 打包
            for (int i = 0; i < squareCount; i++)
            {
                int size = Mathf.RoundToInt(squareParams[i].z);
                if (!Allocate(size, out Vector2Int position))
                {
#if UNITY_EDITOR || UNITY_ASSERTIONS
                    Debug.LogWarning($"BuddyAtlasPacker: Failed to allocate square {i} with size {size}");
#endif
                    squareParams[i].w = -1; // 标记为失败
                    continue;
                }
                
                squareParams[i].x = position.x * xMultiplier;
                squareParams[i].y = position.y;
            }
        }
        
        private int GetFirstFreeIndex(int level)
        {
            for (int i = 0; i < m_FreeMask[level].Count; i++)
                if (m_FreeMask[level][i]) return i;
            return -1;
        }

        public void Dispose()
        {
            if (m_FreeMask != null)
            {
                for (int i = 0; i < m_FreeMask.Length; i++)
                {
                    m_FreeMask[i] = null;
                }
                m_FreeMask = null;
            }
        }
    }
}