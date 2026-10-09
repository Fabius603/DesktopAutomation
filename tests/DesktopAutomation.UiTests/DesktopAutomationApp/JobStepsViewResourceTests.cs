using System.Xml.Linq;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepsViewResourceTests
{
    [Fact]
    public void WholeValueEditors_GroupComponentsAndExposeWholeSourceCommands()
    {
        var root = RepositoryRoot();
        var selectorXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedWholeValueSourceSelector.xaml"));
        var generatedEditorXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));
        var roiXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Roi", "RoiEditor.xaml"));
        var pointEntryXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Analysis",
            "PointEntryEditor.xaml"));
        var stylesXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Styles", "StepEditors.xaml"));

        Assert.DoesNotContain("<CheckBox", selectorXaml);
        Assert.DoesNotContain("ValueReferencePicker", selectorXaml);
        Assert.Contains("Ui.Job.StepInput.Source.WholeValueToolTip", selectorXaml);
        Assert.Contains("UseIndividualValuesCommand", selectorXaml);
        Assert.Contains("DataContext=\"{Binding WholeValueSource}\"", generatedEditorXaml);
        Assert.Contains("DataContext=\"{Binding WholeValueSource.Picker}\"", generatedEditorXaml);
        Assert.Contains("ElementName=ProcessTargetValueHost", generatedEditorXaml);
        Assert.Contains("ElementName=ScreenPointValueHost", generatedEditorXaml);
        Assert.Contains("ElementName=PointPairValueHost", generatedEditorXaml);
        Assert.Contains("CompoundValueSourceGroup", generatedEditorXaml);
        Assert.Contains("x:Key=\"CompoundValueSourceGroup\"", stylesXaml);

        var processTemplateIndex = generatedEditorXaml.IndexOf(
            "<DataTemplate x:Key=\"ProcessTargetFieldTemplate\">", StringComparison.Ordinal);
        var processGroupIndex = generatedEditorXaml.IndexOf(
            "Style=\"{StaticResource CompoundValueSourceGroup}\"", processTemplateIndex, StringComparison.Ordinal);
        var processCriteriaIndex = generatedEditorXaml.IndexOf(
            "Content=\"{Binding ProcessTargetEditor.ManualSourceContent}\"", processGroupIndex, StringComparison.Ordinal);
        var processSourceIndex = generatedEditorXaml.IndexOf(
            "<generated:GeneratedWholeValueSourceSelector", processCriteriaIndex, StringComparison.Ordinal);
        Assert.True(processTemplateIndex >= 0 && processTemplateIndex < processGroupIndex);
        Assert.True(processGroupIndex < processCriteriaIndex && processCriteriaIndex < processSourceIndex);

        var pointGroupIndex = pointEntryXaml.IndexOf("CompoundValueSourceGroup", StringComparison.Ordinal);
        var pointValuesIndex = pointEntryXaml.IndexOf("ResponsiveGeometryPanel", StringComparison.Ordinal);
        var pointSourceIndex = pointEntryXaml.IndexOf(
            "<generated:GeneratedWholeValueSourceSelector", StringComparison.Ordinal);
        Assert.True(pointGroupIndex >= 0 && pointGroupIndex < pointValuesIndex);
        Assert.True(pointValuesIndex < pointSourceIndex);
        Assert.Contains(
            "Visibility=\"{Binding DataContext.WholeValueSource.UsesReference, ElementName=Root",
            pointEntryXaml);

        Assert.Contains("CompoundValueSourceGroup", roiXaml);
        Assert.Contains(
            "Visibility=\"{Binding DataContext.WholeValueSource.UsesReference, ElementName=Root",
            roiXaml);
        Assert.DoesNotContain(
            "<Grid Visibility=\"{Binding IsRoiEnabled, ElementName=Root, Converter={StaticResource BooleanToVisibilityConverter}}\">",
            roiXaml);
    }

    [Fact]
    public void ResultPathPicker_DoesNotBindPressedStateOrNullColorsThroughBrushConversion()
    {
        var root = RepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "ResultPathPicker.xaml"));
        var styles = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Styles", "ReferencePicker.xaml"));

        Assert.DoesNotContain(
            "Binding IsPressed, RelativeSource={RelativeSource TemplatedParent}", styles);
        Assert.Contains("<Trigger Property=\"IsPressed\" Value=\"True\">", styles);
        Assert.Contains(
            "Background=\"{Binding ColorPreview, TargetNullValue=Transparent, FallbackValue=Transparent}\"",
            xaml);
    }

    [Fact]
    public void ResultPathPicker_UsesPixelViewportForNestedVirtualizedLists()
    {
        Assert.True(global::DesktopAutomationApp.Controls.Jobs.ResultPathPicker.IsTargetInsideViewport(
            new System.Windows.Point(12, 48),
            new System.Windows.Size(420, 34),
            new System.Windows.Size(500, 120)));
        Assert.False(global::DesktopAutomationApp.Controls.Jobs.ResultPathPicker.IsTargetInsideViewport(
            new System.Windows.Point(12, 121),
            new System.Windows.Size(420, 34),
            new System.Windows.Size(500, 120)));
    }

    [Fact]
    public void ChoiceGroupEditor_UsesInstanceLocalArbitrarySelection()
    {
        var root = RepositoryRoot();
        var controlPath = Path.Combine(root, "DesktopAutomationApp", "Controls", "Jobs", "Shared", "ChoiceGroupEditor.xaml");
        Assert.True(File.Exists(controlPath));
        var xaml = File.ReadAllText(controlPath);

        Assert.Contains("ItemsSource=\"{Binding ItemsSource, ElementName=Root}\"", xaml);
        Assert.Contains("SelectedItem=\"{Binding SelectedItem, ElementName=Root, Mode=TwoWay}\"", xaml);
        Assert.Contains("<controls:ResponsiveColumnsPanel", xaml);
        Assert.Contains("MinItemWidth=\"130\"", xaml);
        Assert.Contains("HorizontalContentAlignment=\"Stretch\"", xaml);
        Assert.DoesNotContain("WrapPanel", xaml);
        Assert.DoesNotContain("<UniformGrid Rows=\"1\"/>", xaml);
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
    public void StepDetailsAdvancedHeader_UsesAFullWidthHitTarget()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));
        var styleStart = xaml.IndexOf(
            "<Style x:Key=\"StepDetailsExpanderStyle\"", StringComparison.Ordinal);
        var styleEnd = xaml.IndexOf("</Style>", styleStart, StringComparison.Ordinal);
        var style = xaml[styleStart..styleEnd];

        Assert.Contains("MinHeight=\"32\"", style);
        Assert.Contains("HorizontalAlignment=\"Stretch\"", style);
        Assert.Contains("Background=\"Transparent\"", style);
        Assert.Contains("<Border Background=\"{TemplateBinding Background}\"", style);
        Assert.Contains("Padding=\"{TemplateBinding Padding}\"", style);
    }

    [Fact]
    public void ReturnValueRows_ToggleChildrenAcrossTheFullRow()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));
        var styleStart = xaml.IndexOf(
            "<Style x:Key=\"StepResultTreeItemStyle\"", StringComparison.Ordinal);
        var styleEnd = xaml.IndexOf(
            "<Style x:Key=\"JobStepSectionExpanderStyle\"", styleStart, StringComparison.Ordinal);
        var style = xaml[styleStart..styleEnd];

        Assert.Contains("x:Name=\"ResultRowToggle\"", style);
        Assert.Contains("Grid.Row=\"0\" Grid.ColumnSpan=\"2\"", style);
        Assert.Contains("HorizontalAlignment=\"Stretch\"", style);
        Assert.Contains("IsChecked=\"{Binding IsExpanded, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}\"", style);
        Assert.Contains("<Trigger Property=\"HasItems\" Value=\"False\">", style);
        Assert.Contains("Property=\"IsHitTestVisible\" Value=\"False\"", style);
    }

    [Fact]
    public void ReturnValueRows_UseTheInputValueFontSize()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));
        var templateStart = xaml.IndexOf(
            "<HierarchicalDataTemplate x:Key=\"StepResultPropertyTemplate\"", StringComparison.Ordinal);
        var templateEnd = xaml.IndexOf("</HierarchicalDataTemplate>", templateStart, StringComparison.Ordinal);
        var template = xaml[templateStart..templateEnd];

        Assert.Contains("TextElement.FontSize=\"14\"", template);
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
    public void DateTimeInputs_UseTheSharedApplicationStyle()
    {
        var root = RepositoryRoot();
        var controlsXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Styles", "Controls.xaml"));
        var generatedEditorXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));
        var variableEditorXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "JobVariableValueEditor.xaml"));
        var automationEditorXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Views", "AutomationsView", "AutomationDetailView.xaml"));

        Assert.Contains("x:Key=\"AppDateTimePickerStyle\"", controlsXaml);
        Assert.Contains("Style=\"{StaticResource AppDateTimePickerStyle}\"", generatedEditorXaml);
        Assert.Contains("BasedOn=\"{StaticResource AppDateTimePickerStyle}\"", variableEditorXaml);
        Assert.Contains("Style=\"{StaticResource AppDateTimePickerStyle}\"", automationEditorXaml);
    }

    [Fact]
    public void StepFieldLabels_WrapAboveInputsInNarrowEditors()
    {
        var stylesXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Styles", "StepEditors.xaml"));
        var editorXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.Contains("<Setter Property=\"TextWrapping\" Value=\"Wrap\"/>", stylesXaml);
        Assert.Contains("<StackPanel VerticalAlignment=\"Top\" Margin=\"0,0,0,6\"", editorXaml);
    }

    [Fact]
    public void GeneratedStepEditor_CreatesOnlyTheSelectedFieldEditor()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.Contains("GeneratedStepFieldTemplateSelector", xaml);
        Assert.Contains("ContentTemplateSelector=\"{StaticResource GeneratedStepFieldTemplateSelector}\"", xaml);
        Assert.DoesNotContain("x:Key=\"ValueReferenceFieldTemplate\"", xaml);
        Assert.Contains("x:Key=\"VisualOverlayFieldTemplate\"", xaml);
        Assert.Contains("x:Key=\"RoiFieldTemplate\"", xaml);
        Assert.Contains("x:Key=\"WindowsCapabilityFieldTemplate\"", xaml);
        Assert.DoesNotContain("Visibility=\"{Binding UsesValueReferencePicker,", xaml);
        Assert.DoesNotContain("Visibility=\"{Binding UsesVisualOverlay,", xaml);
        Assert.DoesNotContain("Visibility=\"{Binding UsesRoiPicker,", xaml);
        Assert.DoesNotContain("Visibility=\"{Binding UsesWindowsCapabilityPicker,", xaml);
    }

    [Fact]
    public void GeneratedValueSourceInput_ResolvesVisibilityFromItsOwnFieldModel()
    {
        var reusableInputXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedValueSourceInput.xaml"));
        var generatedEditorXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.Contains(
            "DataContext.ShowsInputSourcePicker, RelativeSource={RelativeSource AncestorType={x:Type UserControl}}",
            reusableInputXaml);
        Assert.DoesNotContain("DataContext.ShowsInputSourcePicker, ElementName=InputValueHost", reusableInputXaml);
        Assert.Contains(
            "<Grid Visibility=\"{Binding ShowsInputSourcePicker, Converter={StaticResource BooleanToVisibilityConverter}}\">",
            generatedEditorXaml);
        Assert.DoesNotContain("DataContext.ShowsInputSourcePicker, ElementName=InputValueHost", generatedEditorXaml);
    }

    [Fact]
    public void StepOverview_ProvidesInspectorEditingAndAccessibleBlockActions()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));
        XNamespace ui = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        Assert.DoesNotContain(document.Descendants(ui + "Button"), button =>
            button.Attribute("Command")?.Value.Contains("EditStepCommand", StringComparison.Ordinal) == true);
        Assert.Contains(document.Descendants(ui + "Button"), button =>
            button.Attribute("AutomationProperties.AutomationId")?.Value == "ToggleJobStepBlock");
        Assert.DoesNotContain(document.Descendants(ui + "Button"), button =>
            button.Attribute("AutomationProperties.AutomationId")?.Value == "AddStepToEmptyBranch");
    }

    [Fact]
    public void VisualOverlayEditor_UsesNonVirtualizedRowHostsForReferencePickers()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Output",
            "VisualOverlayEditor.xaml"));

        var document = System.Xml.Linq.XDocument.Parse(xaml);
        System.Xml.Linq.XNamespace ui = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var hosts = document.Descendants(ui + "ListBox").Where(element => element.Attribute("ItemsSource")?.Value.StartsWith("{Binding Overlay", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(2, hosts.Length);
        Assert.All(hosts, host => Assert.Equal("False", host.Attribute("VirtualizingPanel.IsVirtualizing")?.Value));
        Assert.All(hosts, host => Assert.Equal("Extended", host.Attribute("SelectionMode")?.Value));
        Assert.DoesNotContain("VirtualizedOverlayItem", xaml);
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
    public void GeneratedSemanticChoiceEditor_UsesSharedChoiceGroupEditor()
    {
        var root = RepositoryRoot();
        var file = Path.Combine(root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated", "GeneratedStepEditor.xaml");
        Assert.Contains("ChoiceGroupEditor", File.ReadAllText(file));
    }

    [Fact]
    public void ConditionComparison_UsesSharedVariableInput()
    {
        var root = RepositoryRoot();
        var file = Path.Combine(root, "DesktopAutomationApp", "Controls", "Jobs", "Conditions", "ConditionEditor.xaml");
        var xaml = File.ReadAllText(file);
        var generatedXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));
        Assert.Contains("GeneratedValueSourceInput", xaml);
        Assert.Contains("x:Name=\"ConditionComparisonGrid\"", xaml);
        Assert.Contains("A condition reads from left to right", xaml);
        Assert.Contains("Visibility=\"{Binding UsesConditionEditor, Converter={StaticResource InverseBooleanToVisibilityConverter}}\"", generatedXaml);
        Assert.Contains("<Setter Property=\"Grid.RowSpan\" Value=\"2\"/>", generatedXaml);
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
    public void StepCatalog_OffersOrdinaryTypesAndIfWithoutStandaloneAlternativeOrClosingMarkers()
    {
        var catalog = global::DesktopAutomationApp.ViewModels.AddJobStepDialogViewModel
            .CreateStepTypeItems(global::TaskAutomation.Steps.Definitions.BuiltInStepDefinitions.Instance)
            .Cast<global::DesktopAutomationApp.ViewModels.AddJobStepDialogViewModel.StepTypeItem>().ToArray();
        Assert.Contains(catalog, item => item.Name == "If");
        Assert.Contains(catalog, item => item.Name == "Timeout");
        Assert.DoesNotContain(catalog, item => item.Name is "ElseIf" or "Else" or "EndIf");
        Assert.All(catalog, item =>
        {
            Assert.NotEmpty(item.DisplayLabel);
            Assert.NotEmpty(item.Description);
            Assert.NotEmpty(item.Category);
        });
    }

    [Fact]
    public void JobStepsView_ExposesPermanentMasterDetailInspectorAndStatus()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));

        Assert.Contains("AutomationProperties.AutomationId=\"JobStepsStatusBar\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"SelectedJobStepInspector\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"SelectedJobStepTabs\"", xaml);
        Assert.Contains("Style=\"{StaticResource InspectorPageTabControlStyle}\"", xaml);
        Assert.Contains("x:Name=\"PART_InspectorSelectionPill\"", xaml);
        Assert.Contains("<ItemsPresenter Grid.ColumnSpan=\"3\"/>", xaml);
        Assert.Contains("Grid.Column=\"0\" HorizontalAlignment=\"Stretch\"", xaml);
        Assert.DoesNotContain("HorizontalAlignment=\"Left\" Width=\"0\"", xaml);
        Assert.Contains("SelectionChanged=\"SelectedJobStepTabs_SelectionChanged\"", xaml);
        Assert.Contains("Header=\"{loc:Translate Key=Ui.Job.Steps.Editor.Details}\"", xaml);
        Assert.Contains("Header=\"{loc:Translate Key=Ui.Job.Steps.Editor.ReturnValue}\"", xaml);
        Assert.Contains("Header=\"{loc:Translate Key=Ui.Job.Steps.Editor.Debug}\"", xaml);
        Assert.Contains("Content=\"{Binding SelectedStepEditor}\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding SelectedDebugContextGroups}\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding Result.Properties}\"", xaml);
        var document = System.Xml.Linq.XDocument.Parse(xaml);
        var inspector = document.Descendants().Single(element =>
            (string?)element.Attribute("AutomationProperties.AutomationId") == "SelectedJobStepInspector");
        Assert.DoesNotContain(inspector.Descendants(), element =>
            (string?)element.Attribute("HorizontalScrollBarVisibility") == "Auto");
        Assert.Contains("x:Name=\"SelectionOverlay\"", xaml);
        Assert.Contains("TargetName=\"SelectionOverlay\" Property=\"Background\" Value=\"{DynamicResource App.Brush.SelectionBackground}\"", xaml);
    }

    [Fact]
    public void GeneratedStepEditor_StacksFieldsAndLetsEnumPickersFitNarrowInspectors()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));

        Assert.Contains("x:Key=\"GeneratedFieldTemplate\"", xaml);
        Assert.Contains("<Grid.RowDefinitions><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"Auto\"/></Grid.RowDefinitions>", xaml);
        Assert.Contains("<ComboBox MinWidth=\"0\" HorizontalAlignment=\"Stretch\"", xaml);
        Assert.Contains("TextSearch.TextPath=\"Label\"", xaml);
        Assert.Contains("SelectedValuePath=\"Value\"", xaml);
        Assert.Contains("SelectedValue=\"{Binding SelectedEnumValue, Mode=TwoWay}\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"GeneratedStepEnumPicker\"", xaml);
        Assert.DoesNotContain("MinWidth=\"220\"", xaml);
    }

    [Fact]
    public void InspectorEditors_WrapCompoundFieldsAndDisableHorizontalScrolling()
    {
        var root = RepositoryRoot();
        var conditionXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Conditions", "ConditionEditor.xaml"));
        var generatedXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated", "GeneratedStepEditor.xaml"));
        var axisXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Analysis", "AxisExpressionEditor.xaml"));
        var roiXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Controls", "Jobs", "Roi", "RoiEditor.xaml"));

        Assert.Contains("<controls:ResponsiveColumnsPanel x:Name=\"ConditionComparisonGrid\"", conditionXaml);
        Assert.Contains("<controls:ResponsiveColumnsPanel MinItemWidth=\"170\" MaxColumns=\"2\"", conditionXaml);
        Assert.Contains("TextWrapping=\"Wrap\"", conditionXaml);
        Assert.DoesNotContain("<TextBlock Text=\"{Binding ComparisonValueValidationError}\"", conditionXaml);
        Assert.Contains("AutomationProperties.HelpText=\"{Binding ComparisonValueValidationError}\"", conditionXaml);
        Assert.DoesNotContain("MinWidth=\"150\"", conditionXaml);
        Assert.DoesNotContain("MinWidth=\"115\"", conditionXaml);
        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"", conditionXaml);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", conditionXaml);
        Assert.Contains("BasedOn=\"{StaticResource StepInspectorScrollBarStyle}\"", conditionXaml);
        Assert.Contains("<controls:ResponsiveColumnsPanel Grid.Row=\"1\"", axisXaml);
        Assert.DoesNotContain("<ColumnDefinition Width=\"80\"/>", axisXaml);
        Assert.DoesNotContain("<ColumnDefinition Width=\"120\"/>", axisXaml);
        Assert.Contains("<geometry:ResponsiveGeometryPanel MinItemWidth=\"150\" MaxColumns=\"2\"", roiXaml);
        Assert.DoesNotContain("SharedSizeGroup=\"GeometryLabel\"", roiXaml);
        Assert.Equal(4, CountOccurrences(roiXaml, "Style=\"{StaticResource GeometryFieldLabel}\""));
        Assert.Equal(4, CountOccurrences(generatedXaml, "ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\""));
        Assert.Equal(3, CountOccurrences(generatedXaml, "ScrollViewer.VerticalScrollBarVisibility=\"Hidden\""));

        var jobStepsXaml = File.ReadAllText(Path.Combine(
            root, "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));
        Assert.Contains(
            "<ScrollViewer VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Disabled\">",
            jobStepsXaml);
        Assert.Contains("AutomationProperties.AutomationId=\"InspectorResizeGrip\"", jobStepsXaml);
        Assert.Contains("Style=\"{StaticResource PaneGridSplitterStyle}\"", jobStepsXaml);
        Assert.Contains("x:Name=\"ResizeGrip\"", File.ReadAllText(Path.Combine(RepositoryRoot(), "DesktopAutomationApp", "Styles", "Controls.xaml")));
        Assert.Contains("BasedOn=\"{StaticResource StepInspectorScrollBarStyle}\"", jobStepsXaml);
    }

    private static int CountOccurrences(string value, string fragment)
    {
        var count = 0;
        var start = 0;
        while ((start = value.IndexOf(fragment, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += fragment.Length;
        }

        return count;
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DesktopAutomation.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
