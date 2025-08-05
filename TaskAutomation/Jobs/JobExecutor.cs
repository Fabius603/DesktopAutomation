using System;
using System.IO;
using System.Collections.Generic;
using OpenCvSharp;
using ImageCapture.ProcessDuplication;
using ImageCapture.DesktopDuplication;
using ImageCapture.Video;
using System.Drawing;
using ImageDetection.Algorithms.TemplateMatching;
using OpenCvSharp.Extensions;
using ImageDetection;
using SharpDX;
using System.Text;
using Point = OpenCvSharp.Point;
using ImageHelperMethods;
using TaskAutomation.Makro;
using System.Linq;

namespace TaskAutomation.Jobs
{
    public class JobExecutor : IDisposable
    {
        private ProcessDuplicatorResult _processDuplicationResult;
        private DesktopFrame _currentDesktopFrame;
        private Bitmap _currentImage;
        private Mat _currentImageWithResult;
        private bool _disposed = false;
        private StreamVideoRecorder _videoRecorder;
        private ProcessDuplicator _processDuplicator;
        private DesktopDuplicator _desktopDuplicator;
        private TemplateMatching _templateMatcher;
        private TemplateMatchingResult _templateMatchingResult;
        private Mat _imageToProcess;
        private Point _currentOffset = new Point(0, 0);
        private int _currentDesktop = 0;
        private int _currentAdapter = 0;
        private Dictionary<string, MakroList> _makros;
        private DxgiResources dxgiResources { get; } = DxgiResources.Instance;
        private MakroExecutor makroExecutor;
        private string makroFolderPath = Path.Combine(AppContext.BaseDirectory, "MakroFiles");

        // Öffentliche Properties für den Zugriff von außen (z.B. Handler)
        public ProcessDuplicatorResult ProcessDuplicationResult
        {
            get => _processDuplicationResult;
            set => _processDuplicationResult = value;
        }

        public DesktopFrame CurrentDesktopFrame
        {
            get => _currentDesktopFrame;
            set => _currentDesktopFrame = value;
        }

        public Bitmap CurrentImage
        {
            get => _currentImage;
            set => _currentImage = value;
        }

        public Mat CurrentImageWithResult
        {
            get => _currentImageWithResult;
            set => _currentImageWithResult = value;
        }

        public StreamVideoRecorder VideoRecorder
        {
            get => _videoRecorder;
            set => _videoRecorder = value;
        }

        public ProcessDuplicator ProcessDuplicator
        {
            get => _processDuplicator;
            set => _processDuplicator = value;
        }

        public DesktopDuplicator DesktopDuplicator
        {
            get => _desktopDuplicator;
            set => _desktopDuplicator = value;
        }

        public TemplateMatching TemplateMatcher
        {
            get => _templateMatcher;
            set => _templateMatcher = value;
        }

        public TemplateMatchingResult TemplateMatchingResult
        {
            get => _templateMatchingResult;
            set => _templateMatchingResult = value;
        }

        public Mat ImageToProcess
        {
            get => _imageToProcess;
            set => _imageToProcess = value;
        }

        public Point CurrentOffset
        {
            get => _currentOffset;
            set => _currentOffset = value;
        }

        public int CurrentDesktop
        {
            get => _currentDesktop;
            set => _currentDesktop = value;
        }

        public int CurrentAdapter
        {
            get => _currentAdapter;
            set => _currentAdapter = value;
        }

        public Dictionary<string, MakroList> Makros
        {
            get => _makros;
            set => _makros = value;
        }

        public DxgiResources DxgiResources => dxgiResources;

        public MakroExecutor MakroExecutor
        {
            get => makroExecutor;
            set => makroExecutor = value;
        }

