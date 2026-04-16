using System.Collections.Generic;
using System.Drawing;

namespace ImageDetection.Model
{
    public interface IDetectionResult
    {
        bool Success { get; set; }
        Point CenterPoint { get; set; }
        Rectangle? BoundingBox { get; set; }
        float Confidence { get; set; }

        /// <summary>
        /// Alle Treffer über dem Threshold, absteigend nach Confidence sortiert.
        /// Index 0 ist das beste Ergebnis (= dieses Objekt selbst).
        /// </summary>
        IReadOnlyList<IDetectionResult> AllResults { get; }
    }
}
