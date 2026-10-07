using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using DesktopAutomationApp.Behaviors;
using System.Windows.Markup;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using DesktopAutomationApp.Views;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Services.Jobs;
using MahApps.Metro.IconPacks;
using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.DesktopAutomationApp;

/// <summary>Isolated, off-screen WPF rendering. No application services or user data are loaded.</summary>
internal static class StepListRenderHost
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--render-collapsible-panes") return CollapsiblePanesRenderHost.Run(args[1]);
        if (args.Length == 2 && args[0] == "--render-distribution") return DistributionSettingsRenderHost.Run(args[1]);
        if (args.Length == 2 && args[0] == "--render-start-page") return StartPageRenderHost.Run(args[1]);
        if (args.Length == 2 && args[0] == "--render-desktop-capture") return DesktopCaptureEditorRenderHost.Run(args[1]);
        if (args.Length == 2 && args[0] == "--render-macro-editor") return MacroEditorRenderHost.Run(args[1]);
        if (args.Length == 2 && args[0] == "--render-library") return LibraryBrowserRenderHost.Run(args[1]);
        if (args.Length == 2 && args[0] == "--render-logs") return LogScreenRenderHost.Run(args[1]);
        if (args.Length == 2 && args[0] == "--render-context-menus") return ContextMenuRenderHost.Run(args[1]);
        if (args.Length != 2 || args[0] != "--render-step-list") return 0;
        try { Render(args[1]); return 0; }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    internal static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "DesktopAutomation.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static bool HasVisibleAncestors(DependencyObject element, DependencyObject boundary)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement { Visibility: not Visibility.Visible }) return false;
            if (ReferenceEquals(current, boundary)) return true;
        }
        return false;
    }

    internal static System.Windows.Application LoadResources(string directory)
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var source = XDocument.Load(Path.Combine(RepositoryRoot(), "DesktopAutomationApp", "App.xaml"));
        XNamespace ui = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var resources = new XElement(source.Root!.Element(ui + "Application.Resources")!.Element(ui + "ResourceDictionary")!);
        foreach (var attribute in source.Root.Attributes().Where(attribute => attribute.IsNamespaceDeclaration))
        {
            var value = attribute.Value.StartsWith("clr-namespace:", StringComparison.Ordinal)
                ? attribute.Value + ";assembly=DesktopAutomationApp" : attribute.Value;
            resources.SetAttributeValue(attribute.Name, value);
        }
        foreach (var dictionary in resources.Descendants(ui + "ResourceDictionary"))
            if (dictionary.Attribute("Source") is { } resource && resource.Value.StartsWith("Styles/", StringComparison.Ordinal))
                resource.Value = "pack://application:,,,/DesktopAutomationApp;component/" + resource.Value;
        foreach (var element in resources.Descendants().Where(element => element.Name.NamespaceName.StartsWith("clr-namespace:", StringComparison.Ordinal)))
            element.Name = XName.Get(element.Name.LocalName, element.Name.NamespaceName + ";assembly=DesktopAutomationApp");
        var merged = resources.Element(ui + "ResourceDictionary.MergedDictionaries")!;
        var sources = merged.Elements().Select(dictionary => dictionary.Attribute("Source")!.Value).ToArray();
        merged.Remove();
        app.Resources = (ResourceDictionary)XamlReader.Parse(resources.ToString());
        foreach (var resource in sources)
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(resource) });
        foreach (var resource in new[] { "Styles/Themes/Black.xaml", "Styles/Accents/Cyan.xaml" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/" + resource) });
        Directory.CreateDirectory(directory);
        return app;
    }

    private static void Render(string directory)
    {
        var app = LoadResources(directory);
        var reports = new List<StepListRenderReport>();
        foreach (var scenario in new[] { "branches", "nested", "collapsed", "drag-snapped", "drag-free", "invalid-nested", "invalid-draft", "consecutive-closures", "empty-block", "empty-branches", "collapsed-alternative", "moved-block" })
        {
            var outer = new IfStep();
            var selected = new TimeoutStep { IsBreakpoint = true };
            List<JobStep> steps = scenario is "branches" or "drag-snapped" or "drag-free"
                ? [new UserChoiceStep(), outer, new TimeoutStep(), selected, new ElseStep(), new EndJobStep(), new EndIfStep()]
                : [new UserChoiceStep(), outer, new TimeoutStep(), new IfStep(), selected, new ElseStep(), new TimeoutStep(), new EndIfStep(),
                    new ElseStep(), new EndJobStep(), new EndIfStep(), new TimeoutStep()];
            if (scenario == "invalid-nested")
                steps = [new UserChoiceStep(), outer, new ShowTextStep(), new IfStep(), new TimeoutStep(), new EndIfStep(),
                    selected, new ElseStep(), new EndJobStep(), new EndIfStep()];
            if (scenario == "consecutive-closures")
                steps = [new UserChoiceStep(), outer, new IfStep(), new IfStep(), selected,
                    new EndIfStep(), new EndIfStep(), new EndIfStep(), new TimeoutStep()];
            if (scenario == "empty-block")
                steps = [new UserChoiceStep(), outer, new EndIfStep(), selected];
            if (scenario == "empty-branches")
                steps = [new UserChoiceStep(), outer, new ElseIfStep(), new ElseStep(), new EndIfStep(), selected];
            if (scenario == "collapsed-alternative")
                steps = [new UserChoiceStep(), outer, new TimeoutStep(), new ElseIfStep(), new TimeoutStep(),
                    new ElseStep(), new TimeoutStep(), new EndIfStep(), selected];
            var job = new Job { Name = "Freigabe abfragen", Steps = steps };
            var choice = (UserChoiceStep)steps[0];
            choice.Settings.Title = "Freigabe";
            choice.Settings.Question = "Fortsetzen oder Abbrechen";
            choice.Settings.Options = [new() { Label = "Fortsetzen" }, new() { Label = "Abbrechen" }];
            foreach (var conditional in steps.OfType<IfStep>())
                conditional.Settings.Conditions = [new StepCondition { ProviderId = ValueProviderIds.StepResult,
                    SourceStepId = choice.Id, PropertyPath = "SelectedOptionId", Operator = ConditionOperator.Equals,
                    ComparisonValue = choice.Settings.Options[0].Id }];
            foreach (var alternative in steps.OfType<ElseIfStep>()) alternative.Settings.Conditions = outer.Settings.Conditions.ToList();
            if (scenario == "invalid-nested") ((IfStep)steps[3]).Settings.Conditions = [];
            using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(job);
            if (scenario == "invalid-nested")
            {
                foreach (var result in JobValidation.ValidateJob(job).Steps)
                    result.Step.SetValidationResult(result.IsValid, global::DesktopAutomationApp.Localization.JobValidationErrorLocalizer.Localize(result.Error, result.Step));
            }
            vm.SelectedStep = selected;
            if (scenario == "invalid-draft")
            {
                vm.SelectedGeneratedEditor!.Fields.Single(field => field.Descriptor.Id == "delay_ms").InputText = "broken";
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                vm.SetSelectedSteps([steps[0]], vm.Steps);
                if (!vm.ValidationIssues.Any(issue => issue.Step.Id == selected.Id && issue.IsDraft))
                    throw new InvalidOperationException("An invalid draft must survive selecting another step.");
            }
            if (scenario == "collapsed") vm.ToggleBlockCommand.Execute(outer);
            if (scenario == "collapsed-alternative") vm.ToggleBlockCommand.Execute(steps[3]);
            var view = new JobStepsView { DataContext = vm, Width = 1195, Height = 1060, FontSize = 14 };
            var decorator = new AdornerDecorator { Child = view, Width = view.Width, Height = view.Height };
            // Hidden native surface tests screen-to-DIP conversion without moving the user's cursor.
            using var nativeSurface = scenario == "drag-free" ? new HwndSource(new HwndSourceParameters("Step preview coordinate test")
            { WindowStyle = unchecked((int)0x80000000), Width = 1195, Height = 1060, PositionX = 137, PositionY = 91 }) : null;
            if (nativeSurface is not null) nativeSurface.RootVisual = decorator;
            decorator.Measure(new Size(view.Width, view.Height));
            decorator.Arrange(new Rect(0, 0, view.Width, view.Height));
            view.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
            view.UpdateLayout();
            if (scenario != "invalid-draft")
                foreach (var phaseList in Descendants(view).OfType<ListBox>())
                    if (phaseList.Items.Contains(selected)) phaseList.SelectedItem = selected;
            view.UpdateLayout();
            VerifyFreshFlowDrawing(view);
            if (scenario == "nested") VerifyScrolledFlowDrawing(view, decorator, directory);
            if (scenario == "moved-block")
            {
                var move = (AsyncRelayCommand<StepDragDrop.MoveRequest>)vm.ReorderStepCommand;
                var movingSource = (IList)vm.Steps;
                var indices = vm.DragIndicesResolver(new StepDragDrop.DragStartRequest(movingSource, 1, [1]));
                move.Execute(new StepDragDrop.MoveRequest(movingSource, 1, movingSource, movingSource.Count, SourceIndices: indices));
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (move.IsExecuting && DateTime.UtcNow < deadline)
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                if (move.IsExecuting || vm.Steps[1] is not TimeoutStep || vm.Steps[2].Id != outer.Id)
                    throw new InvalidOperationException("The fixture must move the complete nested block after the trailing step.");
                view.UpdateLayout();
                VerifyFreshFlowDrawing(view);
                // A template finishes arranging after the first frame (no panel size change).
                // The port must follow its measured icon without needing a hover repaint.
                var delayedIcon = Descendants(view).OfType<Border>().Single(element =>
                    AutomationProperties.GetAutomationId(element) == "JobStepDragHandle" && ReferenceEquals(element.DataContext, outer));
                var originalMargin = delayedIcon.Margin;
                delayedIcon.Margin = new Thickness(3, 0, -3, 0);
                view.UpdateLayout();
                VerifyFreshFlowDrawing(view);
                VerifyArrowPorts(view);
                delayedIcon.Margin = originalMargin;
                view.UpdateLayout();
                VerifyFreshFlowDrawing(view);
                vm.ToggleBlockCommand.Execute(outer);
                view.UpdateLayout();
                VerifyFreshFlowDrawing(view);
                vm.ToggleBlockCommand.Execute(outer);
                view.Width -= 80;
                decorator.Measure(new Size(view.Width, view.Height));
                decorator.Arrange(new Rect(0, 0, view.Width, view.Height));
                view.UpdateLayout();
                VerifyFreshFlowDrawing(view);
            }
            var all = Descendants(view).ToArray();
            if (scenario == "invalid-nested")
            {
                var invalid = steps[3];
                if (invalid.ValidationError!.Contains("StepValidation.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Validation errors must display translated field labels and messages.");
                var invalidItem = (ListBoxItem)all.OfType<ListBox>().Single(control => control.Items.Contains(invalid))
                    .ItemContainerGenerator.ContainerFromItem(invalid);
                var invalidCard = (Border)invalidItem.Template.FindName("Card", invalidItem);
                if (invalid.IsValid || invalidCard.BorderThickness.Left < 2
                    || invalidCard.BorderBrush != app.Resources["App.Brush.Danger"])
                    throw new InvalidOperationException("The invalid condition must have a visible red card border.");
                var errorButton = all.OfType<Button>().Single(button => AutomationProperties.GetAutomationId(button) == "JobStepValidationIssue"
                    && ReferenceEquals(button.CommandParameter, invalid));
                if (!Descendants(errorButton).OfType<TextBlock>().Any(text => text.Text.Contains(invalid.ValidationError!, StringComparison.Ordinal)))
                    throw new InvalidOperationException("The summary must identify the affected step and its validation error.");
                vm.ToggleBlockCommand.Execute(outer);
                vm.IsRunSectionExpanded = false;
                errorButton.Command.Execute(errorButton.CommandParameter);
                view.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                if (!ReferenceEquals(vm.SelectedStep, invalid) || !vm.IsRunSectionExpanded || vm.CollapsedBlockIds.Contains(outer.Id)
                    || invalidItem.Visibility != Visibility.Visible)
                    throw new InvalidOperationException("Error navigation must reveal and select the affected nested step.");
            }
            foreach (var marker in all.OfType<ListBoxItem>().Where(item => item.DataContext is EndIfStep))
                if (marker.Visibility != Visibility.Collapsed || marker.ActualHeight > 0)
                    throw new InvalidOperationException("Internal closing markers must have no visible or selectable row.");
            if (scenario == "invalid-draft")
            {
                var draftItem = (ListBoxItem)all.OfType<ListBox>().Single(control => control.Items.Contains(selected))
                    .ItemContainerGenerator.ContainerFromItem(selected);
                var draftCard = (Border)draftItem.Template.FindName("Card", draftItem);
                if (draftCard.BorderThickness.Left < 2 || draftCard.BorderBrush != app.Resources["App.Brush.Danger"])
                    throw new InvalidOperationException("A cached invalid draft must retain its error border.");
            }
            if (all.OfType<Button>().Any(button => AutomationProperties.GetAutomationId(button) == "AddStepToEmptyBranch"))
                throw new InvalidOperationException("The step list must not render add-step cards.");
            foreach (var panel in all.OfType<global::DesktopAutomationApp.Controls.ControlFlowBlockPanel>())
            {
                var icons = Descendants(panel).OfType<PackIconMaterial>().ToArray();
                if (icons.Length == 0 || VisualTreeHelper.GetDrawing(panel) is not { } drawing) continue;
                var iconRight = icons.Max(icon => icon.TranslatePoint(new Point(icon.ActualWidth, 0), panel).X);
                foreach (var route in GeometryDrawings(drawing).Where(geometry => geometry.Pen?.Brush is SolidColorBrush solid
                    && (solid.Color == Color.FromRgb(255, 184, 60) || solid.Color == Color.FromRgb(155, 115, 247))))
                    if (route.Bounds.Right > iconRight + 8)
                        throw new InvalidOperationException("Branch guides must stay in the indentation corridor instead of spanning card widths.");
            }
            var cards = new List<StepCardRenderReport>();
            foreach (var item in all.OfType<ListBoxItem>().Where(item => item.DataContext is JobStep step && step is not EndIfStep && item.Visibility == Visibility.Visible))
            {
                if (item.Template.FindName("Card", item) is not Border card) continue;
                var content = Descendants(card).OfType<FrameworkElement>().Where(element => element.ActualWidth > 0 && element.ActualHeight > 0 && HasVisibleAncestors(element, card)).ToArray();
                var number = Descendants(item).OfType<TextBlock>().Single(text => AutomationProperties.GetAutomationId(text) == "JobStepNumber");
                var breakpoint = Descendants(item).OfType<Button>().Single(button => AutomationProperties.GetAutomationId(button) == "JobStepBreakpoint");
                var frame = (Border)item.Template.FindName("Frame", item);
                var step = (JobStep)item.DataContext;
                cards.Add(new(number.TranslatePoint(new Point(number.ActualWidth, 0), item).X,
                    breakpoint.TranslatePoint(new Point(0, 0), item).X, content.OfType<Button>().Count(),
                    content.OfType<TextBlock>().Count(), content.OfType<PackIconMaterial>().Count(),
                    StepListProjection.Get((IList)vm.Steps, vm.StepsVersion).Depth(vm.Steps.IndexOf(step)),
                    frame.TranslatePoint(new Point(0, 0), view).X, frame.TranslatePoint(new Point(frame.ActualWidth, 0), view).X,
                    step is IfStep or ElseIfStep, step is IfStep or ElseIfStep or ElseStep));
            }
            var list = all.OfType<ListBox>().Single(control => control.Items.Contains(selected));
            var handles = cards.Count > 0 && all.OfType<Border>()
                .Where(control => AutomationProperties.GetAutomationId(control) == "JobStepDragHandle")
                .All(control => StepDragDrop.IsInsideDragHandle(Descendants(control).OfType<PackIconMaterial>().Single()));
            var cardTexts = all.OfType<ListBoxItem>().Where(item => item.DataContext is JobStep && item.Visibility == Visibility.Visible)
                .Select(item => item.Template.FindName("Card", item)).OfType<Border>()
                .SelectMany(card => Descendants(card).OfType<TextBlock>());
            handles &= cardTexts.All(text => !StepDragDrop.IsInsideDragHandle(text))
                && StepDragDrop.GetRequireDragHandle(list) && StepDragDrop.GetIsGhostPreviewEnabled(list);
            var collapses = all.OfType<Button>().Where(control => AutomationProperties.GetAutomationId(control) == "ToggleJobStepBlock"
                && control.Visibility == Visibility.Visible).ToArray();
            foreach (var button in collapses)
            {
                var item = list.ContainerFromElement(button) as ListBoxItem;
                var icon = Descendants(item!).OfType<Border>().Single(border => AutomationProperties.GetAutomationId(border) == "JobStepDragHandle");
                if (button.TranslatePoint(new Point(button.ActualWidth, 0), item!).X > icon.TranslatePoint(new Point(), item!).X)
                    throw new InvalidOperationException("The fold arrow must be left of the step symbol.");
                var glyph = Descendants(button).OfType<PackIconMaterial>().Single();
                var folded = vm.CollapsedBlockIds.Contains(((JobStep)button.DataContext).Id);
                if (glyph.Kind != (folded ? PackIconMaterialKind.ChevronRight : PackIconMaterialKind.ChevronDown))
                    throw new InvalidOperationException("The plain fold arrow must indicate right when folded and down when expanded.");
            }
            foreach (var hint in all.OfType<TextBlock>().Where(text => AutomationProperties.GetAutomationId(text) == "EmptyJobStepBranch" && text.Visibility == Visibility.Visible))
                if (hint.IsHitTestVisible || hint.DataContext is not JobStep)
                    throw new InvalidOperationException("Empty-branch information must be passive content of a real step row.");
            var collapseAccessible = collapses.Length > 0 && collapses.All(button => button.ActualWidth >= 32 && button.ActualHeight >= 32
                && !string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)));
            var before = vm.Steps.Select(step => step.Id).ToArray();
            var originalHeight = list.ActualHeight;
            var originalMinHeight = list.MinHeight;
            var sourceItem = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(vm.Steps.IndexOf(selected));
            var originalOpacity = sourceItem.Opacity;
            StepDragPreviewSession? drag = null;
            var snapped = false;
            var ghost = true;
            var stable = true;
            if (scenario.StartsWith("drag-", StringComparison.Ordinal))
            {
                var sourceIndex = vm.Steps.IndexOf(selected);
                var payload = new StepDragDrop.DragPayload((IList)vm.Steps, sourceIndex, [sourceIndex]);
                drag = new StepDragPreviewSession(list, payload, startTimer: false);
                var targetItem = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(5);
                var position = targetItem.TranslatePoint(new Point(StepListProjection.GutterWidth + StepListProjection.Indentation + 8, 0), list);
                var placement = vm.DropPlacementResolver(list, position);
                var valid = vm.PreviewMoveValidator(new StepDragDrop.MoveRequest((IList)vm.Steps, sourceIndex, (IList)vm.Steps, placement.Index,
                    SourceIndices: [sourceIndex]));
                if (!valid) throw new InvalidOperationException("Render fixture must provide a valid cross-branch move.");
                drag.UpdateTarget(list, placement, valid);
                decorator.UpdateLayout();
                var gapHeight = list.ActualHeight;
                drag.ClearTarget();
                decorator.UpdateLayout();
                drag.UpdateTarget(list, placement, valid);
                decorator.UpdateLayout();
                stable = Math.Abs(list.ActualHeight - gapHeight) < 0.5;
                var rejected = !drag.UpdateTarget(list, placement, valid: false);
                stable &= rejected && !drag.IsSnapped && before.SequenceEqual(vm.Steps.Select(step => step.Id));
                drag.UpdateTarget(list, placement, valid);
                if (scenario == "drag-free") drag.UpdateTarget(list, placement with { Distance = 200 }, valid);
                if (nativeSurface is not null)
                {
                    var first = new Point(190, 260);
                    var second = new Point(310, 440);
                    stable &= drag.UpdatePointerFromScreen(view.PointToScreen(first)) && (drag.PointerPosition - first).Length < 1;
                    stable &= drag.UpdatePointerFromScreen(view.PointToScreen(second)) && (drag.PointerPosition - second).Length < 1;
                    stable &= (drag.PointerRelativeTo(list) - view.TranslatePoint(second, list)).Length < 1;
                }
                else drag.UpdatePointer(new Point(310, 440));
                decorator.UpdateLayout();
                snapped = drag.IsSnapped;
                if (drag.PreviewBounds is { } preview)
                {
                    var targetCard = (Border)targetItem.Template.FindName("Card", targetItem);
                    stable &= Math.Abs(preview.Width - targetCard.ActualWidth) < 0.5;
                }
                ghost = Math.Abs(sourceItem.Opacity - 0.22) < 0.01;
                stable &= before.SequenceEqual(vm.Steps.Select(step => step.Id));
            }
            VerifyFreshFlowDrawing(view);
            VerifyArrowPorts(view);
            var image = scenario + ".png";
            var bitmap = new RenderTargetBitmap((int)view.Width, (int)view.Height, 96, 96, PixelFormats.Pbgra32);
            var surface = new DrawingVisual();
            using (var context = surface.RenderOpen())
            {
                context.DrawRectangle((Brush)app.Resources["App.Brush.WindowBackground"], null, new Rect(0, 0, view.Width, view.Height));
                context.DrawRectangle(new VisualBrush(decorator), null, new Rect(0, 0, view.Width, view.Height));
            }
            bitmap.Render(surface);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(Path.Combine(directory, image))) encoder.Save(stream);
            drag?.Dispose();
            decorator.UpdateLayout();
            var restored = Math.Abs(sourceItem.Opacity - originalOpacity) < 0.01
                && list.MinHeight == originalMinHeight && Math.Abs(list.ActualHeight - originalHeight) < 0.5
                && all.OfType<ListBoxItem>().All(item => StepDragDrop.GetPreviewDisplacement(item) == 0);
            reports.Add(new(image, all.OfType<CheckBox>().Count(control => AutomationProperties.GetAutomationId(control) == "SelectedJobStepBreakpointToggle"), cards,
                all.OfType<CheckBox>().Count(control => AutomationProperties.GetAutomationId(control) == "SelectedJobStepEnabledToggle"),
                handles, snapped, ghost, stable, restored, collapseAccessible));
            if (scenario == "collapsed")
            {
                var payload = new StepDragDrop.DragPayload((IList)vm.Steps, 1,
                    vm.DragIndicesResolver(new StepDragDrop.DragStartRequest((IList)vm.Steps, 1, [1])));
                using var collapsedDrag = new StepDragPreviewSession(list, payload, startTimer: false);
                var expected = payload.SourceIndices.Count(index => global::DesktopAutomationApp.Localization.StepLocalization.HasListPosition(vm.Steps[index]));
                if (expected <= 1 || collapsedDrag.MovingStepCount != expected)
                    throw new InvalidOperationException("Collapsed drag must include its hidden steps in the preview count.");
            }
            view.DataContext = null;
        }
        File.WriteAllText(Path.Combine(directory, "layout.json"), JsonSerializer.Serialize(reports));
        StepDialogRenderChecks.Verify(directory);
        VerifyPickerInteractions();
        DragScrollRenderChecks.Verify(directory);
        Console.WriteLine("Picker interactions verified");
        app.Shutdown();
    }

    private static void VerifyPickerInteractions()
    {
        var picked = false;
        var leaf = new ConditionSelectionNode("Value", selectCommand: new RelayCommand(() => picked = true));
        var group = new ConditionSelectionNode("Group", [leaf]);
        var picker = new global::DesktopAutomationApp.Controls.Jobs.ResultPathPicker { Width = 300, ItemsSource = new[] { group } };
        using var surface = new HwndSource(new HwndSourceParameters("Picker interaction test")
        { WindowStyle = unchecked((int)0x80000000), Width = 400, Height = 400, PositionX = -32000, PositionY = -32000 });
        surface.RootVisual = picker;
        picker.Measure(new Size(400, 400));
        picker.Arrange(new Rect(0, 0, 300, 34));
        picker.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        var popup = (System.Windows.Controls.Primitives.Popup)picker.FindName("SelectionPopup");
        popup.Child.Opacity = 0; // Native popup remains invisible throughout the isolated interaction test.
        var toggle = (System.Windows.Controls.Primitives.ToggleButton)picker.FindName("DropDownToggle");
        var onClick = typeof(System.Windows.Controls.Primitives.ButtonBase).GetMethod("OnClick",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        void Click(System.Windows.Controls.Primitives.ButtonBase button) => onClick.Invoke(button, null);
        Click(toggle);
        if (!popup.IsOpen) throw new InvalidOperationException("Picker did not open.");
        Click(toggle);
        if (popup.IsOpen) throw new InvalidOperationException("Clicking an open picker must close it without reopening.");
        Click(toggle);
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        popup.Child.UpdateLayout();
        var groupButton = Descendants(popup.Child).OfType<Button>().Single(button => ReferenceEquals(button.DataContext, group));
        Click(groupButton);
        if (!popup.IsOpen) throw new InvalidOperationException("Expanding a picker group must not close the popup.");
        popup.Child.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        var leafButton = Descendants(popup.Child).OfType<Button>().Single(button => ReferenceEquals(button.DataContext, leaf));
        Click(leafButton);
        if (popup.IsOpen || !picked) throw new InvalidOperationException("Selecting a picker value must apply it and close the popup.");
        Click(toggle);
        picker.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, surface, 0,
            System.Windows.Input.Key.Escape)
        { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
        if (popup.IsOpen) throw new InvalidOperationException("Escape must close the picker.");
        Click(toggle);
        System.Windows.Input.InputManager.Current.ProcessInput(new System.Windows.Input.MouseButtonEventArgs(
            System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
        { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseDownEvent, Source = new Border() });
        if (popup.IsOpen) throw new InvalidOperationException("Clicking outside must close the picker.");
        Click(toggle);
        picker.Visibility = Visibility.Collapsed;
        if (popup.IsOpen) throw new InvalidOperationException("A hidden editor must close its picker.");
    }

    private static void VerifyArrowPorts(FrameworkElement view)
    {
        foreach (var panel in Descendants(view).OfType<global::DesktopAutomationApp.Controls.ControlFlowBlockPanel>())
        {
            if (VisualTreeHelper.GetDrawing(panel) is not { } drawing) continue;
            var secondary = panel.TryFindResource("App.Brush.TextSecondary") as SolidColorBrush;
            if (secondary is not null && GeometryDrawings(drawing).Any(shape => shape.Pen?.Brush is SolidColorBrush pen
                && pen.Color == secondary.Color && Math.Abs(shape.Bounds.Width - 8) < 0.01 && Math.Abs(shape.Bounds.Height - 4) < 0.01))
                throw new InvalidOperationException("Block ends must not draw a decorative gray downward chevron.");
            var ports = Descendants(panel).OfType<ListBoxItem>().Where(item => item.Visibility == Visibility.Visible)
                .Select(item => (Card: item.Template.FindName("Card", item) as FrameworkElement,
                    Icon: Descendants(item).OfType<Border>().FirstOrDefault(element => AutomationProperties.GetAutomationId(element) == "JobStepDragHandle")))
                .Where(pair => pair.Card is not null && pair.Icon is not null)
                .Select(pair => new Point(pair.Icon!.TranslatePoint(new Point(pair.Icon.ActualWidth / 2, 0), panel).X,
                    pair.Card!.TranslatePoint(new Point(), panel).Y - 2)).ToArray();
            foreach (var arrow in GeometryDrawings(drawing).Where(shape => shape.Pen is null && shape.Brush is SolidColorBrush
                && Math.Abs(shape.Bounds.Width - 6) < 0.01 && Math.Abs(shape.Bounds.Height - 4) < 0.01))
            {
                var tip = new Point(arrow.Bounds.Left + 3, arrow.Bounds.Bottom);
                if (!ports.Any(port => (port - tip).Length < 0.01))
                    throw new InvalidOperationException("A sequence arrow must end directly above the measured step icon.");
            }
        }
    }

    private static void VerifyScrolledFlowDrawing(JobStepsView view, AdornerDecorator decorator, string directory)
    {
        var width = view.Width;
        var height = view.Height;
        void Arrange()
        {
            decorator.Width = view.Width;
            decorator.Height = view.Height;
            decorator.Measure(new Size(view.Width, view.Height));
            decorator.Arrange(new Rect(0, 0, view.Width, view.Height));
            view.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        }
        view.Width = 900;
        view.Height = 480;
        Arrange();
        var list = (ListBox)view.FindName("StepsList");
        var panel = Descendants(list).OfType<global::DesktopAutomationApp.Controls.ControlFlowBlockPanel>().Single();
        var scroller = Descendants(list).OfType<ScrollViewer>().First();
        if (scroller.ScrollableWidth < 60) throw new InvalidOperationException("The narrow fixture must require horizontal scrolling.");
        Rect[] Bounds() => GeometryDrawings(VisualTreeHelper.GetDrawing(panel)!).Select(shape => shape.Bounds).ToArray();
        var initial = Bounds();
        var vertical = Descendants(view).OfType<ScrollViewer>().First(candidate => Descendants(candidate).Contains(list));
        if (vertical.ScrollableHeight < 150) throw new InvalidOperationException("The short fixture must require vertical scrolling.");
        foreach (var offset in new[] { 30d, 60.5d, 0d })
        {
            scroller.ScrollToHorizontalOffset(offset);
            vertical.ScrollToVerticalOffset(offset > 60 ? 150 : 0);
            Arrange();
            VerifyFreshFlowDrawing(view);
            VerifyArrowPorts(view);
            var current = Bounds();
            if (initial.Length != current.Length) throw new InvalidOperationException("Scrolling must preserve flow geometry.");
            for (var index = 0; index < initial.Length; index++)
            {
                var expected = initial[index];
                expected.Offset(-scroller.HorizontalOffset, 0);
                if (Math.Abs(expected.Left - current[index].Left) > 0.01 || Math.Abs(expected.Top - current[index].Top) > 0.01
                    || Math.Abs(expected.Width - current[index].Width) > 0.01 || Math.Abs(expected.Height - current[index].Height) > 0.01)
                    throw new InvalidOperationException("Block surfaces, scope rails and arrows must scroll together with the step cards.");
            }
            if (offset > 60)
            {
                var bitmap = new RenderTargetBitmap((int)view.Width, (int)view.Height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(decorator);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file = File.Create(Path.Combine(directory, "nested-scrolled.png"));
                encoder.Save(file);
            }
        }
        view.Width = width;
        view.Height = height;
        Arrange();
    }

    private static void VerifyFreshFlowDrawing(FrameworkElement view)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        var panels = Descendants(view).OfType<global::DesktopAutomationApp.Controls.ControlFlowBlockPanel>().ToArray();
        string Snapshot()
        {
            // Force composition of the current surface, without sending pointer input or
            // invalidating a panel. A later explicit repaint must not correct stale geometry.
            var bitmap = new RenderTargetBitmap((int)view.ActualWidth, (int)view.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(view);
            return string.Join("|", panels.Select(panel => VisualTreeHelper.GetDrawing(panel) is { } drawing
                ? string.Join(";", GeometryDrawings(drawing).Select(shape => shape.Geometry.ToString(CultureInfo.InvariantCulture))) : ""));
        }
        var automatic = Snapshot();
        foreach (var panel in panels) panel.InvalidateVisual();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        var repainted = Snapshot();
        if (automatic != repainted)
            throw new InvalidOperationException("Opening, moving, collapsing and resizing must render current block surfaces and routes without hover.");
    }

    private static IEnumerable<GeometryDrawing> GeometryDrawings(Drawing drawing)
    {
        if (drawing is GeometryDrawing geometry) yield return geometry;
        if (drawing is DrawingGroup group)
            foreach (var child in group.Children)
                foreach (var nested in GeometryDrawings(child)) yield return nested;
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
