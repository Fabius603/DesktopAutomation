using System;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;

namespace ImageDetection.YOLO
{
    /// <summary>
    /// Hilfsklasse für DirectML-spezifische Konfiguration und Optimierungen.
    /// </summary>
    public static class DirectMLHelper
    {
        /// <summary>
        /// Erstellt optimierte SessionOptions für DirectML mit verschiedenen Fallback-Strategien.
        /// </summary>
        public static SessionOptions[] CreateDirectMLConfigurations(GraphOptimizationLevel optimization)
        {
            return new[]
            {
                // Konfiguration 1: Standard DirectML mit Optimierungen
                CreateStandardConfig(optimization),
                
                // Konfiguration 2: Device-Workaround für 80070057 Fehler (funktioniert meist)
                CreateDeviceWorkaroundConfig(optimization),
                
                // Konfiguration 3: Minimal DirectML-Konfiguration
                CreateMinimalConfig()
            };
        }

        private static SessionOptions CreateStandardConfig(GraphOptimizationLevel optimization)
        {
            var options = new SessionOptions
            {
                GraphOptimizationLevel = optimization,
                EnableMemoryPattern = false,  // Oft problematisch mit DirectML
                EnableCpuMemArena = false     // Kann DirectML-GPU Kommunikation stören
            };
            
            options.AppendExecutionProvider_DML(0); // Device 0
            return options;
        }

        private static SessionOptions CreateMinimalConfig()
        {
            var options = new SessionOptions();
            options.AppendExecutionProvider_DML(); // Ohne Device-ID
            return options;
        }

        /// <summary>
        /// Versucht DirectML mit verschiedenen Konfigurationen und gibt die erste funktionierende zurück.
        /// </summary>
        public static InferenceSession CreateDirectMLSession(string modelPath, GraphOptimizationLevel optimization, ILogger logger)
        {
            var configurations = CreateDirectMLConfigurations(optimization);
            Exception? lastException = null;

            for (int i = 0; i < configurations.Length; i++)
            {
                try
                {
                    using var config = configurations[i];
                    var session = new InferenceSession(modelPath, config);
                    logger.LogDebug("DirectML session created with configuration {ConfigIndex}", i + 1);
                    return session;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    logger.LogDebug("DirectML configuration {ConfigIndex} failed, trying next", i + 1);
                    continue;
                }
            }

            // Alle Konfigurationen fehlgeschlagen
            throw new InvalidOperationException(
                "DirectML konnte nicht initialisiert werden. Möglicherweise sind die GPU-Treiber veraltet.", 
                lastException);
        }

        /// <summary>
        /// Spezielle Workaround-Konfiguration für DirectML 80070057 Fehler.
        /// Verwendet explizite GPU-Device-Auswahl und reduzierte Memory-Features.
        /// </summary>
        private static SessionOptions CreateDeviceWorkaroundConfig(GraphOptimizationLevel optimization)
        {
            var options = new SessionOptions();
            options.GraphOptimizationLevel = optimization;
            options.EnableMemoryPattern = false; // Deaktiviert für Kompatibilität
            options.EnableCpuMemArena = false;   // Reduzierte Memory-Features
            
            try
            {
                // Versuche explizite Device-ID 0 (erste GPU)
                options.AppendExecutionProvider_DML(0);
            }
            catch
            {
                // Fallback ohne Device-ID
                options.AppendExecutionProvider_DML();
            }
            
            return options;
        }
    }
}
