using ImageCapture.DesktopDuplication;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SharpDX;
using SharpDX.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using ImageCapture.ProcessDuplication;
using ImageCapture.Video;
using System.Diagnostics;
using System.Drawing;
using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
using TaskAutomation.Jobs;

class Program
{
    //static void Main(string[] args)
    //{
    //    Mat source = new Mat("C:\\Users\\fjsch\\Pictures\\Screenshots\\Screenshot 2025-05-22 202749.png");
    //    Mat template = new Mat("C:\\Users\\fjsch\\Pictures\\Screenshots\\Screenshot 2025-05-22 202830.png");

    //    TemplateMatching templateMatching = new TemplateMatching(TemplateMatchModes.CCoeffNormed);
    //    templateMatching.SetTemplate(template);
    //    templateMatching.SetMultiplePoints(true);

    //    var result = templateMatching.Detect(source);

    //    var output = DrawResult.DrawTemplateMatchingResult(source, result, template.Size());


    //    Console.WriteLine($"Success: {result.Success}");
    //    Console.WriteLine($"CenterPoint: {result.CenterPoint}");
    //    Console.WriteLine($"Confidence: {result.Confidence}");
    //    Console.WriteLine($"Points: {string.Join(", ", result.Points)}");
    //    ShowImage(output);
    //}

    static async Task Main(string[] args)
    {
        string jobFolderPath = "C:\\Users\\schlieper\\source\\repos\\ImageCapture\\TaskAutomation\\JobFiles";
        string _makroFolderPath = "C:\\Users\\schlieper\\source\\repos\\ImageCapture\\TaskAutomation\\MakroFiles";
        while(true)
        {
            Console.WriteLine("Wähle eine Job-Datei\n");

            JobExecutor jobExecutor = new JobExecutor();

            Job job = WaehleDateiAusVerzeichnis(jobExecutor);

            jobExecutor.SetMakroFilePath(_makroFolderPath);
            jobExecutor.ExecuteJob(job);     
        }
    }

    public static Job WaehleDateiAusVerzeichnis(JobExecutor executor)
    {
        Console.WriteLine("----------------------------------------------");
        // Dateien mit Nummer anzeigen
        int i = 0;
        foreach (var job in executor.AllJobs.Values)
        {
            Console.WriteLine($"{i + 1}: {job.Name}");
            i++;
        }
        Console.WriteLine("----------------------------------------------");

        int auswahl = -1;
        while (true)
        {
            Console.Write("Bitte die Nummer der gewünschten Datei eingeben: ");
            string eingabe = Console.ReadLine();

            if (int.TryParse(eingabe, out auswahl) &&
                auswahl >= 1 && auswahl <= executor.AllJobs.Count)
            {
                break;
            }

            Console.WriteLine("\nUngültige Eingabe. Bitte eine gültige Nummer eingeben.");
        }

        return executor.AllJobs.ElementAt(auswahl - 1).Value;
    }
}