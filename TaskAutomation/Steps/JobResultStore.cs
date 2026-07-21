using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    internal sealed class JobResultStore : IJobResultStore
    {
        private readonly Dictionary<Type, StepResultBase>   _byType = new();
        private readonly Dictionary<string, StepResultBase> _byId   = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Type> _stepTypesById = new(StringComparer.OrdinalIgnoreCase);

        public JobResultStore() { }

        // ── Lesen ──────────────────────────────────────────────────────────────

        public TResult Get<TStep, TResult>()
            where TStep   : JobStep
            where TResult : StepResultBase
        {
            if (_byType.TryGetValue(typeof(TStep), out var r) && r is TResult typed)
                return typed;
            return GetDefault<TResult>();
        }

        public TResult GetById<TResult>(string stepId)
            where TResult : StepResultBase
        {
            if (_byId.TryGetValue(stepId, out var r) && r is TResult typed)
                return typed;
            return GetDefault<TResult>();
        }

        // ── Schreiben (aufgerufen von JobStepHandler<TStep,TResult>) ───────────

        public void Set<TStep>(StepResultBase result, string stepId)
            where TStep : JobStep
        {
            _byType.TryGetValue(typeof(TStep), out var previousByType);
            _byId.TryGetValue(stepId, out var previousById);

            _byType[typeof(TStep)] = result;
            _byId[stepId]         = result;
            _stepTypesById[stepId] = typeof(TStep);

            DisposeIfUnreferenced(previousByType, result);
            if (!ReferenceEquals(previousById, previousByType))
                DisposeIfUnreferenced(previousById, result);
        }

        public StepResultBase? GetRaw(string stepId)
            => _byId.TryGetValue(stepId, out var r) ? r : null;

        // ── Interne Verwaltung ─────────────────────────────────────────────────

        /// <summary>
        /// Löscht alle gespeicherten Ergebnisse und gibt Bitmap-Ressourcen frei.
        /// Wird am Beginn jeder Wiederholungsrunde aufgerufen.
        /// </summary>
        internal void DisposeAndClear()
        {
            foreach (var result in _byType.Values.Concat(_byId.Values).Distinct(ReferenceEqualityComparer.Instance))
                DisposeResult(result);
            _byType.Clear();
            _byId.Clear();
            _stepTypesById.Clear();
        }

        /// <summary>Behält nur Ergebnisse der angegebenen Steps und gibt alle übrigen Ressourcen frei.</summary>
        internal void RetainOnly(IEnumerable<string> stepIds)
        {
            var retainedIds = stepIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var removed = _byId
                .Where(pair => !retainedIds.Contains(pair.Key))
                .Select(pair => pair.Value)
                .Distinct(ReferenceEqualityComparer.Instance)
                .ToList();

            foreach (var id in _byId.Keys.Where(id => !retainedIds.Contains(id)).ToList())
            {
                _byId.Remove(id);
                _stepTypesById.Remove(id);
            }

            _byType.Clear();
            foreach (var pair in _byId)
                if (_stepTypesById.TryGetValue(pair.Key, out var stepType))
                    _byType[stepType] = pair.Value;

            foreach (var result in removed)
                if (!_byId.Values.Any(value => ReferenceEquals(value, result)))
                    DisposeResult(result);
        }

        private void DisposeIfUnreferenced(StepResultBase? previous, StepResultBase replacement)
        {
            if (previous == null || ReferenceEquals(previous, replacement))
                return;

            bool stillReferenced = _byType.Values.Any(value => ReferenceEquals(value, previous))
                || _byId.Values.Any(value => ReferenceEquals(value, previous));
            if (!stillReferenced)
                DisposeResult(previous);
        }

        private static void DisposeResult(StepResultBase result)
        {
            if (result is not ICaptureStepResult capture)
                return;

            capture.Image?.Dispose();
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<StepResultBase>
        {
            public static readonly ReferenceEqualityComparer Instance = new();

            public bool Equals(StepResultBase? x, StepResultBase? y)
                => ReferenceEquals(x, y);

            public int GetHashCode(StepResultBase obj)
                => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        // ── Default-Wert per Reflection (gecacht pro Typ) ──────────────────────

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, StepResultBase>
            _defaultCache = new();

        private static TResult GetDefault<TResult>() where TResult : StepResultBase
        {
            return (TResult)_defaultCache.GetOrAdd(typeof(TResult), static t =>
            {
                var field = t.GetField("Default", BindingFlags.Public | BindingFlags.Static);
                if (field?.GetValue(null) is StepResultBase def)
                    return def;

                return (StepResultBase)Activator.CreateInstance(t)!;
            });
        }
    }
}
