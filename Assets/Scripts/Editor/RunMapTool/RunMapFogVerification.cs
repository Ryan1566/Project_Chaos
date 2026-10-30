using System;
using System.Collections.Generic;
using System.Text;
using RunMap;
using UnityEditor;
using UnityEngine;

namespace RunMapTool.Editor
{
    /// <summary>
    /// In-memory contract checks for RunMapFogGrid. This utility never reads or writes project saves.
    /// </summary>
    public static class RunMapFogVerification
    {
        [MenuItem("Tools/RunMap/Fog Grid Verification")]
        public static void RunFromMenu()
        {
            string report = Verify();
            if (report.IndexOf("verification: FAIL", StringComparison.Ordinal) >= 0)
            {
                Debug.LogError(report);
            }
            else
            {
                Debug.Log(report);
            }
        }

        public static string Verify()
        {
            int checks = 0;
            int failures = 0;
            List<string> lines = new List<string>();

            Action<bool, string> check = (condition, name) =>
            {
                checks++;
                if (condition)
                {
                    lines.Add("PASS " + name);
                }
                else
                {
                    failures++;
                    lines.Add("FAIL " + name);
                }
            };

            Func<int, int, bool> open = (x, y) => false;

            RunMapFogGrid emptyGrid = new RunMapFogGrid(1, 1);
            check(emptyGrid.Width == 1 && emptyGrid.Height == 1, "minimum dimensions are accepted");
            check(emptyGrid.Revision == 0 && emptyGrid.ExploredCount == 0, "new grid starts empty");
            check(emptyGrid.DirtyCells.Count == 0, "new grid has no dirty cells");
            check(!emptyGrid.IsVisible(0, 0) && !emptyGrid.IsExplored(0, 0), "new grid cells are hidden");
            check(!emptyGrid.IsVisible(-1, 0) && !emptyGrid.IsVisible(0, -1), "negative reads are false");
            check(!emptyGrid.IsVisible(1, 0) && !emptyGrid.IsExplored(0, 1), "out-of-range reads are false");

            RunMapFogGrid maximumGrid = new RunMapFogGrid(2048, 1024);
            check(maximumGrid.Width == 2048 && maximumGrid.Height == 1024, "maximum dimensions are accepted");

            check(Throws(() => new RunMapFogGrid(0, 10)), "zero width is rejected");
            check(Throws(() => new RunMapFogGrid(10, 0)), "zero height is rejected");
            check(Throws(() => new RunMapFogGrid(-1, 10)), "negative width is rejected");
            check(Throws(() => new RunMapFogGrid(10, -1)), "negative height is rejected");
            check(Throws(() => new RunMapFogGrid(2049, 10)), "width above maximum is rejected");
            check(Throws(() => new RunMapFogGrid(10, 1025)), "height above maximum is rejected");

            RunMapFogGrid circle = new RunMapFogGrid(11, 11);
            Vector2Int[] circleObservers = { new Vector2Int(5, 5) };
            circle.UpdateVisibility(circleObservers, 2, open);
            check(circle.IsVisible(5, 5), "observer cell is visible");
            check(circle.IsVisible(5, 7) && circle.IsVisible(7, 5), "circle includes cardinal radius");
            check(circle.IsVisible(6, 6), "circle includes diagonal inside radius");
            check(!circle.IsVisible(7, 6) && !circle.IsVisible(7, 7), "circle excludes cells beyond radius");
            check(!circle.IsVisible(8, 5), "radius is Euclidean, not a large square");
            check(circle.IsExplored(5, 7) && circle.IsExplored(6, 6), "visible cells become explored");
            check(circle.ExploredCount == CountTrue(circle, 11, 11, false), "explored count matches grid state");
            check(circle.Revision == 1, "first visible state increments revision once");
            check(circle.DirtyCells.Count == circle.ExploredCount, "initial dirty cells cover newly explored cells");
            check(IsUnique(circle.DirtyCells), "initial dirty cells are unique");

            int stableRevision = circle.Revision;
            circle.UpdateVisibility(circleObservers, 2, open);
            check(circle.Revision == stableRevision, "identical update does not increment revision");
            check(circle.DirtyCells.Count == 0, "identical update reports no dirty cells");

            RunMapFogGrid yAxis = new RunMapFogGrid(12, 10);
            yAxis.UpdateVisibility(new[] { new Vector2Int(3, 5) }, 1, open);
            check(yAxis.IsVisible(3, 6), "positive y is upward in row indexing");
            check(ContainsIndex(yAxis.DirtyCells, 6 * 12 + 3), "dirty index uses y * width + x");
            check(ContainsIndex(yAxis.DirtyCells, 5 * 12 + 4), "dirty index uses x offset correctly");

            RunMapFogGrid wallGrid = new RunMapFogGrid(12, 8);
            HashSet<Vector2Int> wall = new HashSet<Vector2Int> { new Vector2Int(5, 4) };
            wallGrid.UpdateVisibility(new[] { new Vector2Int(3, 4) }, 5, (x, y) => wall.Contains(new Vector2Int(x, y)));
            check(wallGrid.IsVisible(5, 4), "blocking cell itself is visible");
            check(wallGrid.IsVisible(4, 4), "cell before a wall is visible");
            check(!wallGrid.IsVisible(6, 4) && !wallGrid.IsVisible(7, 4), "cells behind a wall are hidden");
            check(!wallGrid.IsExplored(6, 4), "cells behind a wall are not explored");

            RunMapFogGrid cornerGrid = new RunMapFogGrid(8, 8);
            HashSet<Vector2Int> cornerWalls = new HashSet<Vector2Int>
            {
                new Vector2Int(3, 2),
                new Vector2Int(2, 3)
            };
            cornerGrid.UpdateVisibility(
                new[] { new Vector2Int(2, 2) },
                3,
                (x, y) => cornerWalls.Contains(new Vector2Int(x, y)));
            check(cornerGrid.IsVisible(3, 2) && cornerGrid.IsVisible(2, 3), "both corner walls are visible");
            check(!cornerGrid.IsVisible(3, 3), "diagonal gap between two walls is blocked");
            check(!cornerGrid.IsVisible(4, 2) && !cornerGrid.IsVisible(2, 4), "corner walls hide cells behind them");

            RunMapFogGrid shared = new RunMapFogGrid(14, 10);
            Vector2Int[] sharedObservers = { new Vector2Int(2, 2), new Vector2Int(10, 6) };
            shared.UpdateVisibility(sharedObservers, 2, open);
            check(shared.IsVisible(0, 2) && shared.IsVisible(8, 6), "first observer contributes vision");
            check(shared.IsVisible(12, 6) && shared.IsVisible(10, 8), "second observer contributes vision");
            check(shared.ExploredCount == CountTrue(shared, 14, 10, false), "multiple observers share one explored set");
            check(shared.DirtyCells.Count == shared.ExploredCount, "overlapping observers do not duplicate explored cells");
            check(IsUnique(shared.DirtyCells), "overlapping observers do not duplicate dirty cells");

            RunMapFogGrid overlap = new RunMapFogGrid(10, 10);
            overlap.UpdateVisibility(
                new[] { new Vector2Int(3, 3), new Vector2Int(4, 3) },
                2,
                open);
            check(overlap.ExploredCount == CountTrue(overlap, 10, 10, false), "overlapping observers count union cells once");
            check(IsUnique(overlap.DirtyCells), "overlapping observer dirty cells are unique");

            RunMapFogGrid moving = new RunMapFogGrid(20, 8);
            moving.UpdateVisibility(new[] { new Vector2Int(2, 4) }, 2, open);
            int initialExplored = moving.ExploredCount;
            int beforeMoveRevision = moving.Revision;
            check(moving.IsVisible(4, 4) && moving.IsExplored(4, 4), "pre-move cell is visible and explored");

            moving.UpdateVisibility(new[] { new Vector2Int(15, 4) }, 2, open);
            check(!moving.IsVisible(4, 4), "old visibility is cleared after moving");
            check(moving.IsExplored(4, 4), "old exploration is retained after moving");
            check(moving.IsVisible(17, 4), "new observer location reveals new cells");
            check(moving.ExploredCount > initialExplored, "moving accumulates explored cells");
            check(moving.Revision == beforeMoveRevision + 1, "movement increments revision once");
            check(ContainsIndex(moving.DirtyCells, 4 * 20 + 4), "old visible cell is reported dirty after moving");
            check(ContainsIndex(moving.DirtyCells, 4 * 20 + 17), "new visible cell is reported dirty after moving");

            int exploredAfterMove = moving.ExploredCount;
            int beforeEmptyRevision = moving.Revision;
            moving.UpdateVisibility(Array.Empty<Vector2Int>(), 2, open);
            check(CountTrue(moving, 20, 8, true) == 0, "empty observers clear current visibility");
            check(moving.ExploredCount == exploredAfterMove, "empty observers retain exploration");
            check(moving.IsExplored(17, 4), "empty observers keep explored cells readable");
            check(moving.Revision == beforeEmptyRevision + 1, "clearing visibility with empty observers increments revision");
            check(moving.DirtyCells.Count > 0, "clearing visibility reports changed cells");

            int emptyRevision = moving.Revision;
            moving.UpdateVisibility(Array.Empty<Vector2Int>(), 2, open);
            check(moving.Revision == emptyRevision, "repeated empty update does not increment revision");
            check(moving.DirtyCells.Count == 0, "repeated empty update reports no dirty cells");

            int exploredBeforeClear = moving.ExploredCount;
            int beforeClearRevision = moving.Revision;
            moving.Clear();
            check(moving.ExploredCount == 0, "clear resets explored count");
            check(CountTrue(moving, 20, 8, true) == 0, "clear resets visibility");
            check(CountTrue(moving, 20, 8, false) == 0, "clear resets exploration");
            check(moving.DirtyCells.Count == exploredBeforeClear, "clear reports every changed explored cell");
            check(moving.Revision == beforeClearRevision + 1, "clear increments revision once when state changes");

            int clearRevision = moving.Revision;
            moving.Clear();
            check(moving.Revision == clearRevision, "clearing an empty grid does not increment revision");
            check(moving.DirtyCells.Count == 0, "clearing an empty grid reports no dirty cells");

            RunMapFogGrid invalidRadius = new RunMapFogGrid(5, 5);
            invalidRadius.UpdateVisibility(new[] { new Vector2Int(2, 2) }, 1, open);
            int invalidRevision = invalidRadius.Revision;
            int invalidDirtyCount = invalidRadius.DirtyCells.Count;
            check(Throws(() => invalidRadius.UpdateVisibility(new[] { new Vector2Int(2, 2) }, -1, open)), "negative radius is rejected");
            check(invalidRadius.Revision == invalidRevision, "rejected radius leaves revision unchanged");
            check(invalidRadius.DirtyCells.Count == invalidDirtyCount, "rejected radius leaves dirty cells unchanged");

            RunMapFogGrid outside = new RunMapFogGrid(6, 6);
            outside.UpdateVisibility(
                new[] { new Vector2Int(-1, 0), new Vector2Int(6, 0), new Vector2Int(0, 6), new Vector2Int(0, -1) },
                3,
                open);
            check(outside.ExploredCount == 0 && outside.DirtyCells.Count == 0, "out-of-bounds observers are ignored");
            check(outside.Revision == 0, "out-of-bounds observers do not change revision");

            RunMapFogGrid zeroRadius = new RunMapFogGrid(5, 5);
            zeroRadius.UpdateVisibility(new[] { new Vector2Int(2, 2) }, 0, open);
            check(zeroRadius.IsVisible(2, 2), "zero radius keeps observer cell visible");
            check(!zeroRadius.IsVisible(2, 3) && !zeroRadius.IsVisible(3, 2), "zero radius excludes neighbours");
            check(zeroRadius.ExploredCount == 1, "zero radius explores one cell");

            RunMapFogGrid hugeRadius = new RunMapFogGrid(5, 5);
            hugeRadius.UpdateVisibility(new[] { new Vector2Int(2, 2) }, int.MaxValue, open);
            check(CountTrue(hugeRadius, 5, 5, true) == 25, "very large radius covers the grid without overflow");

            RunMapFogGrid solidObserver = new RunMapFogGrid(7, 7);
            HashSet<Vector2Int> solidCells = new HashSet<Vector2Int>
            {
                new Vector2Int(3, 3),
                new Vector2Int(4, 3),
                new Vector2Int(3, 4)
            };
            solidObserver.UpdateVisibility(
                new[] { new Vector2Int(3, 3) },
                1,
                (x, y) => solidCells.Contains(new Vector2Int(x, y)));
            check(solidObserver.IsVisible(3, 3), "observer inside a blocking cell is handled");
            check(solidObserver.IsVisible(4, 3) && solidObserver.IsVisible(3, 4), "adjacent blocking cells remain visible");
            check(solidObserver.IsVisible(2, 3) && solidObserver.IsVisible(3, 2), "solid observer does not index out of bounds");

            StringBuilder report = new StringBuilder();
            report.Append("RunMapFogGrid verification: ");
            report.Append(failures == 0 ? "PASS" : "FAIL");
            report.Append(" checks=");
            report.Append(checks);
            report.Append(" failures=");
            report.Append(failures);
            for (int i = 0; i < lines.Count; i++)
            {
                report.Append('\n');
                report.Append(lines[i]);
            }

            return report.ToString();
        }

        private static bool Throws(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                return true;
            }

            return false;
        }

        private static bool ContainsIndex(IReadOnlyList<int> cells, int index)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == index)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUnique(IReadOnlyList<int> cells)
        {
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < cells.Count; i++)
            {
                if (!seen.Add(cells[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountTrue(RunMapFogGrid grid, int width, int height, bool visible)
        {
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (visible ? grid.IsVisible(x, y) : grid.IsExplored(x, y))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
