using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RunMap
{
    /// <summary>
    /// Scene-independent team fog state for a RunMap tile grid.
    /// Visibility is recomputed from the supplied observers; exploration is retained until <see cref="Clear"/>.
    /// </summary>
    public sealed class RunMapFogGrid
    {
        private const int MaxWidth = 2048;
        private const int MaxHeight = 1024;

        private readonly int _width;
        private readonly int _height;
        private readonly ulong[] _exploredBits;
        private readonly List<int> _exploredCells = new List<int>();
        private readonly List<int> _dirtyCells = new List<int>();
        private readonly ReadOnlyCollection<int> _dirtyCellsView;

        private ulong[] _visibleBits;
        private ulong[] _nextVisibleBits;
        private readonly List<int> _visibleCells = new List<int>();
        private readonly List<int> _nextVisibleCells = new List<int>();

        private int _revision;
        private int _exploredCount;

        public RunMapFogGrid(int width, int height)
        {
            if (width < 1 || width > MaxWidth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), width, "Fog grid width must be between 1 and " + MaxWidth + ".");
            }

            if (height < 1 || height > MaxHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "Fog grid height must be between 1 and " + MaxHeight + ".");
            }

            _width = width;
            _height = height;

            int cellCount = width * height;
            int wordCount = (cellCount + 63) >> 6;
            _exploredBits = new ulong[wordCount];
            _visibleBits = new ulong[wordCount];
            _nextVisibleBits = new ulong[wordCount];
            _dirtyCellsView = new ReadOnlyCollection<int>(_dirtyCells);
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        /// <summary>Increments once when an update actually changes visibility or exploration.</summary>
        public int Revision
        {
            get { return _revision; }
        }

        public int ExploredCount
        {
            get { return _exploredCount; }
        }

        /// <summary>
        /// Cells whose visible/explored state changed during the most recent successful update or clear.
        /// Indices are <c>y * Width + x</c>, ascending and unique.
        /// </summary>
        public IReadOnlyList<int> DirtyCells
        {
            get { return _dirtyCellsView; }
        }

        public bool IsVisible(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                return false;
            }

            return GetBit(_visibleBits, y * _width + x);
        }

        public bool IsExplored(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                return false;
            }

            return GetBit(_exploredBits, y * _width + x);
        }

        /// <summary>
        /// Rebuilds current team visibility from all in-bounds observers.
        /// A null observer is ignored. Observers outside the grid are ignored. A null list is rejected.
        /// Radius is measured between cell centres and may be zero (observer cell only); negative values are rejected.
        /// </summary>
        public void UpdateVisibility(
            IReadOnlyList<Vector2Int> observers,
            int radius,
            Func<int, int, bool> blocksSight)
        {
            if (observers == null)
            {
                throw new ArgumentNullException(nameof(observers));
            }

            if (blocksSight == null)
            {
                throw new ArgumentNullException(nameof(blocksSight));
            }

            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "Fog radius cannot be negative.");
            }

            _dirtyCells.Clear();

            // Only the previous visible cells are touched. The rest of the map is not scanned or cleared.
            for (int i = 0; i < observers.Count; i++)
            {
                Vector2Int observer = observers[i];
                if (observer.x < 0 || observer.y < 0 || observer.x >= _width || observer.y >= _height)
                {
                    continue;
                }

                RevealFrom(observer.x, observer.y, radius, blocksSight);
            }

            for (int i = 0; i < _visibleCells.Count; i++)
            {
                int index = _visibleCells[i];
                if (!GetBit(_nextVisibleBits, index))
                {
                    _dirtyCells.Add(index);
                }
            }

            // The old front buffer becomes the next scratch buffer. Clearing only the previous
            // visible cells keeps the per-update cost proportional to local visibility.
            for (int i = 0; i < _visibleCells.Count; i++)
            {
                ClearBit(_visibleBits, _visibleCells[i]);
            }

            ulong[] oldVisibleBits = _visibleBits;
            _visibleBits = _nextVisibleBits;
            _nextVisibleBits = oldVisibleBits;

            _visibleCells.Clear();
            _visibleCells.AddRange(_nextVisibleCells);
            _nextVisibleCells.Clear();

            if (_dirtyCells.Count > 0)
            {
                _dirtyCells.Sort();
                _revision++;
            }
        }

        /// <summary>
        /// Removes all visible and explored state. DirtyCells contains every explored cell that changed.
        /// Revision remains monotonic and increments once when state actually changes.
        /// </summary>
        public void Clear()
        {
            _dirtyCells.Clear();
            if (_exploredCount == 0)
            {
                return;
            }

            for (int i = 0; i < _exploredCells.Count; i++)
            {
                int index = _exploredCells[i];
                ClearBit(_exploredBits, index);
                ClearBit(_visibleBits, index);
                _dirtyCells.Add(index);
            }

            _exploredCells.Clear();
            _visibleCells.Clear();
            _nextVisibleCells.Clear();
            _exploredCount = 0;

            _dirtyCells.Sort();
            _revision++;
        }

        private void RevealFrom(int observerX, int observerY, int radius, Func<int, int, bool> blocksSight)
        {
            // A radius larger than the grid only needs to cover the grid. Clamping also keeps
            // observer +/- radius arithmetic away from integer overflow.
            int scanRadius = Math.Min(radius, Math.Max(_width, _height));
            int minX = Math.Max(0, observerX - scanRadius);
            int maxX = Math.Min(_width - 1, observerX + scanRadius);
            int minY = Math.Max(0, observerY - scanRadius);
            int maxY = Math.Min(_height - 1, observerY + scanRadius);
            long radiusSquared = (long)radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - observerY;
                long dySquared = (long)dy * dy;
                if (dySquared > radiusSquared)
                {
                    continue;
                }

                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - observerX;
                    if ((long)dx * dx + dySquared > radiusSquared)
                    {
                        continue;
                    }

                    int index = y * _width + x;
                    if (GetBit(_nextVisibleBits, index))
                    {
                        continue;
                    }

                    if ((dx == 0 && dy == 0) || HasLineOfSight(observerX, observerY, x, y, blocksSight))
                    {
                        MarkVisible(index);
                    }
                }
            }
        }

        private void MarkVisible(int index)
        {
            SetBit(_nextVisibleBits, index);
            _nextVisibleCells.Add(index);

            if (!GetBit(_visibleBits, index))
            {
                _dirtyCells.Add(index);
            }

            if (!GetBit(_exploredBits, index))
            {
                SetBit(_exploredBits, index);
                _exploredCells.Add(index);
                _exploredCount++;
            }
        }

        /// <summary>
        /// Centre-to-centre supercover ray. The target cell is not tested as an occluder so a
        /// blocking cell remains visible when directly observed. A diagonal step is rejected only
        /// when both orthogonal side cells block, preventing peeking between two walls that meet
        /// at a corner while keeping a single open corner visible.
        /// </summary>
        private static bool HasLineOfSight(
            int observerX,
            int observerY,
            int targetX,
            int targetY,
            Func<int, int, bool> blocksSight)
        {
            if (observerX == targetX && observerY == targetY)
            {
                return true;
            }

            int dx = targetX - observerX;
            int dy = targetY - observerY;
            int stepX = Math.Sign(dx);
            int stepY = Math.Sign(dy);
            int absDx = Math.Abs(dx);
            int absDy = Math.Abs(dy);
            int x = observerX;
            int y = observerY;
            int nextX = 1;
            int nextY = 1;

            while (x != targetX || y != targetY)
            {
                bool crossedCorner = false;

                if (absDx == 0)
                {
                    y += stepY;
                    nextY += 2;
                }
                else if (absDy == 0)
                {
                    x += stepX;
                    nextX += 2;
                }
                else
                {
                    long xTime = (long)nextX * absDy;
                    long yTime = (long)nextY * absDx;
                    if (xTime < yTime)
                    {
                        x += stepX;
                        nextX += 2;
                    }
                    else if (yTime < xTime)
                    {
                        y += stepY;
                        nextY += 2;
                    }
                    else
                    {
                        crossedCorner = true;
                        int sideX = x + stepX;
                        int sideY = y + stepY;
                        if (blocksSight(sideX, y) && blocksSight(x, sideY))
                        {
                            return false;
                        }

                        x = sideX;
                        y = sideY;
                        nextX += 2;
                        nextY += 2;
                    }
                }

                if (x == targetX && y == targetY)
                {
                    return true;
                }

                if (blocksSight(x, y))
                {
                    return false;
                }

                // This assignment documents that the corner check has already inspected both
                // side cells; it also makes the non-corner cases explicit.
                if (crossedCorner)
                {
                    continue;
                }
            }

            return true;
        }

        private static bool GetBit(ulong[] bits, int index)
        {
            return (bits[index >> 6] & (1UL << (index & 63))) != 0;
        }

        private static void SetBit(ulong[] bits, int index)
        {
            bits[index >> 6] |= 1UL << (index & 63);
        }

        private static void ClearBit(ulong[] bits, int index)
        {
            bits[index >> 6] &= ~(1UL << (index & 63));
        }
    }
}
