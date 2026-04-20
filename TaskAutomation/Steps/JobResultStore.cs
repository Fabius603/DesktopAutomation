using System;
using System.Collections.Generic;
using System.Reflection;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    internal sealed class JobResultStore : IJobResultStore
    {
        private readonly Dictionary<Type, StepResultBase>   _byType = new();
        private readonly Dictionary<string, StepResultBase> _byId   = new(StringComparer.OrdinalIgnoreCase);

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

        // ── Interne Verwaltung ─────────────────────────────────────────────────

        /// <summary>
        /// Löscht alle gespeicherten Ergebnisse und gibt Bitmap-Ressourcen frei.
        /// Wird am Beginn jeder Wiederholungsrunde aufgerufen.
        /// </summary>
        internal void DisposeAndClear()
        {
            foreach (var result in _byType.Values)
            {
                switch (result)
                {
                    case CaptureResult cr:
                        cr.Image?.Dispose();
                        cr.ProcessedImage?.Dispose();
                        break;
                    case DetectionResult dr:
                        dr.ProcessedImage?.Dispose();
                        break;
                }
            }
            _byType.Clear();
            _byId.Clear();
        }

        // ── Default-Wert per Reflection ────────────────────────────────────────

        private static TResult GetDefault<TResult>() where TResult : StepResultBase
        {
            // Sucht das statische Default-Feld (z.B. CaptureResult.Default)
            var field = typeof(TResult).GetField("Default",
                BindingFlags.Public | BindingFlags.Static);

            if (field?.GetValue(null) is TResult def)
                return def;

            // Fallback: leere Instanz
            return (TResult)Activator.CreateInstance(typeof(TResult))!;
        }
    }
}
