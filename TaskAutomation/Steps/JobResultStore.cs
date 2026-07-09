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
            _byType[typeof(TStep)] = result;
            _byId[stepId]         = result;
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
            {
                switch (result)
                {
                    case CaptureResult cr:
                        cr.Image?.Dispose();
                        cr.ProcessedImage?.Dispose();
                        break;
                }
            }
            _byType.Clear();
            _byId.Clear();
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
