using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ME.Models;

namespace ME.Views
{
    public partial class RecordEditDialog : Window
    {
        public DateTime ResultStartTime { get; private set; }
        public DateTime? ResultEndTime { get; private set; }
        public int ResultTagId { get; private set; }

        private readonly List<TimeTag> _tags;

        public RecordEditDialog(TimeRecord record, List<TimeTag> tags)
        {
            InitializeComponent();
            _tags = tags.ToList();

            StartTimeBox.Text = record.StartTime.ToString("yyyy-MM-dd HH:mm");
            if (record.EndTime.HasValue)
            {
                EndTimeBox.Text = record.EndTime.Value.ToString("yyyy-MM-dd HH:mm");
                NoEndTimeCheck.IsChecked = false;
            }
            else
            {
                EndTimeBox.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                NoEndTimeCheck.IsChecked = true;
                EndTimeBox.IsEnabled = false;
            }

            TagComboBox.ItemsSource = _tags;
            TagComboBox.SelectedItem = _tags.FirstOrDefault(t => t.Id == record.TagId);
        }

        private void NoEndTime_Changed(object sender, RoutedEventArgs e)
        {
            if (EndTimeBox != null)
                EndTimeBox.IsEnabled = NoEndTimeCheck.IsChecked != true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!DateTime.TryParse(StartTimeBox.Text, out var start))
            {
                MessageBox.Show("开始时间格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime? end = null;
            if (NoEndTimeCheck.IsChecked != true)
            {
                if (!DateTime.TryParse(EndTimeBox.Text, out var endVal))
                {
                    MessageBox.Show("结束时间格式不正确", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                end = endVal;
            }

            ResultStartTime = start;
            ResultEndTime = end;
            ResultTagId = (TagComboBox.SelectedItem as TimeTag)?.Id ?? 0;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
