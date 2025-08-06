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
using TaskAutomation.Makros;
using System.Linq;
using TaskAutomation.Steps;

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
        private Dictionary<string, Makro> _makros;
        private DxgiResources _dxgiResources { get; } = DxgiResources.Instance;
        private MakroExecutor _makroExecutor;
        private string _makroFolderPath = Path.Combine(AppContext.BaseDirectory, "MakroFiles");
        private string _jobFolderPath = Path.Combine(AppContext.BaseDirectory, "JobFiles");
        private Dictionary<string, Job> _allJobs = new Dictionary<string, Job>();
        private Dictionary<string, Makro> _allMakros = new Dictionary<string, Makro>();


        private readonly Dictionary<Type, IJobStepHandler> _stepHandlers = new()
        {
            { typeof(ProcessDuplicationStep), new ProcessDuplicationStepHandler() },
            { typeof(DesktopDuplicationStep), new DesktopDuplicationStepHandler() },
            { typeof(TemplateMatchingStep), new TemplateMatchingStepHandler() },
            { typeof(ShowImageStep), new ShowImageStepHandler() },
            { typeof(VideoCreationStep), new VideoCreationStepHandler() },
            { typeof(MakroExecutionStep), new MakroExecutionStepHandler() }
        };

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

        public DxgiResources DxgiResources => _dxgiResources;

        public MakroExecutor MakroExecutor
        {
            get => _makroExecutor;
            set => _makroExecutor = value;
        }

        public string MakroFolderPath
        {
            get => _makroFolderPath;
        }

        public string JobFolderPath
        {
            get => _jobFolderPath;
        }

        public Dictionary<string, Job> AllJobs
        {
            get => _allJobs;
        }

        public Dictionary<string, Makro> AllMakros
        {
            get => _allMakros;
        }

        public JobExecutor()
        {
            SetAllJobs();
            SetAllMakros();
        }

        private void SetAllJobs()
        {
            if (!Directory.Exists(JobFolderPath))
            {
                Console.WriteLine($"Das {JobFolderPath} Verzeichnis existiert nicht.");
            }

            string[] files = Directory.GetFiles(JobFolderPath);
            if (files.Length == 0)
            {
                Console.WriteLine("Keine Dateien im Verzeichnis gefunden.");
            }

            _allJobs = new Dictionary<string, Job>();

            foreach (string file in files)
            {
                try
                {
                    Job job = JobReader.ReadSteps(file);
                    if (_allJobs.ContainsKey(job.Name))
                    {
                        Console.WriteLine($"Warnung: Ein Job mit dem Namen '{job.Name}' existiert bereits. Der Job wird überschrieben.");
                    }
                    _allJobs[job.Name] = job;
                }
                catch
                {
                    Console.WriteLine($"Fehler beim Laden der Job-Datei: {file}. Bitte überprüfen Sie die Datei auf Korrektheit.");
                }
            }
        }

        private void SetAllMakros()
        {
            if (!Directory.Exists(MakroFolderPath))
            {
                Console.WriteLine($"Das {MakroFolderPath} Verzeichnis existiert nicht.");
            }

            string[] files = Directory.GetFiles(MakroFolderPath);
            if (files.Length == 0)
            {
                Console.WriteLine("Keine Dateien im Verzeichnis gefunden.");
            }

            foreach (string file in files)
            {
                try
                {
                    Makro makro = MakroReader.LadeMakroDatei(file);
                    _allMakros[makro.Name] = makro;
                }
                catch
                {
                    Console.WriteLine($"Fehler beim Laden der Makro-Datei: {file}. Bitte überprüfen Sie die Datei auf Korrektheit.");
                }
            }
        }

        public async void ExecuteJob(Job job)
        {
            if (job == null)
            {
                Console.WriteLine("Error: Job ist null.");
                return;
            }

            Console.WriteLine($"--- Job '{job.Name}' wird gestartet ---");

            var videoStep = job.Steps.OfType<VideoCreationStep>().FirstOrDefault();
            if (videoStep != null)
            {
                _videoRecorder = new StreamVideoRecorder(1920, 1080, 60);
                _videoRecorder.OutputDirectory = videoStep.SavePath;
                _videoRecorder.FileName = videoStep.FileName;
                await _videoRecorder.StartAsync();
                Console.WriteLine($"    VideoRecorder wurde gestartet");
            }

            if (job.Steps.OfType<MakroExecutionStep>().Any())
            {
                _makroExecutor = new MakroExecutor();

                _makros = new Dictionary<string, Makro>();
                foreach (var step in job.Steps.OfType<MakroExecutionStep>())
                {
                    string makroFilePath = Path.Combine(_makroFolderPath, step.MakroName + ".json");
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
                Console.WriteLine($"    Video wird unter {_videoRecorder.OutputDirectory} gespeichert");
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
            if (step == null)
                return false;

            if (_stepHandlers.TryGetValue(step.GetType(), out var handler))
            {
                return handler.Execute(step, jobContext, this);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"    WARNUNG: Unbekannter Step-Typ: {step.GetType().Name}");
            Console.ResetColor();
            return true;
        }

        public void SetMakroFilePath(string filePath)
        {
            if (Directory.Exists(filePath))
            {
                _makroFolderPath = filePath;
                Console.WriteLine($"    Makro-Ordnerpfad gesetzt: {_makroFolderPath}");
            }
            else
            {
                Console.WriteLine($"    FEHLER: Makro-Dateipfad '{filePath}' existiert nicht.");
            }
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