using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace YPipeline
{
    /// <summary>
    /// PenAtlasPacker is a utility class for packing power-of-two textures into a power-of-two atlas (e.g., shadow/reflection probe atlas).
    /// This algorithm is inspired by https://lisyarus.github.io/blog/posts/texture-packing.html.
    /// Time complexity: O(n log n), where n is the number of textures to pack. More specifically, O(n) initialization + O(n log n) sorting + O(n) packing.
    /// </summary>
    public sealed class PenAtlasPacker : IDisposable
    {
        private int[] m_Indices; // 排序索引缓存
        private int[] m_TempSizes; // 纹理大小缓存
        
        private List<Vector2Int> m_Ladder; // 可能需要扩容
        private int m_LadderCount;
        
        private struct DescendingComparer : IComparer<int>
        {
            private int[] m_Sizes;
            public DescendingComparer(int[] sizes)
            {
                m_Sizes = sizes;
            }
            public int Compare(int x, int y)
            {
                return m_Sizes[y].CompareTo(m_Sizes[x]);
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PenAtlasPacker"/> class.
        /// </summary>
        /// <param name="maxSquareCount">set to the maximum reflection probe count or punctual light shadow map square count</param>
        public PenAtlasPacker(int maxSquareCount)
        {
            m_Indices = new int[maxSquareCount];
            m_TempSizes = new int[maxSquareCount];
            m_Ladder = new List<Vector2Int>(Mathf.CeilToInt(Mathf.Sqrt(maxSquareCount)));
        }
        
        /// <summary>
        /// Packs power-of-two squares into a power-of-two atlas using a modified/simplified Skyline Algorithm. Make sure atlas size is sufficient to fit all squares.
        /// </summary>
        /// <param name="squareParams">xy: the output coordinates (bottom-left corner) of each square, z: the size of each square, w: pack failed = -1 (impossible in PenAtlasPacker)</param>
        /// <param name="squareCount">the number of squares to pack</param>
        /// <param name="atlasSize">the size of the atlas, must be ≥ the total area required to fit all squares</param>
        /// <param name="xMultiplier">because the size of octahedral map is (1.5, 1), this multiplier is used to scale the x-coordinate</param>
        public void Pack(ref Vector4[] squareParams, int squareCount, int atlasSize, float xMultiplier = 1.0f)
        {
            if (squareCount == 0) return;
            
            // 初始化缓冲与排序
            for (int i = 0; i < squareCount; i++)
            {
                int size = Mathf.RoundToInt(squareParams[i].z);
                m_TempSizes[i] = size;
                m_Indices[i] = i;
            }
            
            // 这里不直接对 squareParams 进行排序，而是通过 m_Indices 来间接排序，以保持 squareParams 的原始顺序不变
            Span<int> spanIndices = m_Indices.AsSpan(0, squareCount);
            spanIndices.Sort(new DescendingComparer(m_TempSizes)); // Sort indices based on sizes in descending order
            
            // ReadOnlySpan<int> spanSizes = m_TempSizes.AsSpan(0, squareCount);
            // spanIndices.SortIndices(spanSizes);
            // spanIndices.Reverse();
            
            // InsertionSortDescending(squareCount);
        
            // 初始化画笔和阶梯
            Vector2Int pen = new Vector2Int(0, 0);
            m_LadderCount = 0;
            m_Ladder.Clear();
            
            for (int i = 0; i < squareCount; i++)
            {
                int idx = m_Indices[i];
                int size = m_TempSizes[idx];
                squareParams[idx].x = pen.x * xMultiplier; // 分配位置
                squareParams[idx].y = pen.y;
                pen.x += size; // 向右移动画笔
                UpdateLadder(pen.x, pen.y + size); // 更新阶梯（ladder）
        
                // 检查是否到达右边界
                if (pen.x >= atlasSize)
                {
                    if (m_LadderCount > 0)
                    {
                        m_Ladder.RemoveAt(m_LadderCount - 1); // 移除最后一个阶梯点（因为这一行满了）
                        m_LadderCount--;
                    }
                    pen.y += size; // 向上移动画笔
                    pen.x = m_LadderCount > 0 ? m_Ladder[m_LadderCount - 1].x : 0; // 如果还有阶梯，从上一个阶梯的 x 开始；否则从 0 开始
                }
            }
        }
        
        private void UpdateLadder(int x, int y)
        {
            // 如果 ladder 非空，且最后一个点的 y 与当前 y 相同，则合并
            if (m_LadderCount > 0 && m_Ladder[m_LadderCount - 1].y == y)
            {
                Vector2Int temp = m_Ladder[m_LadderCount - 1];
                temp.x = x;
                m_Ladder[m_LadderCount - 1] = temp;
            }
            else // 否则添加新点
            {
                m_Ladder.Add(new Vector2Int(x, y));
                m_LadderCount++;
            }
        }
        
        /// <summary>
        /// Performs an insertion sort on the indices based on the corresponding sizes in descending order.
        /// </summary>
        /// <param name="count">sort count</param>
        private void InsertionSortDescending(int count)
        {
            for (int i = 1; i < count; i++)
            {
                int idx = m_Indices[i];
                int keySize = m_TempSizes[idx];
                int j = i - 1;

                while (j >= 0 && m_TempSizes[m_Indices[j]] < keySize)
                {
                    m_Indices[j + 1] = m_Indices[j];
                    j--;
                }
                m_Indices[j + 1] = idx;
            }
        }

        public void Dispose()
        {
            Array.Clear(m_Indices, 0, m_Indices.Length);
            Array.Clear(m_TempSizes, 0, m_TempSizes.Length);
            m_Ladder.Clear();
            m_Indices = null;
            m_TempSizes = null;
            m_Ladder = null;
        }
    }
}