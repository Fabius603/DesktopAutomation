using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using OpenCvSharp.Extensions;

namespace TaskAutomation.Steps
{
    public class TemplateMatchingStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutionContext executor, CancellationToken ct)
        {
            var tmStep = step as TemplateMatchingStep;
            if (tmStep == null)
            {
                return false;
            }

            executor.TemplateMatchingResult?.Dispose();

            if (executor.TemplateMatcher == null)
            {
                executor.TemplateMatcher = new TemplateMatching(tmStep.Settings.TemplateMatchMode);
            }

            if (executor.CurrentImage == null)
            {
                return true;
            }

            executor.TemplateMatcher.SetROI(tmStep.Settings.ROI);
            if (tmStep.Settings.EnableROI)
                executor.TemplateMatcher.EnableROI();
            else
                executor.TemplateMatcher.DisableROI();

            if (tmStep.Settings.MultiplePoints)
                executor.TemplateMatcher.EnableMultiplePoints();
            else
                executor.TemplateMatcher.DisableMultiplePoints();

            executor.TemplateMatcher.SetTemplate(tmStep.Settings.TemplatePath);
            executor.TemplateMatcher.SetThreshold(tmStep.Settings.ConfidenceThreshold);

            executor.ImageToProcess = executor.CurrentImage.ToMat();
            executor.TemplateMatchingResult = executor.TemplateMatcher.Detect(executor.ImageToProcess, executor.CurrentOffset);


            if (executor.TemplateMatchingResult.Success && tmStep.Settings.DrawResults)
            {
                executor.CurrentImageWithResult = DrawResult.DrawTemplateMatchingResult(
                    executor.ImageToProcess,
                    executor.TemplateMatchingResult,
                    executor.TemplateMatchingResult.TemplateSize);
            }

            return true;
        }
    }
}
