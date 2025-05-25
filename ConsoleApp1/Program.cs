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
        //string targetApplication = "brave";
        //ProcessDuplicator processDuplicator = new ProcessDuplicator(targetApplication);

        //Mat template = new Mat("C:\\Users\\fjsch\\Pictures\\Screenshots\\Screenshot 2025-05-22 202830.png");

        ////Recorder
        ////var recorder = new StreamVideoRecorder(1920, 1080, 60);
        ////await recorder.StartAsync();

        //TemplateMatching templateMatching = new TemplateMatching(TemplateMatchModes.SqDiffNormed);
        //templateMatching.SetTemplate(template);
        //templateMatching.EnableMultiplePoints();
        //templateMatching.SetROI(new Rect(0, 300, 400, 400));
        //templateMatching.EnableROI();
        //templateMatching.SetThreshold(0.99);

        //Mat imageToProcess = new Mat();

        ////VideoImageCreator videoCreator = new VideoImageCreator();
        ////videoCreator.CleanUp();
        //Stopwatch stopwatch = new Stopwatch();
        //while (true)
        //{
        //    using (var result = processDuplicator.CaptureProcess())
        //    {
        //        stopwatch.Restart();
        //        //recorder.AddFrame(result.ProcessImage.Clone() as Bitmap);
        //        imageToProcess = result.ProcessImage.ToMat();
        //        var matchingResult = templateMatching.Detect(imageToProcess);
        //        var resultImage = DrawResult.DrawTemplateMatchingResult(imageToProcess, matchingResult, template.Size());

        //        //videoCreator.AddFrame(result.ProcessImage);
        //        //ShowImage(result.ProcessImage);
        //        stopwatch.Stop();
        //        ShowImage(resultImage);
        //        imageToProcess.Dispose();
        //        matchingResult.Dispose();
        //        //resultImage.Dispose();
        //    }
        //    Console.WriteLine($"Frame took {stopwatch.ElapsedMilliseconds} ms");

        //    if (Console.KeyAvailable)
        //    {
        //        Console.WriteLine("Video wird gespeichert...");
        //        Console.ReadKey(true);
        //        break;
        //    }

        //}
        ////recorder.StopAndSave();

        //processDuplicator.Dispose();
        ////recorder.Dispose();
        //Cv2.DestroyAllWindows();

        ////videoCreator.SaveAsMp4Async("C:\\Users\\fjsch\\Pictures\\trash\\output.mp4", 30).Wait();
        ////videoCreator.CleanUp();
    }

    public static void ShowImage(Bitmap bitmap)
    {
        var mat = bitmap.ToMat();
        ShowImage(mat);
    }

    public static void ShowImage(Mat mat)
    {
        Cv2.Resize(mat, mat, new OpenCvSharp.Size(), 0.7, 0.7);
        Cv2.ImShow("test", mat);
        Cv2.WaitKey(1);
        mat.Dispose();
    }
}