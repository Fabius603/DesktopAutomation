using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using OpenCvSharp.Extensions;
using ImageHelperMethods;
using System.Drawing;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public class TemplateMatchingStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;
            
            if (step is not TemplateMatchingStep tmStep)
            {
                var errorMessage = $"Invalid step type - expected TemplateMatchingStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("TemplateMatchingStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("TemplateMatchingStepHandler: Processing template matching with template '{TemplatePath}'", tmStep.Settings.TemplatePath);

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(tmStep.Settings.TemplatePath))
                {
                    var errorMessage = "No template path specified";
                    logger.LogWarning("TemplateMatchingStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                if (!File.Exists(tmStep.Settings.TemplatePath))
                {
                    var errorMessage = $"Template file not found: '{tmStep.Settings.TemplatePath}'";
                    logger.LogError("TemplateMatchingStepHandler: {ErrorMessage}", errorMessage);
                    throw new FileNotFoundException(errorMessage);
                }

                if (executor.CurrentImage == null)
                {
                    var errorMessage = "No current image available for template matching";
                    logger.LogWarning("TemplateMatchingStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                executor.TemplateMatchingResult?.Dispose();

                if (executor.TemplateMatcher == null)
                {
                    executor.TemplateMatcher = new TemplateMatching(tmStep.Settings.TemplateMatchMode);
                    logger.LogDebug("TemplateMatchingStepHandler: Created new TemplateMatcher with mode {MatchMode}", tmStep.Settings.TemplateMatchMode);
                }

                executor.TemplateMatcher.SetROI(tmStep.Settings.ROI);
                if (tmStep.Settings.EnableROI)
                {
                    executor.TemplateMatcher.EnableROI();
                    logger.LogDebug("TemplateMatchingStepHandler: ROI enabled with bounds {ROI}", tmStep.Settings.ROI);
                }
                else
                {
                    executor.TemplateMatcher.DisableROI();
                }

                // Always use single point detection (MultiplePoints = false)
                executor.TemplateMatcher.DisableMultiplePoints();

                executor.TemplateMatcher.SetTemplate(tmStep.Settings.TemplatePath);
                executor.TemplateMatcher.SetThreshold(tmStep.Settings.ConfidenceThreshold);

                logger.LogInformation("TemplateMatchingStepHandler: Starting template matching with confidence threshold {Threshold}", tmStep.Settings.ConfidenceThreshold);

                executor.ImageToProcess = executor.CurrentImage.ToMat();

                // Use the current offset directly for template matching
                executor.TemplateMatchingResult = executor.TemplateMatcher.Detect(executor.ImageToProcess, executor.CurrentOffset);

                if (executor.TemplateMatchingResult.Success)
                {
                    executor.LatestCalculatedPoint = executor.TemplateMatchingResult.CenterPointOnDesktop;
                    logger.LogInformation("TemplateMatchingStepHandler: Template matching successful at point ({X}, {Y}) with confidence {Confidence:F3}", 
                        executor.LatestCalculatedPoint.Value.X, executor.LatestCalculatedPoint.Value.Y, executor.TemplateMatchingResult.Confidence);
                    
                    if (tmStep.Settings.DrawResults)
                    {
                        executor.CurrentImageWithResult = DrawResult.DrawTemplateMatchingResult(
                            executor.ImageToProcess,
                            executor.TemplateMatchingResult,
                            executor.TemplateMatchingResult.TemplateSize);
                        logger.LogDebug("TemplateMatchingStepHandler: Results drawn on image");
                    }
                }
                else
                {
                    // If template matching failed, reset the point
                    executor.LatestCalculatedPoint = null;
                    logger.LogInformation("TemplateMatchingStepHandler: Template matching failed - no match found above threshold");
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("TemplateMatchingStepHandler: Template matching was cancelled");
                return false; // Return false for cancellation, don't treat as error
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TemplateMatchingStepHandler: Failed to execute template matching: {ErrorMessage}", ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
