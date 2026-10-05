using System.Text.Json.Nodes;
using System.Diagnostics;
using DesktopAutomationApp.Controls.Jobs.Editors.Generated;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Services.Jobs;
using DesktopAutomationApp.Converters;
using MahApps.Metro.IconPacks;
using TaskAutomation.Steps.Definitions;
using DesktopAutomationApp.Views;
using TaskAutomation.Jobs;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class StepDialogRenderChecks
{
    private static readonly Dictionary<Window, HwndSource> Surfaces = [];
    private static FrameworkElement Surface(Window window) => (FrameworkElement)window.Content;

    public static void Verify(string directory)
    {
        VerifyDetailFields(directory);
        VerifyDefaultFocusVisual();
        var variable = new JobVariable
        {
            Name = "Wartezeit",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(1000)
        };
        var first = new TimeoutStep();
        var second = new TimeoutStep();
        foreach (var step in new[] { first, second })
            step.Inputs["delay_ms"] = new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = variable.Id.ToString() };
        var job = new Job
        {
            Name = "Freigabe abfragen",
            Steps = [first, second],
            Variables = [variable,
            new JobVariable { Name = "Ausgabeordner", Scope = JobVariableScope.Shared, ValueKind = ResultValueKind.FilePath, Value = JsonValue.Create(@"C:\Ergebnisse") },
            new JobVariable { Name = "Freigabe", Scope = JobVariableScope.Shared, ValueKind = ResultValueKind.Boolean, Value = JsonValue.Create(true) }]
        };
        using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(job);
        vm.BeginVariableDraftSession();
        vm.SelectedJobVariable = vm.JobVariables.Single(item => item.Name == "Wartezeit");
        vm.SelectedJobVariable.IntegerValue = 1200;
        var pickerVm = new AddJobStepDialogViewModel(new ControllableJobExecutor([job]), [],
            cameraCaptureService: new NoOpCameraCaptureService())
        { IsPickerOnly = true, SelectedType = "If", InsertionDescription = "Einfügen: Nach „Warten“ · Bei jedem Durchlauf" };
        var darkTheme = new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Dark.xaml") };
        System.Windows.Application.Current.Resources.MergedDictionaries.Add(darkTheme);
        var picker = new AddJobStepDialog { DataContext = pickerVm };
        VerifyGroupAppearance(picker);
        using var paletteVm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job
        {
            Name = "Step-Gruppen",
            Steps = [new CameraCaptureStep(), new OcrStep(), new KlickOnPointStep(), new StartProcessStep(),
                new FileSystemOperationStep(), new WindowsStateQueryStep(), new ShowOnDesktopStep(), new TimeoutStep()]
        });
        var palette = new Window { Content = new JobStepsView { DataContext = paletteVm } };
        Render(palette, directory, "step-group-palette.png", 1240, 900);

        Render(picker, directory, "add-step-dialog.png", 1120, 610);
        var controls = Descendants(Surface(picker)).OfType<FrameworkElement>().ToArray();
        var confirm = controls.OfType<Button>().Single(button => AutomationProperties.GetAutomationId(button) == "ConfirmJobStepTypeButton");
        if (!confirm.IsEnabled || controls.Any(control => control is global::DesktopAutomationApp.Controls.Jobs.Editors.Generated.GeneratedStepEditor))
            throw new InvalidOperationException("The add-step picker must select types without rendering settings.");
        pickerVm.StepTypeSearchText = "no-such-step-9876";
        Layout(picker, 1120, 610);
        if (confirm.IsEnabled || pickerVm.HasMatchingStepTypes) throw new InvalidOperationException("An empty search must disable addition.");
        Render(picker, directory, "add-step-empty.png", 940, 480);
        pickerVm.StepTypeSearchText = "";
        pickerVm.SelectedStepCategoryFilter = pickerVm.StepCategories.Last();
        if (pickerVm.StepTypeItems.Cast<AddJobStepDialogViewModel.StepTypeItem>().Any(item => item.Category != pickerVm.SelectedStepCategoryFilter))
            throw new InvalidOperationException("Category selection must filter the step catalog.");
        pickerVm.SelectedStepCategoryFilter = pickerVm.StepCategories[0];
        pickerVm.SelectedType = "If";
        Render(picker, directory, "add-step-compact.png", 940, 480);
        var catalog = Descendants(Surface(picker)).OfType<ListBox>().Single(list => AutomationProperties.GetAutomationId(list) == "JobStepTypePickerList");
        if (catalog.SelectedItem is not AddJobStepDialogViewModel.StepTypeItem { Name: "If" })
            throw new InvalidOperationException("The selected catalog row must agree with the description after category changes.");
        pickerVm.SelectedType = "Timeout";
        pickerVm.ConfirmCommand.Execute(null);
        if (pickerVm.CreatedStep is not TimeoutStep) throw new InvalidOperationException("The picker must create the selected default step.");

        var variables = new JobVariablesDialog { DataContext = vm };
        Render(variables, directory, "job-variables-dialog.png", 1140, 710);
        controls = Descendants(Surface(variables)).OfType<FrameworkElement>().ToArray();
        var detach = controls.OfType<Button>().Where(button => AutomationProperties.GetAutomationId(button) == "DetachJobVariableUsageButton").ToArray();
        if (detach.Length != 2 || detach.Any(button => !button.IsEnabled)) throw new InvalidOperationException("Each shared usage must expose an enabled direct detach action.");
        var apply = controls.OfType<Button>().Single(button => AutomationProperties.GetAutomationId(button) == "ApplyJobVariableChangesButton");
        if (!apply.IsEnabled || vm.SelectedJobVariable.CanChangeKind || variable.Value!.GetValue<int>() != 1000)
            throw new InvalidOperationException("Draft changes must stay unapplied and used variable types protected.");
        Render(variables, directory, "job-variables-compact.png", 980, 580);
        foreach (var id in new[] { "ApplyJobVariableChangesButton", "DiscardJobVariableChangesButton", "AddJobVariableButton" })
        {
            var button = Descendants(Surface(variables)).OfType<Button>().Single(item => AutomationProperties.GetAutomationId(item) == id);
            var bounds = button.TransformToAncestor(Surface(variables)).TransformBounds(new Rect(new Size(button.ActualWidth, button.ActualHeight)));
            if (bounds.Left < 0 || bounds.Right > Surface(variables).ActualWidth || bounds.Bottom > Surface(variables).ActualHeight)
                throw new InvalidOperationException($"Persistent action {id} must fit the compact dialog.");
        }
        var other = vm.JobVariables.Single(item => item.Name == "Freigabe");
        vm.SelectedJobVariable = other;
        vm.SelectedJobVariable = vm.JobVariables.Single(item => item.Name == "Wartezeit");
        if (vm.SelectedJobVariable.IntegerValue != 1200 || variable.Value!.GetValue<int>() != 1000)
            throw new InvalidOperationException("Changing selection must retain the numeric draft without committing it.");
        System.Windows.Application.Current.Resources.MergedDictionaries.Remove(darkTheme);
        Render(variables, directory, "job-variables-black.png", 1140, 710);
        Render(picker, directory, "add-step-black.png", 1120, 610);
        vm.DiscardVariableChangesCommand.Execute(null);
        foreach (var source in Surfaces.Values) source.Dispose();
        Surfaces.Clear();
        picker.Close();
        variables.Close();
        palette.Close();
        Console.WriteLine("Step and variable dialog interactions verified");
    }

    private static void VerifyDefaultFocusVisual()
    {
        var style = System.Windows.Application.Current.TryFindResource(SystemParameters.FocusVisualStyleKey) as Style
            ?? throw new InvalidOperationException("The global keyboard focus style must be configured.");
        var focus = new Control { Style = style };
        focus.ApplyTemplate();
        if (Descendants(focus).Any(child => child is System.Windows.Shapes.Shape))
            throw new InvalidOperationException("The default focus adorner must not draw a dashed outline.");
    }

    private static void VerifyDetailFields(string directory)
    {
        var listener = new BindingAuditListener();
        var bindingSource = PresentationTraceSources.DataBindingSource;
        var previousLevel = bindingSource.Switch.Level;
        bindingSource.Switch.Level = SourceLevels.Error;
        bindingSource.Listeners.Add(listener);
        var errors = new List<string>();
        var checkedTypes = new List<string>();
        try
        {
            foreach (var definition in BuiltInStepDefinitions.Instance.Definitions)
            {
                using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Steps = [definition.CreateDefault()] });
                vm.SelectedStep = vm.Steps[0];
                if (vm.SelectedGeneratedEditor is not { } model) continue;
                listener.Messages.Clear();
                checkedTypes.Add(definition.StepType.Name);
                var editor = new GeneratedStepEditor
                {
                    DataContext = vm.SelectedStepEditor,
                    Background = (Brush)System.Windows.Application.Current.FindResource("App.Brush.Surface"),
                    Foreground = (Brush)System.Windows.Application.Current.FindResource("App.Brush.TextPrimary")
                };
                var window = new Window { Content = editor };
                Layout(window, 400, 1200);
                foreach (var expander in Descendants(editor).OfType<Expander>()) expander.IsExpanded = true;
                Layout(window, 400, 1200);
                if (model.Fields.Count > 0 && !Descendants(editor).OfType<FrameworkElement>().Any(element => element.DataContext is GeneratedStepFieldViewModel))
                    throw new InvalidOperationException($"The real details editor must render fields for {definition.StepType.Name}.");
                foreach (var pair in Descendants(editor).OfType<FrameworkElement>().Where(element => element.DataContext is GeneratedStepPointFieldPairViewModel).Select(element => (GeneratedStepPointFieldPairViewModel)element.DataContext).Distinct())
                {
                    if (pair.WholeValueSource is null && !pair.ShowsIndividualValues)
                        throw new InvalidOperationException("Coordinate pairs without a whole-value source must show their individual inputs.");
                    var inputs = Descendants(editor).OfType<GeneratedValueSourceInput>().Where(input => ReferenceEquals(input.DataContext, pair.XField) || ReferenceEquals(input.DataContext, pair.YField)).ToArray();
                    if (pair.ShowsIndividualValues && (inputs.Length != 2 || inputs.Any(input => !HasVisibleAncestors(input, editor))))
                        throw new InvalidOperationException($"Position coordinates must be visible for {definition.StepType.Name}.");
                    if (pair.WholeValueSource is { } whole && whole.UseJobVariableCommand.CanExecute(null))
                    {
                        whole.UseJobVariableCommand.Execute(null);
                        Layout(window, 400, 1200);
                        if (pair.ShowsIndividualValues || !pair.UsesWholeValueReference || inputs.Any(input => HasVisibleAncestors(input, editor)))
                            throw new InvalidOperationException("A shared point reference must hide individual coordinates immediately.");
                        whole.UseIndividualValuesCommand.Execute(null);
                        Layout(window, 400, 1200);
                        if (!pair.ShowsIndividualValues || pair.UsesWholeValueReference || inputs.Any(input => !HasVisibleAncestors(input, editor)))
                            throw new InvalidOperationException("Returning to individual values must reveal both coordinates immediately.");
                    }
                }
                if (definition.StepType == typeof(ShowTextStep))
                {
                    Render(window, directory, "position-details.png", 400, 1200);
                    Render(window, directory, "position-details-compact.png", 260, 1200);
                }
                if (definition.StepType == typeof(StartProcessStep)) Render(window, directory, "process-position-details.png", 400, 1200);
                errors.AddRange(listener.Messages.Distinct().Select(message => definition.StepType.Name + ": " + message));
                Surfaces[window].Dispose(); Surfaces.Remove(window); window.Close();
            }
        }
        finally { bindingSource.Listeners.Remove(listener); bindingSource.Switch.Level = previousLevel; }
        File.WriteAllLines(Path.Combine(directory, "detail-binding-audit.txt"), errors);
        File.WriteAllLines(Path.Combine(directory, "detail-types-checked.txt"), checkedTypes);
        if (errors.Count > 0) throw new InvalidOperationException($"The detail editor has binding errors: {errors[0]}");
        Console.WriteLine($"Detail field audit: {checkedTypes.Count} step types, {errors.Count} binding errors");
    }

    private static bool HasVisibleAncestors(DependencyObject element, DependencyObject root)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement { Visibility: not Visibility.Visible }) return false;
            if (ReferenceEquals(current, root)) return true;
        }
        return false;
    }

    private sealed class BindingAuditListener : TraceListener
    {
        public List<string> Messages { get; } = [];
        public override void Write(string? message) { if (!string.IsNullOrWhiteSpace(message)) Messages.Add(message); }
        public override void WriteLine(string? message) => Write(message);
    }

    private static void VerifyGroupAppearance(FrameworkElement owner)
    {
        var converter = new StepOverviewConverter();
        var categories = new Dictionary<string, Color>();
        foreach (var definition in BuiltInStepDefinitions.Instance.Definitions)
        {
            var step = definition.CreateDefault();
            var icon = StepIconPresentation.ForType(definition.StepType);
            if (icon == PackIconMaterialKind.ShapeOutline)
                throw new InvalidOperationException($"Missing meaningful icon for {definition.StepType.Name} ({definition.Descriptor.IconKey}).");
            var brush = converter.Convert([step, owner], typeof(Brush), "color", System.Globalization.CultureInfo.CurrentCulture) as SolidColorBrush;
            var pickerBrush = converter.Convert([definition.Descriptor.CategoryId, owner], typeof(Brush), "color", System.Globalization.CultureInfo.CurrentCulture) as SolidColorBrush;
            if (brush is null || pickerBrush is null || brush.Color != pickerBrush.Color || brush.Color == Colors.SlateGray)
                throw new InvalidOperationException($"The catalog and overview must share the category color for {definition.StepType.Name}.");
            if (categories.TryGetValue(definition.Descriptor.CategoryId, out var existing) && existing != brush.Color)
                throw new InvalidOperationException("Every step in a category must have the same color.");
            categories[definition.Descriptor.CategoryId] = brush.Color;
        }
        if (categories.Count != categories.Values.Distinct().Count())
            throw new InvalidOperationException("Step categories must have distinct colors.");
        foreach (var group in BuiltInStepDefinitions.Instance.Definitions.GroupBy(definition => definition.Descriptor.CategoryId))
            if (group.Select(definition => StepIconPresentation.ForType(definition.StepType)).Distinct().Count() != group.Count())
                throw new InvalidOperationException($"Distinct built-in steps should have distinct icons within {group.Key}.");
    }

    private static void Layout(Window window, double width, double height)
    {
        window.Width = width; window.Height = height;
        var surface = Surface(window);
        if (!Surfaces.ContainsKey(window))
        {
            // Hidden native presentation source: load actual dialog content without showing or activating a window.
            var source = new HwndSource(new HwndSourceParameters("Dialog render checks")
            { WindowStyle = unchecked((int)0x80000000), Width = (int)width, Height = (int)height, PositionX = -30000, PositionY = -30000 });
            Surfaces.Add(window, source);
            source.RootVisual = surface;
        }
        surface.Width = width; surface.Height = height;
        surface.Measure(new Size(width, height));
        surface.Arrange(new Rect(0, 0, width, height));
        surface.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        surface.UpdateLayout();
    }
    private static void Render(Window window, string directory, string name, double width, double height)
    {
        Layout(window, width, height);
        var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(Surface(window));
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name)); encoder.Save(stream);
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
