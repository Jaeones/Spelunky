using System;
using System.Collections.Generic;
using System.Text;

namespace Spelunky {

    [Serializable]
    public class RunResult {

        public string runId;
        public string startedAtUtc;
        public string endedAtUtc;
        public int finalStageIndex;
        public float totalDurationSeconds;
        public int finalHealth;
        public int finalBombs;
        public int finalRopes;
        public int finalGold;
        public string endReason;
        public string finalDeathCause;
        public List<StageRunResult> stageResults = new List<StageRunResult>();

        public string ToSummaryString() {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Run {runId} ended: {endReason}");
            builder.AppendLine($"Final stage: {finalStageIndex}");
            builder.AppendLine($"Total time: {totalDurationSeconds:0.00}s");
            builder.AppendLine($"Final resources: HP {finalHealth}, Bombs {finalBombs}, Ropes {finalRopes}, Gold {finalGold}");

            if (!string.IsNullOrWhiteSpace(finalDeathCause)) {
                builder.AppendLine($"Death cause: {finalDeathCause}");
            }

            for (int i = 0; i < stageResults.Count; i++) {
                StageRunResult stage = stageResults[i];
                builder.AppendLine(
                    $"Stage {stage.stageIndex}: {stage.outcome} in {stage.durationSeconds:0.00}s" +
                    (string.IsNullOrWhiteSpace(stage.deathCause) ? string.Empty : $" ({stage.deathCause})")
                );
            }

            return builder.ToString();
        }
    }

    [Serializable]
    public class StageRunResult {

        public int stageIndex;
        public string sceneName;
        public float durationSeconds;
        public string outcome;
        public string deathCause;
    }

}
