using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Spelunky {

    public static class RunLogSummaryExporter {

        public static string ExportMarkdownSummary(string logPath, DateTime sessionStartedUtc, string sessionId, string activePresetLabel, string sessionRunType, string sessionNote, string sessionBuildLabel, string testerTag) {
            try {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrWhiteSpace(projectRoot)) {
                    return "Project root unavailable.";
                }

                string qaDirectory = Path.Combine(projectRoot, "Docs", "QA");
                Directory.CreateDirectory(qaDirectory);
                string reportsDirectory = Path.Combine(qaDirectory, "Reports");
                Directory.CreateDirectory(reportsDirectory);

                string summaryPath = Path.Combine(qaDirectory, "LATEST_PLAYTEST_SUMMARY.md");
                string archivePath = Path.Combine(reportsDirectory, $"PLAYTEST_{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.md");

                string markdown = BuildMarkdown(logPath, sessionStartedUtc, sessionId, activePresetLabel, sessionRunType, sessionNote, sessionBuildLabel, testerTag);

                File.WriteAllText(summaryPath, markdown, Encoding.UTF8);
                File.WriteAllText(archivePath, markdown, Encoding.UTF8);

                int stageExportFailures = ExportStageSummaries(reportsDirectory, logPath, sessionId, activePresetLabel, sessionRunType, sessionNote, sessionBuildLabel, testerTag);
                if (stageExportFailures > 0) {
                    return $"Latest: {summaryPath}\nArchive: {archivePath}\nStage summary export failures: {stageExportFailures}";
                }

                return $"Latest: {summaryPath}\nArchive: {archivePath}";
            }
            catch (IOException exception) {
                Debug.LogError($"RunLogSummaryExporter: Failed to export QA summary due to I/O error.\n{exception}");
                return $"QA summary export failed: {exception.Message}";
            }
            catch (UnauthorizedAccessException exception) {
                Debug.LogError($"RunLogSummaryExporter: Failed to export QA summary due to access error.\n{exception}");
                return $"QA summary export failed: {exception.Message}";
            }
        }

        private static string BuildMarkdown(string logPath, DateTime sessionStartedUtc, string sessionId, string activePresetLabel, string sessionRunType, string sessionNote, string sessionBuildLabel, string testerTag) {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Latest Playtest Summary");
            builder.AppendLine();
            builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Session ID: {sessionId}");
            builder.AppendLine($"Log Source: `{logPath}`");
            builder.AppendLine($"Active Preset: {(string.IsNullOrWhiteSpace(activePresetLabel) ? "none" : activePresetLabel)}");
            builder.AppendLine($"Session Run Type: {(string.IsNullOrWhiteSpace(sessionRunType) ? "unspecified" : sessionRunType)}");
            builder.AppendLine($"Session Note: {(string.IsNullOrWhiteSpace(sessionNote) ? "none" : sessionNote)}");
            builder.AppendLine($"Session Build: {(string.IsNullOrWhiteSpace(sessionBuildLabel) ? "dev-local" : sessionBuildLabel)}");
            builder.AppendLine($"Tester Tag: {(string.IsNullOrWhiteSpace(testerTag) ? "unknown" : testerTag)}");
            builder.AppendLine();
            builder.AppendLine("## Highlighted Signals");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine(RunLogAnalyzer.BuildRecentHighlights(logPath).TrimEnd());
            builder.AppendLine("```");
            builder.AppendLine();
            builder.AppendLine("## Session Overview");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine(RunLogAnalyzer.BuildSessionOverview(logPath, sessionStartedUtc).TrimEnd());
            builder.AppendLine("```");
            builder.AppendLine();
            builder.AppendLine("## Full History Overview");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine(RunLogAnalyzer.BuildOverview(logPath).TrimEnd());
            builder.AppendLine("```");
            builder.AppendLine();
            builder.AppendLine("## Recent Run Stats");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine(RunLogAnalyzer.BuildRecentStats(logPath).TrimEnd());
            builder.AppendLine("```");
            builder.AppendLine();
            builder.AppendLine("## Recent Runs");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine(RunLogAnalyzer.BuildRecentRuns(logPath).TrimEnd());
            builder.AppendLine("```");
            return builder.ToString();
        }

        private static int ExportStageSummaries(string reportsDirectory, string logPath, string sessionId, string activePresetLabel, string sessionRunType, string sessionNote, string sessionBuildLabel, string testerTag) {
            int failureCount = 0;

            for (int stageIndex = 1; stageIndex <= RunManager.DefaultStageCount; stageIndex++) {
                try {
                    string filePath = Path.Combine(reportsDirectory, $"STAGE_{stageIndex:00}_SUMMARY.md");
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine($"# Stage {stageIndex} Summary");
                    builder.AppendLine();
                    builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    builder.AppendLine($"Session ID: {sessionId}");
                    builder.AppendLine($"Active Preset: {(string.IsNullOrWhiteSpace(activePresetLabel) ? "none" : activePresetLabel)}");
                    builder.AppendLine($"Session Run Type: {(string.IsNullOrWhiteSpace(sessionRunType) ? "unspecified" : sessionRunType)}");
                    builder.AppendLine($"Session Note: {(string.IsNullOrWhiteSpace(sessionNote) ? "none" : sessionNote)}");
                    builder.AppendLine($"Session Build: {(string.IsNullOrWhiteSpace(sessionBuildLabel) ? "dev-local" : sessionBuildLabel)}");
                    builder.AppendLine($"Tester Tag: {(string.IsNullOrWhiteSpace(testerTag) ? "unknown" : testerTag)}");
                    builder.AppendLine();
                    builder.AppendLine("```text");
                    builder.AppendLine(RunLogAnalyzer.BuildStageSummary(logPath, stageIndex).TrimEnd());
                    builder.AppendLine("```");
                    File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);
                }
                catch (IOException exception) {
                    failureCount++;
                    Debug.LogError($"RunLogSummaryExporter: Failed to export Stage {stageIndex} summary due to I/O error.\n{exception}");
                }
                catch (UnauthorizedAccessException exception) {
                    failureCount++;
                    Debug.LogError($"RunLogSummaryExporter: Failed to export Stage {stageIndex} summary due to access error.\n{exception}");
                }
            }

            return failureCount;
        }
    }

}
