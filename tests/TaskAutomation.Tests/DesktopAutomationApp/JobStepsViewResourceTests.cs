namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepsViewResourceTests
{
    [Fact]
    public void ChoiceGroupEditor_UsesInstanceLocalArbitrarySelection()
    {
        var root = RepositoryRoot();
        var controlPath = Path.Combine(root, "DesktopAutomationApp", "Controls", "Jobs", "Shared", "ChoiceGroupEditor.xaml");
        Assert.True(File.Exists(controlPath));
        var xaml = File.ReadAllText(controlPath);

        Assert.Contains("ItemsSource=\"{Binding ItemsSource, ElementName=Root}\"", xaml);
        Assert.Contains("SelectedItem=\"{Binding SelectedItem, ElementName=Root, Mode=TwoWay}\"", xaml);
        Assert.Contains("<UniformGrid Rows=\"1\"/>", xaml);
        Assert.Contains("HorizontalContentAlignment=\"Stretch\"", xaml);
        Assert.DoesNotContain("WrapPanel", xaml);
        Assert.DoesNotContain("RadioButton", xaml);
        Assert.DoesNotContain("GroupName", xaml);
    }

    [Fact]
    public void ProcessEditor_ShowsOnlyManualContent()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.Contains(
            "Content=\"{Binding ProcessTargetEditor.ManualSourceContent}\"",
            xaml);
        Assert.DoesNotContain("ProcessTargetEditor.SelectedSourceOption", xaml);
        Assert.Contains(
            "DataType=\"{x:Type viewModels:GeneratedProcessNameTargetContentViewModel}\"",
            xaml);
        Assert.DoesNotContain(
            "DataType=\"{x:Type viewModels:GeneratedProcessReferenceTargetContentViewModel}\"",
            xaml);
        Assert.Contains("Text=\"{loc:Translate Key=Ui.Step.Settings.ProcessName}\"", xaml);
        Assert.Contains("Text=\"{loc:Translate Key=Ui.Step.Settings.WindowTitleContains}\"", xaml);
        Assert.Contains(
            "DataContext=\"{Binding Editor.WindowTitleField}\"",
            xaml);
        Assert.DoesNotContain("x:Key=\"ProcessSourceContentTemplate\"", xaml);
        Assert.Contains("Content=\"{Binding SelectedContent, ElementName=Root}\"", File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Shared", "ChoiceGroupEditor.xaml")));
    }

    [Fact]
    public void JobStepsView_RegistersConvertersPreviouslyProvidedByLegacyTemplates()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));

        Assert.Contains("<conv:StepNumberConverter x:Key=\"StepNumberConverter\"/>", xaml);
        Assert.Contains("<conv:StepDisplayNameConverter x:Key=\"StepDisplayNameConverter\"/>", xaml);
    }

    [Fact]
    public void GeneratedStepEditor_UsesReadOnlyExpansionBindingAndStretchedLayout()
    {
        var editorXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));
        var dialogXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "AddJobStepDialog.xaml"));

        Assert.Contains("IsExpanded=\"{Binding IsInitiallyExpanded, Mode=OneWay}\"", editorXaml);
        Assert.DoesNotContain("IsExpanded=\"{Binding IsInitiallyExpanded}\"", editorXaml);
        Assert.Contains("HorizontalContentAlignment=\"Stretch\"", editorXaml);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", dialogXaml);
    }

    [Fact]
    public void StepFieldLabels_WrapInsideTheirLabelColumn()
    {
        var stylesXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Styles", "StepEditors.xaml"));
        var editorXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.Contains("<Setter Property=\"TextWrapping\" Value=\"Wrap\"/>", stylesXaml);
        Assert.Contains("<StackPanel VerticalAlignment=\"Top\" Margin=\"0,0,12,0\">", editorXaml);
    }

    [Fact]
    public void GeneratedStepEditor_CreatesOnlyTheSelectedFieldEditor()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.Contains("GeneratedStepFieldTemplateSelector", xaml);
        Assert.Contains("ContentTemplateSelector=\"{StaticResource GeneratedStepFieldTemplateSelector}\"", xaml);
        Assert.Contains("x:Key=\"ValueReferenceFieldTemplate\"", xaml);
        Assert.Contains("x:Key=\"VisualOverlayFieldTemplate\"", xaml);
        Assert.Contains("x:Key=\"RoiFieldTemplate\"", xaml);
        Assert.Contains("x:Key=\"WindowsCapabilityFieldTemplate\"", xaml);
        Assert.DoesNotContain("Visibility=\"{Binding UsesValueReferencePicker,", xaml);
        Assert.DoesNotContain("Visibility=\"{Binding UsesVisualOverlay,", xaml);
        Assert.DoesNotContain("Visibility=\"{Binding UsesRoiPicker,", xaml);
        Assert.DoesNotContain("Visibility=\"{Binding UsesWindowsCapabilityPicker,", xaml);
    }

    [Fact]
    public void GeneratedStepEditor_ResolvesDialogCommandsThroughItsAncestor()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.DoesNotContain("DataContext.ChooseMonitorCommand, ElementName=Root", xaml);
        Assert.DoesNotContain("DataContext.BrowseGeneratedFileCommand, ElementName=Root", xaml);
        Assert.Contains(
            "DataContext.ChooseMonitorCommand, RelativeSource={RelativeSource AncestorType={x:Type generated:GeneratedStepEditor}}",
            xaml);
        Assert.Contains(
            "DataContext.BrowseGeneratedFileCommand, RelativeSource={RelativeSource AncestorType={x:Type generated:GeneratedStepEditor}}",
            xaml);
    }

    [Fact]
    public void RemainingSemanticChoiceEditors_UseSharedChoiceGroupEditor()
    {
        var root = RepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated", "GeneratedStepEditor.xaml"),
            Path.Combine(root, "DesktopAutomationApp", "Controls", "Jobs", "Conditions", "ConditionEditor.xaml")
        };

        Assert.All(files, file => Assert.Contains("ChoiceGroupEditor", File.ReadAllText(file)));
    }

    [Fact]
    public void ObsoleteTwoSlotSelectors_AreRemoved()
    {
        var root = RepositoryRoot();
        Assert.False(File.Exists(Path.Combine(root, "DesktopAutomationApp", "Controls", "Jobs", "Shared", "ValueSourceSelector.xaml")));
        Assert.False(File.Exists(Path.Combine(root, "DesktopAutomationApp", "Controls", "Jobs", "ProcessTargetModeSelector.xaml")));
    }

    [Fact]
    public void GeneratedChoiceEditor_RendersHierarchicalNodes()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.Contains("ItemsSource=\"{Binding Nodes}\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding Branches}\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding Children}\"", xaml);
        Assert.DoesNotContain("PrimarySourceFields", xaml);
        Assert.DoesNotContain("SecondarySourceFields", xaml);
    }

    [Fact]
    public void JobStepDialogPrototype_UsesSearchableTwoColumnLayout()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "AddJobStepDialog.xaml"));

        Assert.Contains("Text=\"{Binding StepTypeSearchText, UpdateSourceTrigger=PropertyChanged}\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding StepTypeItems}\"", xaml);
        Assert.Contains("SelectedValue=\"{Binding SelectedType}\"", xaml);
        Assert.Contains("Width=\"300\"", xaml);
        Assert.DoesNotContain("Kind=\"FormTextbox\"", xaml);
        Assert.Contains("<Grid x:Name=\"ItemContainer\">", xaml);
        Assert.Contains("<ColumnDefinition Width=\"5\"/>", xaml);
        Assert.Contains("<Border x:Name=\"ItemCard\" Grid.Column=\"2\"", xaml);
        Assert.Contains("x:Name=\"SelectionIndicator\"", xaml);
        Assert.Contains("TargetName=\"ItemCard\" Property=\"Background\" Value=\"{DynamicResource App.Brush.SurfaceHover}\"", xaml);
        Assert.Contains("TargetName=\"SelectionIndicator\" Property=\"Background\" Value=\"{DynamicResource App.Brush.SelectionBorder}\"", xaml);
        Assert.Contains("ContentTemplate=\"{StaticResource GeneratedEditor}\"", xaml);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DesktopAutomation.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