        public string MakroFolderPath
        {
            get => makroFolderPath;
            set => makroFolderPath = value;
        }
        public async void ExecuteJob(Job job)
        {
            if (job == null)
            {
                Console.WriteLine("Error: Job ist null.");
                return;
            }

            Console.WriteLine($"--- Job '{job.Name}' wird gestartet ---");

            if(job.Steps.OfType<VideoCreationStep>().Any())
            {
                _videoRecorder = new StreamVideoRecorder(1920, 1080, 60);
                await _videoRecorder.StartAsync();
                Console.WriteLine("    VideoRecorder wurde gestartet.");
            }

            if(job.Steps.OfType<MakroExecutionStep>().Any())
            {
                makroExecutor = new MakroExecutor();

                _makros = new Dictionary<string, MakroList>();
                foreach (var step in job.Steps.OfType<MakroExecutionStep>())
                {
                    string makroFilePath = Path.Combine(makroFolderPath, step.MakroName + ".json");
                    _makros[step.MakroName] = MakroReader.LadeMakroDatei(makroFilePath);
                }
            }

            bool continueJob = true;
            do
            {
                _processDuplicationResult?.Dispose(); // Aufräumen vom vorherigen Durchlauf (falls repeating)
                _processDuplicationResult = null;

                foreach (var step_object in job.Steps)
                {
                    Console.WriteLine($"--> Step: {step_object.GetType().Name}");
                    try
                    {
                        if (!ExecuteStep(step_object, job)) // ExecuteStep gibt false zurück, falls der Job abgebrochen werden soll
                        {
                            continueJob = false;
                            Console.WriteLine($"Job '{job.Name}' wurde durch einen Step vorzeitig beendet.");
                            break; // Inneren Loop (Steps) verlassen
                        }
                    }
                    catch (NotImplementedException nie)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"    WARNUNG: Schritt '{step_object.GetType().Name}' ist nicht vollständig implementiert: {nie.Message}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"    FEHLER beim Ausführen von Step '{step_object.GetType().Name}': {ex.Message}");
                        Console.WriteLine($"    StackTrace: {ex.StackTrace}");
                        Console.ResetColor();
                        continueJob = false; 
                        break;
                    }
                }

                if (!continueJob) break; // Äußeren Loop (repeating) verlassen

                if (job.Repeating)
                {
                    if(Console.KeyAvailable)
                    {
                        job.Repeating = false; // Stoppt die Wiederholung
                    }
                }

            } while (job.Repeating && continueJob);

            if (job.Steps.OfType<VideoCreationStep>().Any())
            {
                _videoRecorder.StopAndSave();
                Console.WriteLine($"    Video wird unter {_videoRecorder.GetOutputPath()} gespeichert");
            }

            // Finale Aufräumarbeiten für den Job
            _processDuplicationResult?.Dispose();
            _processDuplicationResult = null;

            _videoRecorder?.Dispose(); // VideoWriter schließen, falls er geöffnet war
            _videoRecorder = null;

            Cv2.DestroyAllWindows();
            Console.WriteLine($"--- Job '{job.Name}' beendet ---");
        }

        /// <summary>
        /// Führt einen einzelnen Job-Schritt aus.
        /// </summary>
        /// <param name="step">Das Schrittobjekt.</param>
        /// <param name="jobContext">Der aktuelle Job-Kontext (für übergreifende Infos wie Repeating).</param>
        /// <returns>True, wenn der Job fortgesetzt werden soll, false bei Abbruch.</returns>
        private bool ExecuteStep(object step, Job jobContext)
        {
            switch (step)
            {
                case ProcessDuplicationStep pdStep:
                    return HandleProcessDuplicationStep(pdStep);
                case DesktopDuplicationStep ddStep:
                    return HandleDesktopDuplicationStep(ddStep);
                case TemplateMatchingStep tmStep:
                    return HandleTemplateMatchingStep(tmStep);
                case ShowImageStep siStep:
                    return HandleShowImageStep(siStep);
                case VideoCreationStep vcStep:
                    return HandleVideoCreationStep(vcStep);
                case MakroExecutionStep miStep:
                    return HandleMakroExecutionStep(miStep);
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    WARNUNG: Unbekannter Step-Typ: {step.GetType().Name}");
                    Console.ResetColor();
                    return true; // Unbekannte Steps ignorieren und weitermachen
            }
        }

        // --- Handler-Methoden für jeden Step-Typ ---

