using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System;
using UnityEngine;

namespace Spelunky {

    public static class RunLogAnalyzer {

        private static readonly List<StageRunResult> EmptyStageResults = new List<StageRunResult>(0);

        private sealed class StageAggregate {
            public int Count;
            public int Cleared;
            public int Deaths;
            public int Interrupted;
            public float TotalDuration;
            public readonly Dictionary<string, int> DeathCauseCounts = new Dictionary<string, int>();
        }

        public static string BuildOverview(string path) {
            List<RunResult> results = LoadResults(path);
            if (results.Count == 0) {
                return "No persisted run logs found.";
            }

            return BuildAggregateSummary(results, $"Logged runs: {results.Count}");
        }

        public static string BuildSessionOverview(string path, DateTime sessionStartedUtc) {
            List<RunResult> sessionResults = LoadResults(path)
                .Where(result => TryGetTimestamp(result, out DateTime timestamp) && timestamp >= sessionStartedUtc)
                .ToList();

            if (sessionResults.Count == 0) {
                return "No runs recorded in this session.";
            }

            return BuildAggregateSummary(sessionResults, $"Session runs: {sessionResults.Count}");
        }

        public static int CountSessionRuns(string path, DateTime sessionStartedUtc) {
            return LoadResults(path)
                .Count(result => TryGetTimestamp(result, out DateTime timestamp) && timestamp >= sessionStartedUtc);
        }

