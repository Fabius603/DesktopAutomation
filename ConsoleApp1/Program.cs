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
using TaskAutomation;

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
        var job = JobReader.ReadSteps("C:\\Users\\fjsch\\source\\repos\\ImageCapture\\TaskAutomation\\Task.json");

        JobExecutor jobExecutor = new JobExecutor();
        jobExecutor.ExecuteJob(job);     
    }
}