        private bool HandleMakroExecutionStep(MakroExecutionStep step)
        {
            if(_currentAdapter == null)
            {
                Console.WriteLine("    FEHLER: Kein aktueller Adapter gesetzt (_currentAdapter ist null). Step wird übersprungen.");
                return true; // Oder false, wenn der Job abbrechen soll
            }
            if (_currentDesktop == null)
            {
                Console.WriteLine("    FEHLER: Kein aktueller Desktop gesetzt (_currentDesktop ist null). Step wird übersprungen.");
                return true; // Oder false, wenn der Job abbrechen soll
            }

            makroExecutor.ExecuteMakro(_makros[step.MakroName], _currentAdapter, _currentDesktop, dxgiResources);
            Console.WriteLine($"    Makro '{step.MakroName}' wurde ausgeführt auf Adapter {_currentAdapter} und Desktop {_currentDesktop}.");
            return true;
        }

        private bool HandleProcessDuplicationStep(ProcessDuplicationStep step)
        {
            _processDuplicationResult?.Dispose(); // Vorherigen Frame freigeben
            _currentImage?.Dispose(); // Vorheriges Desktop-Bild freigeben
            if(_processDuplicator == null)
            {
                _processDuplicator = new ProcessDuplicator(step.ProcessName);
                Console.WriteLine($"    Prozessfenster-Aufnahme gestartet für: '{step.ProcessName}'");
            }
            
            _processDuplicationResult = _processDuplicator.CaptureProcess(); 
            if (!_processDuplicationResult.ProcessFound)
            {
                Console.WriteLine($"    FEHLER: Prozess {step.ProcessName} konnte nicht gefunden werden.");
                return true; // Oder false, wenn der Job abbrechen soll
            }
            Console.WriteLine($"    Prozessfenster aufgenommen!");

            _currentDesktop = _processDuplicationResult.DesktopIdx;
            _currentAdapter = _processDuplicationResult.AdapterIdx;
            _currentImage = _processDuplicationResult.ProcessImage.Clone() as Bitmap;
            _currentOffset = _processDuplicationResult.WindowOffsetOnDesktop;
            return true;
        }

        private bool HandleDesktopDuplicationStep(DesktopDuplicationStep step)
        {
            _currentImage?.Dispose();
            _currentDesktopFrame?.Dispose();
            if (_desktopDuplicator == null)
            {
                _desktopDuplicator = new DesktopDuplicator(step.GraphicsCardAdapter, step.OutputDevice);
                Console.WriteLine($"    Desktop-Aufnahme gestartet für: Adapter {step.GraphicsCardAdapter}, Output {step.OutputDevice}");
            }
            try
            {
                _currentDesktopFrame = _desktopDuplicator.GetLatestFrame();
                _currentDesktop = step.OutputDevice;
                _currentAdapter = step.GraphicsCardAdapter;
                _currentImage = _currentDesktopFrame?.DesktopImage?.Clone() as Bitmap;
            }
            catch
            {
                Console.WriteLine($"    FEHLER: Desktop konnte nicht erfasst werden.");
                return true;
            }

            return true;
        }

        private bool HandleTemplateMatchingStep(TemplateMatchingStep step)
        {
            _templateMatchingResult?.Dispose();

            if (_templateMatcher == null)
            {
                _templateMatcher = new TemplateMatching(step.TemplateMatchMode);
                Console.WriteLine($"    Template Matching gestartet mit Modus: {step.TemplateMatchMode}");
            }

            if (_currentImage == null)
            {
                Console.WriteLine("    FEHLER: Kein Bild für Template Matching vorhanden (_currentFrame ist leer). Step wird übersprungen.");
                return true;
            }
            // ROI
            _templateMatcher.SetROI(step.ROI);
            if(step.EnableROI)
            {
                _templateMatcher.EnableROI();
            }
            else
            {
                _templateMatcher.DisableROI();
            }

            // Multiple Points
            if (step.MultiplePoints)
            {
                _templateMatcher.EnableMultiplePoints();
            }
            else
            {
                _templateMatcher.DisableMultiplePoints();
            }

            // Template
            _templateMatcher.SetTemplate(step.TemplatePath);

            // Threshold
            _templateMatcher.SetThreshold(step.ConfidenceThreshold);

            _imageToProcess = _currentImage.ToMat();
            _templateMatchingResult = _templateMatcher.Detect(_imageToProcess, _currentOffset);

            Console.WriteLine($"    Ergebnis: Erfolg={_templateMatchingResult.Success}, Konfidenz={_templateMatchingResult.Confidence:F2}%");

            if (_templateMatchingResult.Success)
            {
                if (step.DrawResults)
                {
                    _currentImageWithResult = DrawResult.DrawTemplateMatchingResult(_imageToProcess, _templateMatchingResult, _templateMatchingResult.TemplateSize);
                    Console.WriteLine("    Ergebnis wurde auf das Bild gemalt");
                }
            }
            return true;
        }

