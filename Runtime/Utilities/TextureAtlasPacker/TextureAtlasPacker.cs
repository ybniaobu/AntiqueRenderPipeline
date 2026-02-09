using System;
using System.Collections.Generic;
using UnityEngine;

namespace YPipeline
{
    public class TextureAtlasPacker
    {
        private const int k_MaxCount = 32;
        private int[] m_Indices; // 排序索引缓存
        private int[] m_TempSizes; // 纹理大小缓存
        
        private Vector2Int[] m_Ladder;
        private int m_LadderCount;
        
        public TextureAtlasPacker()
        {
            m_Indices = new int[k_MaxCount];
            m_TempSizes = new int[k_MaxCount];
            m_Ladder = new Vector2Int[k_MaxCount];
        }
        
        /// <summary>
        /// 借鉴了 Skyline 的改进的 Shelf Algorithm
        /// </summary>
        /// <param name="squareParams">xy: 每个方块的输出坐标（左下角）, z: 方块大小</param>
        /// <param name="squareCount">方块数量</param>
        /// <param name="packSize">打包图集尺寸，必须 ≥ 所有方块能放入</param>
        /// <param name="xMultiplier">因为 reflection probe 的大小是 (1.5, 1)，需乘上 1.5</param>
        public void Pack(ref Vector4[] squareParams, int squareCount, int packSize, float xMultiplier = 1.0f)
        {
            // 初始化缓冲与排序
            for (int i = 0; i < squareCount; i++)
            {
                int size = Mathf.RoundToInt(squareParams[i].z);
                m_TempSizes[i] = size;
                m_Indices[i] = i;
            }
            InsertionSortDescending(squareCount); // 数量较小，故这里选用插入排序
        
            // 初始化画笔和阶梯
            Vector2Int pen = new Vector2Int(0, 0);
            m_LadderCount = 0;
            
            for (int i = 0; i < squareCount; i++)
            {
                int idx = m_Indices[i];
                int size = m_TempSizes[idx];
                squareParams[idx].x = pen.x * xMultiplier; // 分配位置
                squareParams[idx].y = pen.y;
                pen.x += size; // 向右移动画笔
                UpdateLadder(pen.x, pen.y + size); // 更新阶梯（ladder）
        
                // 检查是否到达右边界
                if (pen.x >= packSize)
                {
                    if (m_LadderCount > 0) m_LadderCount--; // 移除最后一个阶梯点（因为这一行满了）
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
                m_Ladder[m_LadderCount - 1].x = x;
            }
            else // 否则添加新点
            {
                m_Ladder[m_LadderCount].x = x;
                m_Ladder[m_LadderCount].y = y;
                m_LadderCount++;
            }
        }
        
        /// <summary>
        /// Shelf Algorithm 简单排序
        /// </summary>
        /// <param name="squareParams">xy: 每个方块的输出坐标（左下角）, z: 方块大小</param>
        /// <param name="squareCount">方块数量</param>
        /// <param name="packSize">打包图集尺寸，必须 ≥ 所有方块能放入</param>
        /// <param name="xMultiplier">因为 reflection probe 的大小是 (1.5, 1)，需乘上 1.5</param>
        public void SimplePack(ref Vector4[] squareParams, int squareCount, int packSize, float xMultiplier = 1.0f)
        {
            // 排序
            for (int i = 0; i < squareCount; i++)
            {
                int size = Mathf.RoundToInt(squareParams[i].z);
                m_TempSizes[i] = size;
                m_Indices[i] = i;
            }
            InsertionSortDescending(squareCount); //因为 reflection probe 本来就按重要性和大小排过序了，故这里选用插入排序
    
            // pack
            int x = 0, y = 0, rowHeight = 0;
            for (int i = 0; i < squareCount; i++)
            {
                int idx = m_Indices[i];
                int currentSize = m_TempSizes[idx];
                if (x + currentSize > packSize)
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }
        
                squareParams[idx].x = x * xMultiplier;
                squareParams[idx].y = y;
                x += currentSize;
                if (currentSize > rowHeight) rowHeight = currentSize;
            }
        }
        
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
            Array.Clear(m_Indices, 0, k_MaxCount);
            Array.Clear(m_TempSizes, 0, k_MaxCount);
            Array.Clear(m_Ladder, 0, k_MaxCount);
            m_Indices = null;
            m_TempSizes = null;
            m_Ladder = null;
        }
    }
}