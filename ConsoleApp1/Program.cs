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
using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;
using Common.Logging;
using TaskAutomation.Hotkeys;
using TaskAutomation.Orchestration;
using DesktopOverlay;
using GameOverlay.Drawing;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        //var overlay = new Overlay(0, 0, 800, 600);
        //overlay.AddItem(new TextItem(
        //    id: "welcome",
        //    fontName: "Consolas",
        //    fontSize: 16,
        //    text: "Willkommen im Desktop-Overlay!",
        //    color: new Color(255, 255, 255, 255),
        //    x: 50, y: 50
        //));

        //overlay.AddItem(new RectangleItem(
        //    id: "sampleBox",
        //    fillColor: new Color(255, 0, 0, 128),
        //    strokeColor: new Color(0, 255, 0, 255),
        //    strokeWidth: 3f,
        //    left: 100, top: 100, right: 300, bottom: 250
        //));

        //overlay.RunInNewThread();

        //overlay.AddItem(new RectangleItem(
        //    id: "box",
        //    fillColor: new Color(0, 128, 255, 128),
        //    strokeColor: new Color(255, 255, 255, 255),
        //    strokeWidth: 2f,
        //    left: 300, top: 300, right: 700, bottom: 600
        //));

        string jobFolderPath = "C:\\Users\\schlieper\\source\\repos\\ImageCapture\\TaskAutomation\\Configs\\Job";
        string makroFolderPath = "C:\\Users\\schlieper\\source\\repos\\ImageCapture\\TaskAutomation\\Configs\\Makro";
        string hotkeyFolderPath = "C:\\Users\\schlieper\\source\\repos\\ImageCapture\\TaskAutomation\\Configs\\Hotkey";

        Console.WriteLine("Starte TaskAutomation...");

        //JobExecutor jobExecutor = new JobExecutor();
        //GlobalHotkeyService globalHotkeyService = GlobalHotkeyService.Instance;
        //JobDispatcher jobDispatcher = new JobDispatcher(globalHotkeyService, jobExecutor);

        Console.WriteLine("Deine Hotkeys sind jetzt aktiv");

        Console.WriteLine("Hotkeys aktiv. Drücke eine beliebige Taste, um das Programm zu beenden.");
        Console.ReadKey();
    }
}