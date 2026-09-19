using System;
using System.Collections.Generic;

namespace GemforgeCascade.Core
{
    public enum ObjectiveKind
    {
        Score,
        CollectColor,
        ClearLayers
    }

    public sealed class ObjectiveProgress
    {
        public ObjectiveKind Kind { get; }
        public int ColorIndex { get; }
        public int Target { get; }
        public int Current { get; internal set; }
        public bool Complete => Current >= Target;

        internal ObjectiveProgress(ObjectiveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!Enum.IsDefined(typeof(ObjectiveKind), data.kind))
                throw new InvalidOperationException($"Unknown objective kind {data.kind}.");
            if (data.target <= 0)
                throw new InvalidOperationException("Objective target must be positive.");

            Kind = (ObjectiveKind)data.kind;
            ColorIndex = data.colorIndex;
            Target = data.target;
        }
    }

    public sealed class ObjectiveTracker
    {
        private readonly List<ObjectiveProgress> objectives = new List<ObjectiveProgress>();

        public IReadOnlyList<ObjectiveProgress> Objectives => objectives;
        public bool Complete
        {
            get
            {
                if (objectives.Count == 0)
                    return false;
                foreach (ObjectiveProgress objective in objectives)
                    if (!objective.Complete)
                        return false;
                return true;
            }
        }

        public ObjectiveTracker(IEnumerable<ObjectiveData> definitions, int colorCount)
        {
            if (definitions == null)
                return;

            foreach (ObjectiveData definition in definitions)
            {
                var objective = new ObjectiveProgress(definition);
                if (objective.Kind == ObjectiveKind.CollectColor &&
                    (objective.ColorIndex < 0 || objective.ColorIndex >= colorCount))
                    throw new InvalidOperationException("Collect-color objective uses an invalid color.");
                objectives.Add(objective);
            }
        }

        public void Apply(ClearResult clearResult, int totalScore)
        {
            if (clearResult == null)
                throw new ArgumentNullException(nameof(clearResult));

            foreach (ObjectiveProgress objective in objectives)
            {
                switch (objective.Kind)
                {
                    case ObjectiveKind.Score:
                        objective.Current = Math.Min(totalScore, objective.Target);
                        break;
                    case ObjectiveKind.CollectColor:
                        objective.Current = Math.Min(
                            objective.Current + clearResult.RemovedColor(objective.ColorIndex),
                            objective.Target);
                        break;
                    case ObjectiveKind.ClearLayers:
                        objective.Current = Math.Min(
                            objective.Current + clearResult.LayersCleared,
                            objective.Target);
                        break;
                }
            }
        }
    }
}
