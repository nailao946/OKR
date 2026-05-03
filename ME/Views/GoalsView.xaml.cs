using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ME.Data;
using ME.Models;
using ME.Services;
using ME.ViewModels;
using ME.Core;

namespace ME.Views
{
    public partial class GoalsView : System.Windows.Controls.UserControl
    {
        private GoalsViewModel _vm;
        private int? _filterTagId;

        // Drag state
        private bool _isDragging;
        private Point _dragStart;
        private Border _draggedBorder;
        private int _dragSourceIndex;
        private List<Goal> _dragSourceList;
        private StackPanel _dragPanel;
        private StackPanel _dragMainPanel;
        private Border _placeholderBorder;

        public GoalsView()
        {
            InitializeComponent();
            _vm = new GoalsViewModel();
            DataContext = _vm;
            BuildTagFilter();
            LoadGoalsWithSections();
            EventAggregator.Instance.Subscribe<string>(OnTagChanged);
        }

        private void OnTagChanged(string message)
        {
            if (message == "TagChanged")
            {
                BuildTagFilter();
                LoadGoalsWithSections();
            }
        }

        private void GoalsView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible) LoadGoalsWithSections();
        }

        private void BuildTagFilter()
        {
            TagFilterPanel.Children.Clear();
            var tagRepo = new TagRepository();
            var tags = tagRepo.GetAllTags();

            var allBtn = new Button
            {
                Content = "全部",
                Style = (Style)FindResource(_filterTagId == null ? "PrimaryButtonStyle" : "SecondaryButtonStyle"),
                Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 6, 0),
                FontSize = 12, Tag = (int?)null
            };
            allBtn.Click += TagFilter_Click;
            TagFilterPanel.Children.Add(allBtn);

            foreach (var tag in tags)
            {
                var btn = new Button
                {
                    Content = tag.Name,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tag.Color)),
                    Foreground = Brushes.White,
                    Style = (Style)FindResource(_filterTagId == tag.Id ? "PrimaryButtonStyle" : "SecondaryButtonStyle"),
                    Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 6, 0),
                    FontSize = 12, Tag = (int?)tag.Id
                };
                btn.Click += TagFilter_Click;
                TagFilterPanel.Children.Add(btn);
            }
        }

        private void TagFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                _filterTagId = (int?)btn.Tag;
                BuildTagFilter();
                LoadGoalsWithSections();
            }
        }

        private void LoadGoalsWithSections()
        {
            _vm.LoadGoals();
            var incomplete = new List<Goal>();
            var completed = new List<Goal>();

            foreach (var g in _vm.Goals)
            {
                if (_filterTagId.HasValue && g.TagId != _filterTagId.Value) continue;
                if (g.IsArchived) completed.Add(g);
                else incomplete.Add(g);
            }

            BuildGoalTree(IncompleteGoalsPanel, incomplete, false);
            BuildGoalTree(CompletedGoalsPanel, completed, true);
        }

        private void BuildGoalTree(StackPanel panel, List<Goal> goals, bool isCompleted)
        {
            panel.Children.Clear();

            foreach (var goal in goals)
            {
                var wrapper = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

                // Goal card
                var card = new Border
                {
                    Style = (Style)FindResource("CardStyle"),
                    Cursor = isCompleted ? Cursors.Hand : Cursors.SizeAll,
                    Tag = goal
                };
                SetupGoalDragDrop(card, goal, goals, panel);

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var progressColor = string.IsNullOrEmpty(goal.TagColor) ? (SolidColorBrush)FindResource("PrimaryBrush")
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString(goal.TagColor));

                // Completion circle
                bool isQuant = goal.QuantitativeTarget.HasValue && goal.QuantitativeTarget > 0;
                var circle = new Border
                {
                    Width = 24, Height = 24,
                    CornerRadius = isQuant ? new CornerRadius(4) : new CornerRadius(12),
                    BorderThickness = new Thickness(2), VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 0), Cursor = Cursors.Hand,
                    Background = isQuant ? (isCompleted ? (SolidColorBrush)FindResource("PrimaryBrush") : progressColor)
                        : (isCompleted ? (SolidColorBrush)FindResource("PrimaryBrush") : Brushes.Transparent),
                    BorderBrush = isCompleted ? (SolidColorBrush)FindResource("PrimaryBrush") : (SolidColorBrush)FindResource("BorderBrush"),
                    Child = new TextBlock
                    {
                        Text = isCompleted ? "✓" : (isQuant ? "+" : ""),
                        Foreground = Brushes.White, FontSize = isCompleted ? 12 : 14, FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                    }
                };
                circle.Tag = goal;
                circle.MouseLeftButtonDown += (s, e) => { GoalCircle_Click(s, e); e.Handled = true; };
                Grid.SetColumn(circle, 0);
                grid.Children.Add(circle);

                // Text area
                var textPanel = new StackPanel { IsHitTestVisible = false };

                // Tag badge + Name
                var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
                if (!string.IsNullOrEmpty(goal.TagName))
                {
                    var tagBadge = new Border
                    {
                        CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 8, 0),
                        Background = new SolidColorBrush(string.IsNullOrEmpty(goal.TagColor)
                            ? Color.FromRgb(0, 122, 255)
                            : (Color)ColorConverter.ConvertFromString(goal.TagColor)),
                        Child = new TextBlock { Text = goal.TagName, FontSize = 10, Foreground = Brushes.White }
                    };
                    namePanel.Children.Add(tagBadge);
                }
                namePanel.Children.Add(new TextBlock
                {
                    Text = goal.Name, FontSize = 16, FontWeight = FontWeights.SemiBold,
                    Foreground = isCompleted ? (SolidColorBrush)FindResource("SecondaryTextBrush") : (SolidColorBrush)FindResource("TextBrush"),
                    TextDecorations = isCompleted ? TextDecorations.Strikethrough : null
                });
                textPanel.Children.Add(namePanel);

                // Expired label for goal
                bool isGoalExpired = !isCompleted && goal.EndDate.HasValue && goal.EndDate.Value.Date < DateTime.Today;
                if (isGoalExpired)
                {
                    textPanel.Children.Add(new TextBlock
                    {
                        Text = "任务已过期", FontSize = 10, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 59, 48)),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }

                if (!isCompleted)
                {
                    if (!string.IsNullOrEmpty(goal.Description))
                    {
                        textPanel.Children.Add(new TextBlock
                        {
                            Text = goal.Description, FontSize = 11,
                            Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush"),
                            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0),
                            MaxHeight = 35, TextTrimming = TextTrimming.CharacterEllipsis
                        });
                    }

                    // Progress + time frame
                    var infoPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                    infoPanel.Children.Add(new TextBlock { Text = "进度:", FontSize = 10, Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush") });
                    infoPanel.Children.Add(new TextBlock
                    {
                        Text = $"{goal.Progress:F0}%", FontSize = 10, FontWeight = FontWeights.SemiBold,
                        Foreground = progressColor, Margin = new Thickness(3, 0, 8, 0)
                    });
                    var timeFrame = goal.TimeFrame == GoalTimeFrame.LongTerm ? "长期" : goal.TimeFrame == GoalTimeFrame.Inspiration ? "灵感" : "短期";
                    infoPanel.Children.Add(new TextBlock { Text = timeFrame, FontSize = 10, Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush") });
                    if (goal.EndDate.HasValue)
                    {
                        var deadlineColor = isGoalExpired 
                            ? new SolidColorBrush(Color.FromRgb(255, 59, 48))
                            : (SolidColorBrush)FindResource("SecondaryTextBrush");
                        infoPanel.Children.Add(new TextBlock
                        {
                            Text = $" 截止:{goal.EndDate.Value:yyyy/MM/dd}", FontSize = 10,
                            Foreground = deadlineColor, Margin = new Thickness(8, 0, 0, 0)
                        });
                    }
                    textPanel.Children.Add(infoPanel);

                    // Progress bar
                    var pb = new ProgressBar
                    {
                        Value = goal.Progress, Maximum = 100, Height = 8,
                        Margin = new Thickness(0, 5, 0, 0),
                        Background = (SolidColorBrush)FindResource("BackgroundBrush"),
                        Foreground = progressColor
                    };
                    textPanel.Children.Add(pb);
                }

                Grid.SetColumn(textPanel, 1);
                grid.Children.Add(textPanel);

                // Buttons
                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                btnPanel.Children.Add(CreateGoalButton("编辑", EditGoal_Click, goal));
                btnPanel.Children.Add(CreateGoalButton("子任务", AddSubtask_Click, goal));
                btnPanel.Children.Add(CreateGoalButton("删除", DeleteGoal_Click, goal));
                Grid.SetColumn(btnPanel, 2);
                grid.Children.Add(btnPanel);

                card.Child = grid;
                wrapper.Children.Add(card);

                // Subtasks
                if (goal.Subtasks != null && goal.Subtasks.Count > 0)
                {
                    var subtaskExpander = new Expander
                    {
                        IsExpanded = true,
                        Margin = new Thickness(24, 0, 0, 0),
                        Header = new TextBlock
                        {
                            Text = $"子任务 ({goal.Subtasks.Count})",
                            FontSize = 11,
                            Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush")
                        }
                    };
                    var subtaskPanel = new StackPanel();
                    foreach (var sub in goal.Subtasks)
                    {
                        subtaskPanel.Children.Add(CreateGoalSubtaskCard(sub, goal.TagColor));
                    }
                    subtaskExpander.Content = subtaskPanel;
                    wrapper.Children.Add(subtaskExpander);
                }

                panel.Children.Add(wrapper);
            }
        }

        private Border CreateGoalSubtaskCard(TaskItem task, string tagColor)
        {
            var card = new Border
            {
                Style = (Style)FindResource("CardStyle"),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand,
                Tag = task
            };

            var progressColor = string.IsNullOrEmpty(tagColor) ? (SolidColorBrush)FindResource("PrimaryBrush")
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString(tagColor));

            bool isCustomRecurring = task.Type == TaskType.Recurring && task.RecurringPattern == RecurringPattern.Custom && task.RecurringTargetCount.HasValue && task.RecurringTargetCount > 1;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Completion circle
            bool isQuant = task.Type == TaskType.Quantitative && task.QuantitativeTarget.HasValue && task.QuantitativeTarget > 0;
            var circle = new Border
            {
                Width = 18, Height = 18,
                CornerRadius = isQuant ? new CornerRadius(3) : new CornerRadius(9),
                BorderThickness = new Thickness(1.5), VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand,
                Background = isQuant ? progressColor : Brushes.Transparent,
                BorderBrush = task.IsCompleted ? progressColor : (SolidColorBrush)FindResource("BorderBrush"),
                Child = new TextBlock
                {
                    Text = task.IsCompleted ? "✓" : (isQuant ? "+" : (isCustomRecurring ? $"{task.RecurringCurrentCount ?? 0}" : "")),
                    Foreground = Brushes.White, FontSize = task.IsCompleted ? 8 : 10, FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                }
            };
            circle.Tag = task;
            circle.MouseLeftButtonDown += (s, e) => SubtaskCircle_Click(s, e);
            Grid.SetColumn(circle, 0);
            grid.Children.Add(circle);

            // Text area
            var textPanel = new StackPanel { IsHitTestVisible = false };
            textPanel.Children.Add(new TextBlock
            {
                Text = task.Title, FontSize = 12,
                Foreground = task.IsCompleted ? (SolidColorBrush)FindResource("SecondaryTextBrush") : (SolidColorBrush)FindResource("TextBrush"),
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null
            });

            // Expired label
            bool isExpired = !task.IsCompleted && task.EndDate.HasValue && task.EndDate.Value.Date < DateTime.Today;
            if (isExpired)
            {
                textPanel.Children.Add(new TextBlock
                {
                    Text = "任务已过期", FontSize = 10, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 59, 48)),
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            if (!task.IsCompleted)
            {
                // Description
                if (!string.IsNullOrEmpty(task.Description))
                {
                    textPanel.Children.Add(new TextBlock
                    {
                        Text = task.Description, FontSize = 10,
                        Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush"),
                        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0),
                        MaxHeight = 30, TextTrimming = TextTrimming.CharacterEllipsis
                    });
                }

                // Info panel: type + deadline
                var infoPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
                var typeText = task.Type == TaskType.Recurring ? "循环" : task.Type == TaskType.Quantitative ? "量化" : "单次";
                infoPanel.Children.Add(new TextBlock { Text = typeText, FontSize = 10, Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush") });
                if (task.EndDate.HasValue)
                {
                    var deadlineColor = isExpired 
                        ? new SolidColorBrush(Color.FromRgb(255, 59, 48))
                        : (SolidColorBrush)FindResource("SecondaryTextBrush");
                    infoPanel.Children.Add(new TextBlock
                    {
                        Text = $" 截止:{task.EndDate.Value:yyyy/MM/dd}", FontSize = 10,
                        Foreground = deadlineColor, Margin = new Thickness(6, 0, 0, 0)
                    });
                }
                textPanel.Children.Add(infoPanel);
            }

            // Custom recurring progress bar for subtask
            if (isCustomRecurring)
            {
                var current = task.RecurringCurrentCount ?? 0;
                var target = task.RecurringTargetCount ?? 1;
                var pct = target > 0 ? Math.Min((double)current / target * 100, 100) : 0;

                var pb = new ProgressBar
                {
                    Maximum = 100, Height = 4,
                    Margin = new Thickness(0, 4, 120, 0),
                    Background = (SolidColorBrush)FindResource("BackgroundBrush"),
                    Foreground = progressColor,
                    Value = pct
                };
                textPanel.Children.Add(pb);

                textPanel.Children.Add(new TextBlock
                {
                    Text = $"{current}/{target}",
                    FontSize = 9, FontWeight = FontWeights.SemiBold,
                    Foreground = progressColor,
                    Margin = new Thickness(0, 3, 0, 0)
                });
            }
            // Quantitative progress bar for subtask
            else if (isQuant)
            {
                var pb = new ProgressBar
                {
                    Maximum = 100, Height = 4,
                    Margin = new Thickness(0, 4, 120, 0),
                    Background = (SolidColorBrush)FindResource("BackgroundBrush"),
                    Foreground = progressColor,
                    Value = task.QuantitativeTarget > 0
                        ? Math.Min((task.QuantitativeCurrent ?? 0) / task.QuantitativeTarget.Value * 100, 100) : 0
                };
                textPanel.Children.Add(pb);

                textPanel.Children.Add(new TextBlock
                {
                    Text = $"{task.QuantitativeCurrent ?? 0:F0}/{task.QuantitativeTarget.Value:F0}",
                    FontSize = 9, FontWeight = FontWeights.SemiBold,
                    Foreground = progressColor,
                    Margin = new Thickness(0, 3, 0, 0)
                });
            }

            Grid.SetColumn(textPanel, 1);
            grid.Children.Add(textPanel);

            // Edit/Delete buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            btnPanel.Children.Add(CreateGoalButton("编辑", EditTaskFromGoal_Click, task));
            btnPanel.Children.Add(CreateGoalButton("删除", DeleteTaskFromGoal_Click, task));
            Grid.SetColumn(btnPanel, 2);
            grid.Children.Add(btnPanel);

            card.Child = grid;
            return card;
        }

        private Button CreateGoalButton(string content, RoutedEventHandler handler, object tag)
        {
            var btn = new Button
            {
                Content = content,
                Style = (Style)FindResource("SecondaryButtonStyle"),
                Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 4, 0),
                FontSize = 11, Tag = tag
            };
            btn.Click += handler;
            return btn;
        }

        // ============ DRAG AND DROP FOR GOALS ============
        private void SetupGoalDragDrop(Border card, Goal goal, List<Goal> sourceList, StackPanel mainPanel)
        {
            card.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var source = e.OriginalSource as DependencyObject;
                while (source != null)
                {
                    if (source is Button || source is Border b && b.Cursor == Cursors.Hand)
                        return;
                    source = VisualTreeHelper.GetParent(source);
                }
                _dragStart = e.GetPosition(null);
                _draggedBorder = card;
                _dragSourceList = sourceList;
                _dragSourceIndex = sourceList.IndexOf(goal);
                _dragPanel = VisualTreeHelper.GetParent(card) as StackPanel;
                _dragMainPanel = mainPanel;
            };

            card.PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton != MouseButtonState.Pressed || _draggedBorder == null) return;
                var pos = e.GetPosition(null);
                var diff = pos - _dragStart;

                if (!_isDragging && Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    _draggedBorder.Opacity = 0.5;
                    _draggedBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 10, ShadowDepth = 2, Opacity = 0.3, Color = Colors.Black
                    };
                    Mouse.Capture(_draggedBorder);

                    _placeholderBorder = new Border
                    {
                        Style = (Style)FindResource("CardStyle"),
                        Height = _draggedBorder.ActualHeight, Opacity = 0.3,
                        Margin = new Thickness(0, 0, 0, 12),
                        Background = (SolidColorBrush)FindResource("PrimaryBrush"),
                        IsHitTestVisible = false
                    };
                    if (_dragMainPanel != null && _dragPanel != null)
                    {
                        int srcIdx = _dragMainPanel.Children.IndexOf(_dragPanel);
                        if (srcIdx >= 0)
                            _dragMainPanel.Children.Insert(srcIdx + 1, _placeholderBorder);
                    }
                }

                if (_isDragging && _dragMainPanel != null)
                {
                    // Remove placeholder first, calculate drop index, then re-insert
                    bool hadPlaceholder = _dragMainPanel.Children.Contains(_placeholderBorder);
                    if (hadPlaceholder) _dragMainPanel.Children.Remove(_placeholderBorder);

                    var mousePos = e.GetPosition(_dragMainPanel);
                    int dropIndex = 0;
                    for (int i = 0; i < _dragMainPanel.Children.Count; i++)
                    {
                        var child = _dragMainPanel.Children[i] as FrameworkElement;
                        if (child == null) continue;
                        var transform = child.TransformToAncestor(_dragMainPanel);
                        var childPos = transform.Transform(new Point(0, 0));
                        if (mousePos.Y < childPos.Y + child.ActualHeight / 2)
                        {
                            dropIndex = i;
                            break;
                        }
                        dropIndex = i + 1;
                    }

                    int insertIndex = Math.Min(dropIndex, _dragMainPanel.Children.Count);
                    if (insertIndex >= 0)
                        _dragMainPanel.Children.Insert(insertIndex, _placeholderBorder);
                }
            };

            card.PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (_isDragging && _draggedBorder != null)
                {
                    _draggedBorder.Opacity = 1.0;
                    _draggedBorder.Effect = null;
                    Mouse.Capture(null);

                    // Remove placeholder and calculate final drop index
                    if (_placeholderBorder != null && _dragMainPanel != null && _dragMainPanel.Children.Contains(_placeholderBorder))
                        _dragMainPanel.Children.Remove(_placeholderBorder);

                    var mousePos = e.GetPosition(_dragMainPanel);
                    int dropIndex = 0;
                    for (int i = 0; i < _dragMainPanel.Children.Count; i++)
                    {
                        var child = _dragMainPanel.Children[i] as FrameworkElement;
                        if (child == null) continue;
                        var transform = child.TransformToAncestor(_dragMainPanel);
                        var childPos = transform.Transform(new Point(0, 0));
                        if (mousePos.Y < childPos.Y + child.ActualHeight / 2)
                        {
                            dropIndex = i;
                            break;
                        }
                        dropIndex = i + 1;
                    }

                    if (dropIndex != _dragSourceIndex)
                    {
                        var item = _dragSourceList[_dragSourceIndex];
                        _dragSourceList.RemoveAt(_dragSourceIndex);
                        if (dropIndex > _dragSourceIndex) dropIndex--;
                        _dragSourceList.Insert(dropIndex, item);

                        var repo = new GoalRepository();
                        for (int i = 0; i < _dragSourceList.Count; i++)
                        {
                            _dragSourceList[i].SortOrder = i;
                            repo.UpdateGoal(_dragSourceList[i]);
                        }
                        LoadGoalsWithSections();
                    }
                    else
                    {
                        _placeholderBorder = null;
                    }
                }
                _isDragging = false;
                _draggedBorder = null;
                _dragPanel = null;
                _dragMainPanel = null;
            };
        }

        private void GoalCircle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Goal goal)
            {
                // Quantitative goal - open input dialog
                if (goal.QuantitativeTarget.HasValue && goal.QuantitativeTarget > 0 && !goal.IsArchived)
                {
                    var taskProxy = new TaskItem
                    {
                        Type = TaskType.Quantitative,
                        QuantitativeMode = goal.QuantitativeMode ?? QuantitativeMode.Accumulate,
                        QuantitativeStart = goal.QuantitativeStart,
                        QuantitativeTarget = goal.QuantitativeTarget,
                        QuantitativeCurrent = goal.QuantitativeCurrent,
                        QuantitativeUnit = goal.QuantitativeUnit
                    };
                    var dialog = new QuantitativeInputDialog(taskProxy) { Owner = Window.GetWindow(this) };
                    if (dialog.ShowDialog() == true)
                    {
                        var repo = new GoalRepository();
                        goal.QuantitativeCurrent = dialog.NewValue;
                        if (goal.QuantitativeTarget.HasValue && goal.QuantitativeTarget.Value > 0)
                        {
                            goal.Progress = Math.Min(goal.QuantitativeCurrent.Value / goal.QuantitativeTarget.Value * 100, 100);
                            if (goal.Progress >= 100)
                            {
                                goal.IsArchived = true;
                                goal.Progress = 100;
                            }
                        }
                        repo.UpdateGoal(goal);
                        SoundService.PlayCompletionSound();
                        LoadGoalsWithSections();
                    }
                    return;
                }

                var repo2 = new GoalRepository();
                goal.IsArchived = !goal.IsArchived;
                goal.Progress = goal.IsArchived ? 100 : 0;
                repo2.UpdateGoal(goal);
                SoundService.PlayCompletionSound();
                LoadGoalsWithSections();
            }
        }

        private void AddGoal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GoalEditDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && dialog.ResultGoal != null)
            {
                var repo = new GoalRepository();
                var id = repo.InsertGoal(dialog.ResultGoal);
                dialog.ResultGoal.Id = id;
                dialog.PersistSubtasks();
                LoadGoalsWithSections();
            }
        }

        private void EditGoal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Goal goal)
            {
                var dialog = new GoalEditDialog(goal) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true && dialog.ResultGoal != null)
                {
                    dialog.ResultGoal.Id = goal.Id;
                    var repo = new GoalRepository();
                    repo.UpdateGoal(dialog.ResultGoal);
                    dialog.PersistSubtasks();
                    LoadGoalsWithSections();
                }
            }
        }

        private void AddSubtask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Goal goal)
            {
                var dialog = new TaskEditDialog(isSubtaskMode: true) { Owner = Window.GetWindow(this), Title = "添加子任务" };
                if (dialog.ShowDialog() == true && dialog.ResultTask != null)
                {
                    dialog.ResultTask.GoalId = goal.Id;
                    var repo = new TaskRepository();
                    var id = repo.InsertTask(dialog.ResultTask);
                    dialog.ResultTask.Id = id;

                    // Recalculate goal progress if CountTowardsParent
                    if (dialog.ResultTask.CountTowardsParent)
                        RecalcGoalProgressFromSubtasks(goal.Id, repo);

                    LoadGoalsWithSections();
                }
            }
        }

        private void DeleteGoal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Goal goal)
            {
                var repo = new GoalRepository();
                repo.SoftDeleteGoal(goal.Id);
                LoadGoalsWithSections();
            }
        }

        private void RestoreGoal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Goal goal)
            {
                var repo = new GoalRepository();
                goal.IsArchived = false;
                goal.Progress = 0;
                repo.UpdateGoal(goal);
                SoundService.PlayCompletionSound();
                LoadGoalsWithSections();
            }
        }

        private void ManageTags_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TagEditDialog { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
            BuildTagFilter();
        }

        private void SubtaskCircle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is TaskItem task)
            {
                if (task.Type == TaskType.Quantitative && task.QuantitativeMode.HasValue)
                {
                    var dialog = new QuantitativeInputDialog(task) { Owner = Window.GetWindow(this) };
                    if (dialog.ShowDialog() == true)
                    {
                        var repo = new TaskRepository();
                        var oldValue = task.QuantitativeCurrent ?? 0;
                        task.QuantitativeCurrent = dialog.NewValue;
                        var delta = task.QuantitativeCurrent.Value - oldValue;
                        if (task.QuantitativeTarget.HasValue && task.QuantitativeCurrent >= task.QuantitativeTarget.Value)
                        {
                            task.IsCompleted = true;
                            task.CompletedAt = DateTime.Now;
                        }
                        repo.UpdateTask(task);

                        if (task.CountTowardsParent)
                        {
                            // Sync delta to parent task
                            if (task.ParentTaskId.HasValue)
                                SyncParentTaskProgress(task.ParentTaskId.Value, delta, repo);

                            // Recalculate goal progress from all CountTowardsParent subtasks
                            if (task.GoalId.HasValue)
                                RecalcGoalProgressFromSubtasks(task.GoalId.Value, repo);
                        }

                        SoundService.PlayCompletionSound();
                        LoadGoalsWithSections();
                    }
                    return;
                }

                var repo2 = new TaskRepository();
                var taskService = new TaskService();

                // For recurring tasks, handle completion differently
                if (task.Type == TaskType.Recurring && task.RecurringPattern.HasValue)
                {
                    // For custom recurring tasks with multiple times per day
                    if (task.RecurringPattern == RecurringPattern.Custom && task.RecurringTargetCount.HasValue && task.RecurringTargetCount > 1)
                    {
                        // Get the actual task from repository to update
                        var actualTask = repo2.GetTaskById(task.Id);
                        if (actualTask == null) return;

                        // Check if completed on this date
                        bool isCompletedToday = taskService.IsRecurringTaskCompletedOnDate(actualTask, DateTime.Today);
                        if (isCompletedToday)
                        {
                            // Already completed for today, do nothing
                            return;
                        }

                        // Increment count
                        actualTask.RecurringCurrentCount = (actualTask.RecurringCurrentCount ?? 0) + 1;

                        // Check if reached target
                        if (actualTask.RecurringCurrentCount >= actualTask.RecurringTargetCount)
                        {
                            actualTask.IsCompleted = true;
                            actualTask.CompletedAt = DateTime.Now;
                            actualTask.LastCompletedDate = DateTime.Today;
                        }

                        repo2.UpdateTask(actualTask);
                    }
                    else
                    {
                        // Toggle completion for today
                        bool isCompletedToday = taskService.IsRecurringTaskCompletedOnDate(task, DateTime.Today);
                        if (isCompletedToday)
                        {
                            // Uncomplete for today
                            task.LastCompletedDate = null;
                            task.IsCompleted = false;
                            task.CompletedAt = null;
                        }
                        else
                        {
                            // Complete for today
                            task.LastCompletedDate = DateTime.Today;
                            task.IsCompleted = true;
                            task.CompletedAt = DateTime.Now;
                        }
                        repo2.UpdateTask(task);
                    }
                }
                else
                {
                    // For non-recurring tasks, toggle completion normally
                    task.IsCompleted = !task.IsCompleted;
                    task.CompletedAt = task.IsCompleted ? DateTime.Now : (DateTime?)null;
                    repo2.UpdateTask(task);
                }

                // Recalculate goal progress when a subtask is toggled
                if (task.GoalId.HasValue)
                    RecalcGoalProgressFromSubtasks(task.GoalId.Value, repo2);

                SoundService.PlayCompletionSound();
                
                // Notify dashboard to refresh
                EventAggregator.Instance.Publish("TaskCompleted");
                
                LoadGoalsWithSections();
            }
        }

        private void SyncParentTaskProgress(int parentTaskId, double delta, TaskRepository repo)
        {
            var allTasks = repo.GetAllTasks();
            var parent = allTasks.Find(t => t.Id == parentTaskId && !t.IsDeleted);
            if (parent != null && parent.Type == TaskType.Quantitative)
            {
                parent.QuantitativeCurrent = (parent.QuantitativeCurrent ?? 0) + delta;
                if (parent.QuantitativeTarget.HasValue && parent.QuantitativeCurrent >= parent.QuantitativeTarget.Value)
                {
                    parent.IsCompleted = true;
                    parent.CompletedAt = DateTime.Now;
                }
                repo.UpdateTask(parent);

                // Also sync to goal if parent task belongs to a goal
                if (parent.GoalId.HasValue)
                    RecalcGoalProgressFromSubtasks(parent.GoalId.Value, repo);
            }
        }

        private void RecalcGoalProgressFromSubtasks(int goalId, TaskRepository repo)
        {
            var goalRepo = new GoalRepository();
            var goal = goalRepo.GetAllGoals().Find(g => g.Id == goalId && !g.IsDeleted);
            if (goal == null) return;

            var allTasks = repo.GetTasksByGoalId(goalId);

            if (goal.QuantitativeTarget.HasValue && goal.QuantitativeTarget > 0)
            {
                // Quantitative goal: sum CountTowardsParent subtasks
                double total = 0;
                foreach (var t in allTasks)
                {
                    if (!t.IsDeleted && t.CountTowardsParent && t.Type == TaskType.Quantitative)
                        total += t.QuantitativeCurrent ?? 0;
                }
                goal.QuantitativeCurrent = total;
                goal.Progress = Math.Min(total / goal.QuantitativeTarget.Value * 100, 100);
                if (goal.Progress >= 100)
                {
                    goal.IsArchived = true;
                    goal.Progress = 100;
                }
            }
            else
            {
                // Non-quantitative goal: progress from completed task count
                if (allTasks.Count == 0) return;
                int completed = 0;
                foreach (var t in allTasks)
                {
                    if (t.IsCompleted) completed++;
                }
                goal.Progress = (double)completed / allTasks.Count * 100;
                if (goal.Progress >= 100)
                {
                    goal.IsArchived = true;
                    goal.Progress = 100;
                }
            }

            goalRepo.UpdateGoal(goal);
        }

        private void EditTaskFromGoal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is TaskItem task)
            {
                var dialog = new TaskEditDialog(task) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true && dialog.ResultTask != null)
                {
                    dialog.ResultTask.Id = task.Id;
                    var repo = new TaskRepository();
                    repo.UpdateTask(dialog.ResultTask);
                    dialog.PersistSubtasks();
                    LoadGoalsWithSections();
                }
            }
        }

        private void DeleteTaskFromGoal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is TaskItem task)
            {
                var repo = new TaskRepository();

                // Subtract from parent task before deleting
                if (task.CountTowardsParent && task.ParentTaskId.HasValue)
                {
                    var delta = -(task.QuantitativeCurrent ?? 0);
                    SyncParentTaskProgress(task.ParentTaskId.Value, delta, repo);
                }

                repo.SoftDeleteTask(task.Id);

                // Recalc goal after delete (deleted task is now excluded from sum)
                if (task.GoalId.HasValue)
                    RecalcGoalProgressFromSubtasks(task.GoalId.Value, repo);

                LoadGoalsWithSections();
            }
        }
    }
}
