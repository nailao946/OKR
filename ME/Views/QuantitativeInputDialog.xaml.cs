using System;
using System.Windows;
using System.Windows.Input;
using ME.Models;

namespace ME.Views
{
    public partial class QuantitativeInputDialog : Window
    {
        public double NewValue { get; private set; }
        private readonly TaskItem _task;

        public QuantitativeInputDialog(TaskItem task)
        {
            InitializeComponent();
            _task = task;

            TaskTitleText.Text = task.Title;
            var current = task.QuantitativeCurrent ?? task.QuantitativeStart ?? 0;
            var target = task.QuantitativeTarget ?? 0;
            var unit = task.QuantitativeUnit ?? "";

            NewValue = current;

            if (task.QuantitativeMode == QuantitativeMode.Accumulate)
            {
                ModeText.Text = $"模式：求和（累加）| 单位：{unit}";
                InputLabel.Text = $"输入要增加的数值（{unit}）";
            }
            else
            {
                ModeText.Text = $"模式：更新（覆盖）| 单位：{unit}";
                InputLabel.Text = $"输入新的数值（{unit}）";
            }

            CurrentValueText.Text = $"{current} {unit}";
            TargetValueText.Text = $"{target} {unit}";

            var progress = target > 0 ? Math.Min(current / target * 100, 100) : 0;
            ProgressBar.Value = progress;
            ProgressText.Text = $"{progress:F1}%";

            ValueInput.Focus();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(ValueInput.Text, out double input))
            {
                ConfirmDialog.Show(this, "提示", "请输入有效数值", "确定");
                return;
            }

            var current = _task.QuantitativeCurrent ?? _task.QuantitativeStart ?? 0;

            if (_task.QuantitativeMode == QuantitativeMode.Accumulate)
            {
                NewValue = current + input;
            }
            else
            {
                NewValue = input;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Confirm_Click(sender, e);
            }
        }
    }
}
