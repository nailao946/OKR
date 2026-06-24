using System.Windows;
using System.Windows.Controls;
using ME.ViewModels;
using ME.Views;

namespace ME
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm;
        private GoalsView _goalsView;
        private TasksView _tasksView;
        private CalendarView _calendarView;
        private MapView _mapView;
        private DashboardView _dashboardView;
        private ReviewView _reviewView;
        private RecycleBinView _recycleBinView;
        private SettingsView _settingsView;
        private TimeTrackView _timeTrackView;
        private UserControl _currentView;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainWindowViewModel();
            DataContext = _vm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateView(0);
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedIndex >= 0)
            {
                UpdateView(NavList.SelectedIndex);
            }
        }

        private void UpdateView(int index)
        {
            if (_currentView != null)
                _currentView.Visibility = Visibility.Collapsed;

            switch (index)
            {
                case 0: ShowView(ref _tasksView, () => new TasksView(), "任务列表"); break;
                case 1: ShowView(ref _goalsView, () => new GoalsView(), "目标管理"); break;
                case 2: ShowView(ref _calendarView, () => new CalendarView(), "日历视图"); break;
                case 3: ShowView(ref _mapView, () => new MapView(), "目标地图"); break;
                case 4: ShowView(ref _dashboardView, () => new DashboardView(), "数据看板"); break;
                case 5: ShowView(ref _reviewView, () => new ReviewView(), "定期盘点"); break;
                case 6: ShowView(ref _timeTrackView, () => new TimeTrackView(), "时间追踪"); break;
                case 7: ShowView(ref _settingsView, () => new SettingsView(), "设置"); break;
            }
        }

        private void ShowView<T>(ref T view, System.Func<T> create, string title) where T : UserControl
        {
            if (view == null)
            {
                view = create();
                view.Visibility = Visibility.Collapsed;
                ContentGrid.Children.Add(view);
            }

            if (_currentView != null && _currentView != view)
                _currentView.Visibility = Visibility.Collapsed;

            view.Visibility = Visibility.Visible;
            _currentView = view;
            TitleText.Text = title;
        }
    }
}
