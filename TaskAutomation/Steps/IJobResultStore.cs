using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Speichert die Ergebnisse aller Steps einer Job-Ausführungsrunde.
    /// Gibt für nicht-ausgeführte Steps immer den typsicheren Default zurück –
    /// kein null, kein Crash, auch wenn der Step nicht im Job vorhanden ist.
    /// </summary>
    public interface IJobResultStore
    {
        /// <summary>
        /// Letztes Ergebnis des Steps vom Typ <typeparamref name="TStep"/>.
        /// Gibt <c>TResult.Default</c> zurück wenn der Step noch nicht gelaufen ist.
        /// </summary>
        TResult Get<TStep, TResult>()
            where TStep   : JobStep
            where TResult : StepResultBase;

        /// <summary>
        /// Ergebnis eines konkreten Steps anhand seiner Step-ID.
        /// Nützlich wenn mehrere Steps desselben Typs im Job vorhanden sind.
        /// Gibt <c>TResult.Default</c> zurück wenn der Step noch nicht gelaufen ist.
        /// </summary>
        TResult GetById<TResult>(string stepId)
            where TResult : StepResultBase;

        /// <summary>
        /// Schreibt das Ergebnis eines Steps (wird von der Handler-Basisklasse automatisch aufgerufen).
        /// </summary>
        void Set<TStep>(StepResultBase result, string stepId)
            where TStep : JobStep;
    }
}
