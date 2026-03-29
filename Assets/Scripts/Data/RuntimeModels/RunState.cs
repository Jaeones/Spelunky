using System;
using System.Collections.Generic;
using System.Text;

namespace Spelunky {

    [Serializable]
    public class RunStageLoadRequest {

        public int stageIndex;
        public string stageId;
        public string sceneName;
        public bool isFinalStage;

        public override string ToString() {
            return $"{stageId} (Stage {stageIndex}) -> scene '{sceneName}'";
        }

    }

    public enum RunProgressState {
        Active,
        Transitioning,
        Death,
        Clear,
        Restart
    }

    /// <summary>
    /// Serializable snapshot of a single run's persistent state across stages.
    /// Keep this intentionally small until stage-to-stage flow is wired up.
    /// </summary>
    [Serializable]
    public class RunState {

        public const int DefaultTotalStageCount = 4;
        public const float DefaultFinalEscapeTimeLimitSeconds = 45f;

        public string runId;
        public int currentStageIndex;
        public string currentStageId;
        public int totalStageCount;
        public int health;
        public int maxHealth;
        public int bombs;
        public int ropes;
        public int gold;
        public float elapsedTime;
        public float currentStageElapsedTime;
        public RunProgressState progressState;
        public bool isFinalEscapeActive;
        public float finalEscapeTriggeredAtStageTime;
        public float finalEscapeTimeLimitSeconds;
        public List<string> accessoryIds = new List<string>();

        public bool HasNextStage => currentStageIndex < totalStageCount;
        public bool IsRunEnded => progressState == RunProgressState.Death || progressState == RunProgressState.Clear || progressState == RunProgressState.Restart;

        public static string CreateStageId(int stageIndex) {
            return $"stage-{Math.Max(1, stageIndex):00}";
        }

        public static RunState CreateDefault() {
            return new RunState {
                runId = Guid.NewGuid().ToString("N"),
                currentStageIndex = 1,
                currentStageId = CreateStageId(1),
                totalStageCount = DefaultTotalStageCount,
                health = 4,
                maxHealth = 4,
                bombs = 4,
                ropes = 4,
                gold = 0,
                elapsedTime = 0f,
                currentStageElapsedTime = 0f,
                progressState = RunProgressState.Active,
                isFinalEscapeActive = false,
                finalEscapeTriggeredAtStageTime = 0f,
                finalEscapeTimeLimitSeconds = DefaultFinalEscapeTimeLimitSeconds,
                accessoryIds = new List<string>()
            };
        }

        public string ToDebugString() {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"RunId: {runId}");
            builder.AppendLine($"Stage: {currentStageIndex}/{totalStageCount}");
            builder.AppendLine($"StageId: {currentStageId}");
            builder.AppendLine($"State: {progressState}");
            builder.AppendLine($"Health: {health}/{maxHealth}");
            builder.AppendLine($"Bombs: {bombs}");
            builder.AppendLine($"Ropes: {ropes}");
            builder.AppendLine($"Gold: {gold}");
            builder.AppendLine($"Run Time: {elapsedTime:0.00}s");
            builder.AppendLine($"Stage Time: {currentStageElapsedTime:0.00}s");
            builder.AppendLine($"Final Escape Active: {isFinalEscapeActive}");
            if (isFinalEscapeActive) {
                float elapsedSinceTrigger = Math.Max(0f, currentStageElapsedTime - finalEscapeTriggeredAtStageTime);
                float remaining = Math.Max(0f, finalEscapeTimeLimitSeconds - elapsedSinceTrigger);
                builder.AppendLine($"Final Escape Triggered At: {finalEscapeTriggeredAtStageTime:0.00}s");
                builder.AppendLine($"Final Escape Remaining: {remaining:0.00}s");
            }
            builder.AppendLine($"Accessories: {(accessoryIds.Count > 0 ? string.Join(", ", accessoryIds) : "none")}");
            return builder.ToString();
        }

    }

}
