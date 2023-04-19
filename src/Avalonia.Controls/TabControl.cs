using System.Linq;
using Avalonia.Collections;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Automation;
using Avalonia.Controls.Metadata;
using System;
using Avalonia.Controls.Generators;

namespace Avalonia.Controls
{
    /// <summary>
    /// A tab control that displays a tab strip along with the content of the selected tab.
    /// </summary>
    [TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
    public class TabControl : SelectingItemsControl, IContentPresenterHost
    {
        /// <summary>
        /// Defines the <see cref="TabStripPlacement"/> property.
        /// </summary>
        public static readonly StyledProperty<Dock> TabStripPlacementProperty =
            AvaloniaProperty.Register<TabControl, Dock>(nameof(TabStripPlacement), defaultValue: Dock.Top);

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<TabControl>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<TabControl>();

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
            ContentControl.ContentTemplateProperty.AddOwner<TabControl>();

        /// <summary>
        /// The selected content property
        /// </summary>
        public static readonly StyledProperty<object?> SelectedContentProperty =
            AvaloniaProperty.Register<TabControl, object?>(nameof(SelectedContent));

        /// <summary>
        /// The selected content template property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> SelectedContentTemplateProperty =
            AvaloniaProperty.Register<TabControl, IDataTemplate?>(nameof(SelectedContentTemplate));
        
        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new WrapPanel());

        /// <summary>
        /// Initializes static members of the <see cref="TabControl"/> class.
        /// </summary>
        static TabControl()
        {
            SelectionModeProperty.OverrideDefaultValue<TabControl>(SelectionMode.AlwaysSelected);
            ItemsPanelProperty.OverrideDefaultValue<TabControl>(DefaultPanel);
            AffectsMeasure<TabControl>(TabStripPlacementProperty);
            SelectedItemProperty.Changed.AddClassHandler<TabControl>((x, e) => x.UpdateSelectedContent());
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<TabControl>(AutomationControlType.Tab);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the tabstrip placement of the TabControl.
        /// </summary>
        public Dock TabStripPlacement
        {
            get { return GetValue(TabStripPlacementProperty); }
            set { SetValue(TabStripPlacementProperty, value); }
        }

        /// <summary>
        /// Gets or sets the default data template used to display the content of the selected tab.
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the content of the selected tab.
        /// </summary>
        /// <value>
        /// The content of the selected tab.
        /// </value>
        public object? SelectedContent
        {
            get { return GetValue(SelectedContentProperty); }
            internal set { SetValue(SelectedContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the content template for the selected tab.
        /// </summary>
        /// <value>
        /// The content template of the selected tab.
        /// </value>
        public IDataTemplate? SelectedContentTemplate
        {
            get { return GetValue(SelectedContentTemplateProperty); }
            internal set { SetValue(SelectedContentTemplateProperty, value); }
        }

        internal ItemsPresenter? ItemsPresenterPart { get; private set; }

        internal IContentPresenter? ContentPart { get; private set; }

        /// <inheritdoc/>
        IAvaloniaList<ILogical> IContentPresenterHost.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        bool IContentPresenterHost.RegisterContentPresenter(IContentPresenter presenter)
        {
            return RegisterContentPresenter(presenter);
        }

        protected internal override Control CreateContainerForItemOverride(ItemContainerType type)
        {
            if (type != ItemContainerType.Default)
                throw new InvalidOperationException("Unsupported ItemContainerType.");
            return new TabItem();
        }

        protected internal override ItemContainerType GetContainerTypeForItemOverride(object? item)
        {
            return item is TabItem ? ItemContainerType.ItemIsOwnContainer : ItemContainerType.Default;
        }

        protected internal override void PrepareContainerForItemOverride(Control element, object? item, int index)
        {
            base.PrepareContainerForItemOverride(element, item, index);
            
            if (element is TabItem tabItem)
            {
                if (ContentTemplate is { } ct)
                    tabItem.ContentTemplate = ct;
                tabItem.SetValue(TabStripPlacementProperty, TabStripPlacement);
            }

            if (index == SelectedIndex && element is ContentControl container)
            {
                SelectedContentTemplate = container.ContentTemplate;
                SelectedContent = container.Content;
            }
        }

        protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
        {
            base.ContainerIndexChangedOverride(container, oldIndex, newIndex);

            var selectedIndex = SelectedIndex;
            
            if (selectedIndex == oldIndex || selectedIndex == newIndex)
                UpdateSelectedContent();
        }

        protected internal override void ClearContainerForItemOverride(Control element)
        {
            base.ClearContainerForItemOverride(element);
            UpdateSelectedContent();
        }

        private void UpdateSelectedContent()
        {
            if (SelectedIndex == -1)
            {
                SelectedContent = SelectedContentTemplate = null;
            }
            else
            {
                var container = SelectedItem as IContentControl ??
                    ContainerFromIndex(SelectedIndex) as IContentControl;
                SelectedContentTemplate = container?.ContentTemplate;
                SelectedContent = container?.Content;
            }
        }

        /// <summary>
        /// Called when an <see cref="IContentPresenter"/> is registered with the control.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        protected virtual bool RegisterContentPresenter(IContentPresenter presenter)
        {
            if (presenter.Name == "PART_SelectedContentHost")
            {
                ContentPart = presenter;
                return true;
            }

            return false;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            ItemsPresenterPart = e.NameScope.Get<ItemsPresenter>("PART_ItemsPresenter");
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Pointer.Type == PointerType.Mouse)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left && e.Pointer.Type != PointerType.Mouse)
            {
                var container = GetContainerFromEventSource(e.Source);
                if (container != null
                    && container.GetVisualsAt(e.GetPosition(container))
                        .Any(c => container == c || container.IsVisualAncestorOf(c)))
                {
                    e.Handled = UpdateSelectionFromEventSource(e.Source);
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TabStripPlacementProperty)
                RefreshContainers();
        }
    }
}