        public void SetMakroFilePath(string filePath)
        {
            if (Directory.Exists(filePath))
            {
                makroFolderPath = filePath;
                Console.WriteLine($"    Makro-Ordnerpfad gesetzt: {makroFolderPath}");
            }
            else
            {
                Console.WriteLine($"    FEHLER: Makro-Dateipfad '{filePath}' existiert nicht.");
            }
        }
        private bool HandleShowImageStep(ShowImageStep step)
        {
            if (_currentImage == null)
            {
                Console.WriteLine("    FEHLER: Kein Bild zum Anzeigen vorhanden (_currentFrame ist leer). Step wird übersprungen.");
                return true;
            }

            void ShowBitmapImage(Bitmap bitmap, string name)
            {
                var mat = bitmap.ToMat();
                ShowMatImage(mat, name);
            }

            void ShowMatImage(Mat mat, string name)
            {
                Cv2.Resize(mat, mat, new OpenCvSharp.Size(), 0.5, 0.5);
                Cv2.ImShow(name, mat);
                Cv2.WaitKey(1);
            }

            if(step.ShowRawImage)
            {
                string windowName = $"{step.WindowName} - Raw Image";
                Console.WriteLine($"    Bild anzeigen: Fenster='{windowName}'");
                ShowBitmapImage(_currentImage, windowName);
            }
            if(step.ShowProcessedImage)
            {
                string windowName = $"{step.WindowName} - Processed Image";
                Console.WriteLine($"    Bild anzeigen: Fenster='{windowName}'");
                if(_currentImageWithResult != null && !_currentImageWithResult.IsDisposed && _currentImageWithResult.Height >= 10 && _currentImageWithResult.Width >= 10)
                {
                    ShowMatImage(_currentImageWithResult, windowName);
                }
                else
                {
                    if(_currentImage != null)
                    {
                        ShowBitmapImage(_currentImage, windowName);
                    }
                }
            }
            return true;
        }

        private bool HandleVideoCreationStep(VideoCreationStep step)
        {
            if(_videoRecorder == null)
            {
                Console.WriteLine("    FEHLER: VideoRecorder ist null");
                return false;
            }
            if(step.SavePath != string.Empty && step.SavePath != null)
            {
                _videoRecorder.SetOutputPath(step.SavePath);
            }
            if(step.FileName != string.Empty && step.FileName != null)
            {
                _videoRecorder.SetFileName(step.FileName);
            }


            if (step.ShowRawImage)
            {
                _videoRecorder.AddFrame(_currentImage.Clone() as Bitmap);
                Console.WriteLine("    Bild wird in Video eingefügt!");
            }
            else if(step.ShowProcessedImage)
            {
                if(_currentImageWithResult != null && !_currentImageWithResult.IsDisposed)
                {
                    _videoRecorder.AddFrame(_currentImageWithResult.ToBitmap().Clone() as Bitmap);
                }
                else
                {
                    _videoRecorder.AddFrame(_currentImage.Clone() as Bitmap);
                }
                Console.WriteLine("    Bild wird in Video eingefügt!");
            }

            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _processDuplicationResult?.Dispose();
                    _currentImage?.Dispose();
                    _videoRecorder?.Dispose();
                    _currentDesktopFrame?.Dispose();
                    _currentImageWithResult?.Dispose();
                    _processDuplicator?.Dispose();
                    _desktopDuplicator?.Dispose();
                    _templateMatcher?.Dispose();
                    _templateMatchingResult?.Dispose();
                    _imageToProcess?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}