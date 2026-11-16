// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do the farm half and the outing half still feed each other when the world is a set of free, daily-regenerated biome maps joined by mid-map branches you must unlock?
// Date: 2026-10-30
//
// Batchmode smoke check for the map generator (pure data, no Play mode):
//   Unity.exe -batchmode -quit -projectPath . -executeMethod DayLoopProto.DayLoopMapSmoke.Run -logFile -
// For each map, several seeds and several days: length in range, ends walled
// by terrain, every branch / landmark / building placed, the boss placed, the
// oasis (charm fishing) present, a fishing spot wherever there is water,
// deterministic within a day, different across days for exploration maps.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DayLoopProto
{
    public static class DayLoopMapSmoke
    {
        public static void Run()
        {
            int problems = 0, checks = 0;
            foreach (int seed in new[] { 20261029, 1, 777, 123456, 99999 })
            {
                foreach (var def in World.Maps.Values)
                {
                    for (int day = 1; day <= 4; day++)
                    {
                        checks++;
                        var a = MapGen.Generate(def, seed, day);
                        var b = MapGen.Generate(def, seed, day);
                        var issues = new List<string>();

                        if (!a.floor.SequenceEqual(b.floor) || a.branches.Count != b.branches.Count) issues.Add("NOT DETERMINISTIC");
                        if (def.RerollsDaily && day > 1 && MapGen.Generate(def, seed, day - 1).floor.SequenceEqual(a.floor)) issues.Add("same map as yesterday");
                        if (a.width < def.minWidth - 1f || a.width > def.maxWidth + 1f) issues.Add($"width {a.width}");

                        int wantBranches = World.EdgesOf(def.id).Count();
                        if (a.branches.Count != wantBranches) issues.Add($"branches {a.branches.Count}/{wantBranches}");
                        if (a.landmarks.Count != def.landmarks.Length) issues.Add($"landmarks {a.landmarks.Count}/{def.landmarks.Length}");
                        if (a.buildings.Count != def.buildings.Length) issues.Add($"buildings {a.buildings.Count}/{def.buildings.Length}");
                        if (def.boss != null && def.kind != MapKind.Village && float.IsNaN(a.bossX)) issues.Add("boss not placed");
                        if (def.forcedZone != null && a.zones.All(z => z.def.name != def.forcedZone)) issues.Add($"missing zone {def.forcedZone}");
                        if (a.waters.Count > 0 && a.fishingSpots.Count < a.waters.Count) issues.Add("water without fishing spot");
                        if (def.fishItem != null && !a.fishingSpots.Any(f => f.kind == def.pondWater)) issues.Add("no spot to fish the branch item");
                        foreach (var br in a.branches)
                            if (br.x < a.playMin || br.x > a.playMax) issues.Add($"branch {br.edge.Id} outside play area");

                        // Ends must stop the player: cliff/rock wall rises > jump height, water has a shore wall.
                        foreach (var (kind, left) in new[] { (a.leftEnd, true), (a.rightEnd, false) })
                        {
                            float shore = a.GroundY(left ? a.playMin : a.playMax);
                            float outer = a.GroundY(left ? a.playMin - 3f : a.playMax + 3f);
                            bool water = kind != EndKind.Cliff && kind != EndKind.RockWall;
                            if (!water && outer - shore < 3f) issues.Add($"{kind} too low ({outer - shore:0.0})");
                            if (water && !a.invisibleWalls.Any(w => Mathf.Abs(w - (left ? a.playMin : a.playMax)) < 1f)) issues.Add($"{kind} has no shore wall");
                        }
                        if (a.ceil != null)
                        {
                            float minGap = float.MaxValue;
                            for (int i = 0; i < a.floor.Length; i++)
                            {
                                float x = i * MapLayout.Step;
                                if (x > a.playMin && x < a.playMax) minGap = Mathf.Min(minGap, a.ceil[i] - a.floor[i]);
                            }
                            if (minGap < 4.5f) issues.Add($"cave gap {minGap:0.0}");
                        }

                        problems += issues.Count;
                        if (day == 1 || issues.Count > 0)
                            Debug.Log($"[DayLoopSmoke] seed {seed} d{day} {def.id,-6} w {a.width:0} ends {a.leftEnd}/{a.rightEnd} " +
                                      $"zones [{string.Join(", ", a.zones.Select(z => z.def.name))}] " +
                                      $"branches [{string.Join(", ", a.branches.Select(x => $"{x.edge.Other(def.id)}@{x.x:0}"))}] " +
                                      $"landmarks {a.landmarks.Count} buildings {a.buildings.Count} enemies {a.enemies.Count} ore {a.ores.Count} " +
                                      $"waters {a.waters.Count} spots {a.fishingSpots.Count} boss {(float.IsNaN(a.bossX) ? "-" : a.bossX.ToString("0"))} " +
                                      $"{(issues.Count == 0 ? "OK" : "ISSUES: " + string.Join("; ", issues))}");
                    }
                }
            }
            Debug.Log($"[DayLoopSmoke] done, {checks} maps checked, {problems} issue(s)");
        }
    }
}
