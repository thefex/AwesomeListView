using System;
using System.Windows.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using AwesomeListView.WindowsPhone.Extensions;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace AwesomeListView
{
    public sealed class AwesomeListView : ListView
    {
        private const string ScrollViewerControl = "ScrollViewer";
        private const string ContainerControl = "Container";
        private const string PullToRefreshIndicator = "PullToRefreshIndicator";
        private const string LoadingControl = "LoadingContentPresenter";
        private const string EmptyViewControl = "EmptyContentPresenter";
        private const string PullToRefreshFixRectHackControl = "PullRefreshScrollHeightFix";

        public static readonly DependencyProperty RefreshHeaderHeightProperty =
            DependencyProperty.Register("RefreshHeaderHeight", typeof(double), typeof(AwesomeListView),
                new PropertyMetadata(100D));

        public static readonly DependencyProperty RefreshCommandProperty = DependencyProperty.Register(
            "RefreshCommand", typeof(ICommand), typeof(AwesomeListView), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadingContentProperty =
            DependencyProperty.Register("LoadingContent", typeof(object), typeof(AwesomeListView),
                new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadingTemplateProperty =
            DependencyProperty.Register("LoadingTemplate", typeof(DataTemplate), typeof(AwesomeListView),
                new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for EmptyViewContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EmptyViewContentProperty =
            DependencyProperty.Register("EmptyViewContent", typeof(object), typeof(AwesomeListView),
                new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for EmptyViewTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EmptyViewTemplateProperty =
            DependencyProperty.Register("EmptyViewTemplate", typeof(DataTemplate), typeof(AwesomeListView),
                new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PullToRefreshTemplateProperty =
            DependencyProperty.Register("PullToRefreshTemplate", typeof(DataTemplate), typeof(AwesomeListView),
                new PropertyMetadata(default(DataTemplate)));

        // Using a DependencyProperty as the backing store for HasAnyItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasAnyItemsProperty =
            DependencyProperty.Register("HasAnyItems", typeof(bool), typeof(AwesomeListView),
                new PropertyMetadata(false, HasAnyItemsPropertyChanged));

        // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(AwesomeListView),
                new PropertyMetadata(true, IsLoadedPropertyChanged));

        private readonly double offsetThreshold = 100;

        private DispatcherTimer compressionTimer;
        private Border container;
        private ContentPresenter emptyContentView;
        private bool isCompressedEnough;
        private bool isCompressionTimerRunning;
        private bool isReadyToRefresh;
        private ItemsPresenter itemsPresenter;
        private ContentPresenter loadingContentView;
        private Rectangle pullToRefreshFixHeightRect;
        private Grid pullToRefreshIndicator;
        private ScrollViewer scrollViewer;
        private DispatcherTimer timer;

        public AwesomeListView()
        {
            DefaultStyleKey = typeof(AwesomeListView);
        }

        public bool HasAnyItems
        {
            get { return (bool)GetValue(HasAnyItemsProperty); }
            set { SetValue(HasAnyItemsProperty, value); }
        }

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }


        public object LoadingContent
        {
            get { return GetValue(LoadingContentProperty); }
            set { SetValue(LoadingContentProperty, value); }
        }


        public DataTemplate LoadingTemplate
        {
            get { return (DataTemplate)GetValue(LoadingTemplateProperty); }
            set { SetValue(LoadingTemplateProperty, value); }
        }

        public object EmptyViewContent
        {
            get { return GetValue(EmptyViewContentProperty); }
            set { SetValue(EmptyViewContentProperty, value); }
        }

        public DataTemplate EmptyViewTemplate
        {
            get { return (DataTemplate)GetValue(EmptyViewTemplateProperty); }
            set { SetValue(EmptyViewTemplateProperty, value); }
        }

        public DataTemplate PullToRefreshTemplate
        {
            get { return (DataTemplate)GetValue(PullToRefreshTemplateProperty); }
            set { SetValue(PullToRefreshTemplateProperty, value); }
        }

        public ICommand RefreshCommand
        {
            get { return (ICommand)GetValue(RefreshCommandProperty); }
            set { SetValue(RefreshCommandProperty, value); }
        }

        public double RefreshHeaderHeight
        {
            get { return (double)GetValue(RefreshHeaderHeightProperty); }
            set { SetValue(RefreshHeaderHeightProperty, value); }
        }

        private static void HasAnyItemsPropertyChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            var awesomeListView = dependencyObject as AwesomeListView;
            if (awesomeListView == null)
                return;

            if (!awesomeListView.IsLoading)
                awesomeListView.HandleEmptyView();
        }

        private static async void IsLoadedPropertyChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            var awesomeListView = dependencyObject as AwesomeListView;
            var isLoading = (bool)args.NewValue;

            if (awesomeListView.loadingContentView == null)
                return;

            await Window.Current.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (isLoading)
                {
                    awesomeListView.loadingContentView.FadeIn();
                    awesomeListView.emptyContentView.FadeOut();
                    awesomeListView.pullToRefreshIndicator.FadeOut();
                    awesomeListView.itemsPresenter.FadeOut();

                }
                else
                {
                    awesomeListView.loadingContentView.FadeOut();
                    awesomeListView.pullToRefreshIndicator.FadeIn();
                    awesomeListView.itemsPresenter.FadeIn();
                    awesomeListView.HandleEmptyView();
                }
            });
        }

        private void HandleEmptyView()
        {
            if (emptyContentView == null || IsLoading)
                return;

            if (HasAnyItems)
            {
                emptyContentView.FadeOut();
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                emptyContentView.FadeIn();
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }

        public event EventHandler RefreshContent;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            scrollViewer = (ScrollViewer)GetTemplateChild(ScrollViewerControl);
            scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            scrollViewer.Margin = new Thickness(0, 0, 0, -RefreshHeaderHeight);
            var transform = new CompositeTransform();
            transform.TranslateY = -RefreshHeaderHeight;
            scrollViewer.RenderTransform = transform;
            container = (Border)GetTemplateChild(ContainerControl);
            pullToRefreshIndicator = (Grid)GetTemplateChild(PullToRefreshIndicator);

            pullToRefreshFixHeightRect = (Rectangle)GetTemplateChild(PullToRefreshFixRectHackControl);
            pullToRefreshFixHeightRect.Height = Window.Current.CoreWindow.Bounds.Height * 0.95f;
            loadingContentView = (ContentPresenter)GetTemplateChild(LoadingControl);
            emptyContentView = (ContentPresenter)GetTemplateChild(EmptyViewControl);
            itemsPresenter = (ItemsPresenter)GetTemplateChild("ItemsPresenter");

            SizeChanged += OnSizeChanged;
            Loaded += PullToRefreshScrollViewer_Loaded;

            if (IsLoading)
                loadingContentView.FadeIn();
        }

        /// <summary>
        ///     Initiate timers to detect if we're scrolling into negative space
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PullToRefreshScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(60);
            timer.Tick += Timer_Tick;

            compressionTimer = new DispatcherTimer();
            compressionTimer.Interval = TimeSpan.FromMilliseconds(30);
            compressionTimer.Tick += CompressionTimer_Tick;

            StartTimer(timer);
        }

        /// <summary>
        ///     Clip the bounds of the control to avoid showing the pull to refresh text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
            };
        }

        /// <summary>
        ///     Detect if we've scrolled all the way to the top. Stop timers when we're not completely in the top
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            if (Math.Abs(e.NextView.VerticalOffset) < 1)
            {
                StartTimer(timer);
            }
            else
            {
                StopTimer(timer);
                StopTimer(compressionTimer);


                isCompressionTimerRunning = false;
                isCompressedEnough = false;
                isReadyToRefresh = false;

                OnNormalStateRequested();
            }
        }

        /// <summary>
        ///     Detect if I've scrolled far enough and been there for enough time to refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompressionTimer_Tick(object sender, object e)
        {
            if (isCompressedEnough)
            {
                OnRefreshStateRequested();
                isReadyToRefresh = true;
            }
            else
                isCompressedEnough = false;

            StopTimer(compressionTimer);
            isCompressionTimerRunning = false;

        }

        /// <summary>
        ///     Invoke timer if we've scrolled far enough up into negative space. If we get back to offset 0 the refresh command
        ///     and event is invoked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, object e)
        {
            if (container != null)
            {
                var elementBounds =
                    pullToRefreshIndicator.TransformToVisual(container)
                        .TransformBounds(new Rect(0.0, 0.0, pullToRefreshIndicator.Height, RefreshHeaderHeight));
                var compressionOffset = elementBounds.Bottom;
                OnPullOffsetChanged(compressionOffset / offsetThreshold);

                if (compressionOffset > offsetThreshold)
                {
                    if (isCompressionTimerRunning == false)
                    {
                        isCompressionTimerRunning = true;
                        StartTimer(compressionTimer);
                    }

                    isCompressedEnough = true;
                }
                else if (compressionOffset <= double.Epsilon && isReadyToRefresh)
                {
                    InvokeRefresh();
                }
            }
        }

        /// <summary>
        ///     Set correct visual state and invoke refresh event and command
        /// </summary>
        private void InvokeRefresh()
        {
            isReadyToRefresh = false;
            OnNormalStateRequested();

            RefreshContent?.Invoke(this, EventArgs.Empty);

            if (RefreshCommand != null && RefreshCommand.CanExecute(null))
            {
                RefreshCommand.Execute(null);
            }
        }

        public event Action NormalStateRequested;
        public event Action RefreshStateRequested;
        public event Action<double> PullOffsetChanged;

        private void OnNormalStateRequested()
        {
            NormalStateRequested?.Invoke();
        }

        private void OnRefreshStateRequested()
        {
            RefreshStateRequested?.Invoke();
        }

        private void OnPullOffsetChanged(double obj)
        {
            if (emptyContentView != null)
                emptyContentView.RenderTransform = new TranslateTransform() { Y = obj * 8.5 * Math.Pow(6, obj) };
            PullOffsetChanged?.Invoke(obj);
        }

        private void StartTimer(DispatcherTimer dispatcherTimer)
        {
            if (dispatcherTimer != null && !dispatcherTimer.IsEnabled)
                dispatcherTimer.Start();
        }

        public void StopTimer(DispatcherTimer dispatcherTimer)
        {
            if (dispatcherTimer != null && dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop();
        }
    }
}