        public static string BuildRecentStats(string path, int count = 5) {
            List<RunResult> recentRuns = LoadResults(path)
                .OrderByDescending(GetSortKey)
                .Take(Mathf.Max(1, count))
                .ToList();

            if (recentRuns.Count == 0) {
                return "No recent run stats available.";
            }

            int clearCount = recentRuns.Count(result => string.Equals(result.endReason, "clear"));
            int deathCount = recentRuns.Count(result => string.Equals(result.endReason, "death"));
            float clearRate = recentRuns.Count > 0 ? (float)clearCount / recentRuns.Count * 100f : 0f;
            float stage4ReachRate = recentRuns.Count > 0 ? (float)recentRuns.Count(result => result.finalStageIndex >= 4) / recentRuns.Count * 100f : 0f;
            float averageFinalStage = (float)recentRuns.Average(result => result.finalStageIndex);
            float averageFinalHealth = (float)recentRuns.Average(result => result.finalHealth);

            Dictionary<int, int> stageStops = new Dictionary<int, int>();
            Dictionary<string, int> deathCauseCounts = new Dictionary<string, int>();
            for (int i = 0; i < recentRuns.Count; i++) {
                RunResult result = recentRuns[i];
                if (!stageStops.ContainsKey(result.finalStageIndex)) {
                    stageStops[result.finalStageIndex] = 0;
                }

                stageStops[result.finalStageIndex]++;

                if (!string.IsNullOrWhiteSpace(result.finalDeathCause)) {
                    if (!deathCauseCounts.ContainsKey(result.finalDeathCause)) {
                        deathCauseCounts[result.finalDeathCause] = 0;
                    }

                    deathCauseCounts[result.finalDeathCause]++;
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Recent runs: {recentRuns.Count}");
            builder.AppendLine($"Clear rate: {clearRate:0}% ({clearCount} clear / {deathCount} death)");
            builder.AppendLine($"Stage 4 reach rate: {stage4ReachRate:0}%");
            builder.AppendLine($"Average final stage: {averageFinalStage:0.0}");
            builder.AppendLine($"Average final HP: {averageFinalHealth:0.0}");
            builder.AppendLine("Stage stop distribution:");

            foreach (KeyValuePair<int, int> pair in stageStops.OrderBy(pair => pair.Key)) {
                builder.AppendLine($"Stage {pair.Key}: {pair.Value}");
            }

            if (deathCauseCounts.Count > 0) {
                builder.AppendLine("Death cause mix:");
                foreach (KeyValuePair<string, int> pair in deathCauseCounts.OrderByDescending(pair => pair.Value).Take(3)) {
                    float ratio = deathCount > 0 ? (float)pair.Value / deathCount * 100f : 0f;
                    builder.AppendLine($"{pair.Key}: {pair.Value} ({ratio:0}%)");
                }
            }

            return builder.ToString();
        }

        public static string BuildRecentHighlights(string path, int count = 5) {
            List<RunResult> recentRuns = LoadResults(path)
                .OrderByDescending(GetSortKey)
                .Take(Mathf.Max(1, count))
                .ToList();

            if (recentRuns.Count == 0) {
                return "No recent highlight signals available.";
            }

            int clearCount = recentRuns.Count(result => string.Equals(result.endReason, "clear"));
            int stage4ReachCount = recentRuns.Count(result => result.finalStageIndex >= 4);
            float averageFinalHealth = (float)recentRuns.Average(result => result.finalHealth);
            string topDeathCause = recentRuns
                .Where(result => !string.IsNullOrWhiteSpace(result.finalDeathCause))
                .GroupBy(result => result.finalDeathCause)
                .OrderByDescending(group => group.Count())
                .Select(group => $"{group.Key} ({group.Count()})")
                .FirstOrDefault() ?? "none";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Recent clear count: {clearCount}/{recentRuns.Count}");
            builder.AppendLine($"Recent Stage 4 reach: {stage4ReachCount}/{recentRuns.Count}");
            builder.AppendLine($"Recent average final HP: {averageFinalHealth:0.0}");
            builder.AppendLine($"Top recent death cause: {topDeathCause}");
            return builder.ToString();
        }

        public static string BuildStageSummary(string path, int stageIndex) {
            List<RunResult> results = LoadResults(path);
            if (results.Count == 0) {
                return $"No records for Stage {stageIndex}.";
            }

            StageAggregate aggregate = BuildStageAggregate(results, stageIndex);
            if (aggregate.Count == 0) {
                return $"No records for Stage {stageIndex}.";
            }

            float averageDuration = aggregate.Count > 0 ? aggregate.TotalDuration / aggregate.Count : 0f;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Stage {stageIndex} entries: {aggregate.Count}");
            builder.AppendLine($"Cleared: {aggregate.Cleared}");
            builder.AppendLine($"Deaths: {aggregate.Deaths}");
            builder.AppendLine($"Interrupted: {aggregate.Interrupted}");
            builder.AppendLine($"Average stage time: {averageDuration:0.0}s");

            if (aggregate.DeathCauseCounts.Count > 0) {
                builder.AppendLine("Stage death causes:");
                foreach (KeyValuePair<string, int> pair in aggregate.DeathCauseCounts.OrderByDescending(pair => pair.Value).Take(3)) {
                    float ratio = aggregate.Deaths > 0 ? (float)pair.Value / aggregate.Deaths * 100f : 0f;
                    builder.AppendLine($"{pair.Key}: {pair.Value} ({ratio:0}%)");
                }
            }

            return builder.ToString();
        }

        private static string BuildAggregateSummary(List<RunResult> results, string header) {
            int clearCount = results.Count(result => string.Equals(result.endReason, "clear"));
            int deathCount = results.Count(result => string.Equals(result.endReason, "death"));
            float averageDuration = (float)results.Average(result => result.totalDurationSeconds);
            float clearRate = results.Count > 0 ? (float)clearCount / results.Count * 100f : 0f;
            float stage4ReachRate = results.Count > 0 ? (float)results.Count(result => result.finalStageIndex >= 4) / results.Count * 100f : 0f;
            float averageFinalStage = (float)results.Average(result => result.finalStageIndex);
            float averageFinalHealth = (float)results.Average(result => result.finalHealth);
            float averageFinalBombs = (float)results.Average(result => result.finalBombs);
            float averageFinalRopes = (float)results.Average(result => result.finalRopes);

            float[] stageTotals = new float[RunManager.DefaultStageCount];
            int[] stageCounts = new int[RunManager.DefaultStageCount];
            Dictionary<string, int> deathCauseCounts = new Dictionary<string, int>();

            for (int i = 0; i < results.Count; i++) {
                RunResult result = results[i];
                List<StageRunResult> stageResults = GetStageResults(result);

                for (int stageIndex = 0; stageIndex < stageResults.Count; stageIndex++) {
                    StageRunResult stage = stageResults[stageIndex];
                    int arrayIndex = Mathf.Clamp(stage.stageIndex - 1, 0, stageTotals.Length - 1);
                    stageTotals[arrayIndex] += stage.durationSeconds;
                    stageCounts[arrayIndex]++;
                }

                if (!string.IsNullOrWhiteSpace(result.finalDeathCause)) {
                    if (!deathCauseCounts.ContainsKey(result.finalDeathCause)) {
                        deathCauseCounts[result.finalDeathCause] = 0;
                    }

                    deathCauseCounts[result.finalDeathCause]++;
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(header);
            builder.AppendLine($"Clears: {clearCount} | Deaths: {deathCount} | Clear rate: {clearRate:0}%");
            builder.AppendLine($"Stage 4 reach rate: {stage4ReachRate:0}%");
            builder.AppendLine($"Average total time: {averageDuration:0.0}s");
            builder.AppendLine($"Average final stage: {averageFinalStage:0.0}");
            builder.AppendLine($"Average final resources: HP {averageFinalHealth:0.0}, Bombs {averageFinalBombs:0.0}, Ropes {averageFinalRopes:0.0}");

            for (int stageIndex = 0; stageIndex < stageTotals.Length; stageIndex++) {
                if (stageCounts[stageIndex] == 0) {
                    continue;
                }

                float averageStageTime = stageTotals[stageIndex] / stageCounts[stageIndex];
                builder.AppendLine($"Stage {stageIndex + 1} avg: {averageStageTime:0.0}s");
            }

            if (deathCauseCounts.Count > 0) {
                builder.AppendLine("Top death causes:");
                foreach (KeyValuePair<string, int> pair in deathCauseCounts.OrderByDescending(pair => pair.Value).Take(3)) {
                    float ratio = deathCount > 0 ? (float)pair.Value / deathCount * 100f : 0f;
                    builder.AppendLine($"{pair.Key}: {pair.Value} ({ratio:0}%)");
                }
            }

            return builder.ToString();
        }

        public static string BuildRecentRuns(string path, int count = 5) {
            List<RunResult> results = LoadResults(path);
            if (results.Count == 0) {
                return "No recent run records.";
            }

            List<RunResult> recentRuns = results
                .OrderByDescending(GetSortKey)
                .Take(Mathf.Max(1, count))
                .ToList();

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < recentRuns.Count; i++) {
                RunResult result = recentRuns[i];
                List<StageRunResult> stageResults = GetStageResults(result);
                builder.AppendLine($"[{i + 1}] {result.endReason} | Stage {result.finalStageIndex} | {result.totalDurationSeconds:0.0}s | HP {result.finalHealth} | B {result.finalBombs} | R {result.finalRopes}");

                if (!string.IsNullOrWhiteSpace(result.finalDeathCause)) {
                    builder.AppendLine($"Cause: {result.finalDeathCause}");
                }

                for (int stageIndex = 0; stageIndex < stageResults.Count; stageIndex++) {
                    StageRunResult stage = stageResults[stageIndex];
                    builder.AppendLine($"S{stage.stageIndex}: {stage.durationSeconds:0.0}s {stage.outcome}");
                }

                if (i < recentRuns.Count - 1) {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static List<RunResult> LoadResults(string path) {
            List<RunResult> results = new List<RunResult>();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) {
                return results;
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }

                try {
                    RunResult result = JsonUtility.FromJson<RunResult>(line);
                    if (result != null) {
                        results.Add(result);
                    }
                }
                catch {
                    // Ignore malformed rows so one bad line does not block QA summaries.
                }
            }

            return results;
        }

        private static long GetSortKey(RunResult result) {
            if (!TryGetTimestamp(result, out DateTime timestamp)) {
                return 0L;
            }

            return timestamp.Ticks;
        }

        private static bool TryGetTimestamp(RunResult result, out DateTime timestamp) {
            timestamp = default;
            if (result == null || string.IsNullOrWhiteSpace(result.endedAtUtc)) {
                return false;
            }

            return DateTime.TryParse(result.endedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp);
        }

        private static StageAggregate BuildStageAggregate(List<RunResult> results, int stageIndex) {
            StageAggregate aggregate = new StageAggregate();

            for (int i = 0; i < results.Count; i++) {
                RunResult result = results[i];
                List<StageRunResult> stageResults = GetStageResults(result);
                for (int stageEntryIndex = 0; stageEntryIndex < stageResults.Count; stageEntryIndex++) {
                    StageRunResult stage = stageResults[stageEntryIndex];
                    if (stage.stageIndex != stageIndex) {
                        continue;
                    }

                    aggregate.Count++;
                    aggregate.TotalDuration += stage.durationSeconds;

                    if (string.Equals(stage.outcome, "cleared")) {
                        aggregate.Cleared++;
                    }
                    else if (string.Equals(stage.outcome, "death")) {
                        aggregate.Deaths++;
                    }
                    else {
                        aggregate.Interrupted++;
                    }

                    if (!string.IsNullOrWhiteSpace(stage.deathCause)) {
                        if (!aggregate.DeathCauseCounts.ContainsKey(stage.deathCause)) {
                            aggregate.DeathCauseCounts[stage.deathCause] = 0;
                        }

                        aggregate.DeathCauseCounts[stage.deathCause]++;
                    }
                }
            }

            return aggregate;
        }

        private static List<StageRunResult> GetStageResults(RunResult result) {
            if (result == null || result.stageResults == null) {
                return EmptyStageResults;
            }

            return result.stageResults;
        }
    }

}
