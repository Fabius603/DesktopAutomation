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
        string jobFolderPath = "C:\\Users\\fjsch\\source\\repos\\ImageCapture\\TaskAutomation\\JobFiles";
        string makroFolderPath = "C:\\Users\\fjsch\\source\\repos\\ImageCapture\\TaskAutomation\\MakroFiles";
        while(true)
        {
            Console.WriteLine("Wähle eine Job-Datei\n");
            string filePath = WaehleDateiAusVerzeichnis(jobFolderPath);
            var job = JobReader.ReadSteps(filePath);

            JobExecutor jobExecutor = new JobExecutor();
            jobExecutor.SetMakroFilePath(makroFolderPath);
            jobExecutor.ExecuteJob(job);     
        }
    }

    public static string WaehleDateiAusVerzeichnis(string verzeichnisPfad)
    {
        if (!Directory.Exists(verzeichnisPfad))
        {
            Console.WriteLine("Das Verzeichnis existiert nicht.");
            return null;
        }

        string[] dateien = Directory.GetFiles(verzeichnisPfad);
        if (dateien.Length == 0)
        {
            Console.WriteLine("Keine Dateien im Verzeichnis gefunden.");
            return null;
        }
        Console.WriteLine("----------------------------------------------");
        // Dateien mit Nummer anzeigen
        for (int i = 0; i < dateien.Length; i++)
        {
            Console.WriteLine($"{i + 1}: {Path.GetFileName(dateien[i])}");
        }
        Console.WriteLine("----------------------------------------------");

        int auswahl = -1;
        while (true)
        {
            Console.Write("Bitte die Nummer der gewünschten Datei eingeben: ");
            string eingabe = Console.ReadLine();

            if (int.TryParse(eingabe, out auswahl) &&
                auswahl >= 1 && auswahl <= dateien.Length)
            {
                break;
            }

            Console.WriteLine("\nUngültige Eingabe. Bitte eine gültige Nummer eingeben.");
        }

        return dateien[auswahl - 1];
    }
}