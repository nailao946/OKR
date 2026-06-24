using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using ME.Models;
using ME.Data;
using ME.Core;

namespace ME.Views
{
    public partial class TagEditDialog : Window
    {
        public GoalTag ResultTag { get; private set; }
        private string _selectedColor = "#007AFF";
        private readonly TagRepository _tagRepo = new TagRepository();
        private int? _editingTagId = null;

        public TagEditDialog()
        {
            InitializeComponent();
            UpdateColorSelection();
            LoadTagList();
        }

        private void LoadTagList()
        {
            TagListPanel.Children.Clear();
            var tags = _tagRepo.GetAllTags();

            if (tags.Count == 0)
            {
                TagListPanel.Children.Add(new TextBlock
                {
                    Text = "暂无标签",
                    FontSize = 12,
                    Foreground = (SolidColorBrush)FindResource("SecondaryTextBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (var tag in tags)
            {
                var card = new Border
                {
                    Style = (Style)FindResource("CardStyle"),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Color circle
                var colorCircle = new Border
                {
                    Width = 24, Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                try
                {
                    colorCircle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tag.Color));
                }
                catch
                {
                    colorCircle.Background = Brushes.Gray;
                }
                Grid.SetColumn(colorCircle, 0);
                grid.Children.Add(colorCircle);

                // Tag name
                var nameText = new TextBlock
                {
                    Text = tag.Name,
                    FontSize = 13,
                    Foreground = (SolidColorBrush)FindResource("TextBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameText, 1);
                grid.Children.Add(nameText);

                // Buttons panel
                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var editBtn = new Button
                {
                    Content = "编辑",
                    Style = (Style)FindResource("SecondaryButtonStyle"),
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 11,
                    Tag = tag.Id,
                    Margin = new Thickness(0, 0, 4, 0)
                };
                editBtn.Click += EditTag_Click;
                btnPanel.Children.Add(editBtn);

                var deleteBtn = new Button
                {
                    Content = "删除",
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 11,
                    Tag = tag.Id,
                    Background = new SolidColorBrush(Color.FromRgb(255, 59, 48)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                deleteBtn.Click += DeleteTag_Click;
                btnPanel.Children.Add(deleteBtn);

                Grid.SetColumn(btnPanel, 2);
                grid.Children.Add(btnPanel);

                card.Child = grid;
                TagListPanel.Children.Add(card);
            }
        }

        private void EditTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int tagId)
            {
                var tag = _tagRepo.GetTagById(tagId);
                if (tag == null) return;

                _editingTagId = tagId;
                TagNameBox.Text = tag.Name;
                _selectedColor = tag.Color;
                FormTitle.Text = "编辑标签";
                CancelEditBtn.Visibility = Visibility.Visible;
                UpdateColorSelection();
            }
        }

        private void DeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int tagId)
            {
                if (ConfirmDialog.Show(this, "确认删除", "确定要删除这个标签吗？\n关联该标签的目标将取消标签关联。", "删除", "取消"))
                {
                    _tagRepo.DeleteTag(tagId);
                    if (_editingTagId == tagId)
                    {
                        ResetForm();
                    }
                    LoadTagList();
                    EventAggregator.Instance.Publish("TagChanged");
                }
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.OriginalSource is System.Windows.Controls.Border)
                DragMove();
        }

        private void ResetForm()
        {
            _editingTagId = null;
            TagNameBox.Text = "";
            _selectedColor = "#007AFF";
            FormTitle.Text = "创建新标签";
            CancelEditBtn.Visibility = Visibility.Collapsed;
            UpdateColorSelection();
        }

        private void Color_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string color)
            {
                _selectedColor = color;
                UpdateColorSelection();
            }
        }

        private void UpdateColorSelection()
        {
            foreach (var child in ColorPanel.Children)
            {
                if (child is Border b && b.Tag is string tag)
                {
                    bool isSelected = string.Equals(tag, _selectedColor, System.StringComparison.OrdinalIgnoreCase);
                    if (isSelected)
                    {
                        b.BorderBrush = Brushes.White;
                        b.BorderThickness = new Thickness(3);
                        b.Width = 44;
                        b.Height = 44;
                        b.CornerRadius = new CornerRadius(22);
                    }
                    else
                    {
                        b.BorderBrush = Brushes.Transparent;
                        b.BorderThickness = new Thickness(2);
                        b.Width = 38;
                        b.Height = 38;
                        b.CornerRadius = new CornerRadius(19);
                    }
                }
            }

            // Update preview and text box
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(_selectedColor);
                ColorPreview.Background = new SolidColorBrush(color);
                CustomColorBox.Text = _selectedColor;
            }
            catch { }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TagNameBox.Text))
            {
                ConfirmDialog.Show(this, "提示", "请输入标签名称", "确定");
                TagNameBox.Focus();
                return;
            }

            if (_editingTagId.HasValue)
            {
                // Update existing tag
                var existingTag = _tagRepo.GetTagById(_editingTagId.Value);
                if (existingTag != null)
                {
                    existingTag.Name = TagNameBox.Text.Trim();
                    existingTag.Color = _selectedColor;
                    _tagRepo.UpdateTag(existingTag);
                    ResultTag = existingTag;
                }
            }
            else
            {
                // Create new tag
                ResultTag = new GoalTag
                {
                    Name = TagNameBox.Text.Trim(),
                    Color = _selectedColor,
                    CreatedAt = System.DateTime.Now
                };
                _tagRepo.InsertTag(ResultTag);
            }

            DialogResult = true;
            Close();
        }

        private void PickColor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            dialog.FullOpen = true;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(_selectedColor);
                dialog.Color = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            }
            catch { }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dialog.Color;
                _selectedColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                UpdateColorSelection();
            }
        }

        private void ApplyCustomColor_Click(object sender, RoutedEventArgs e)
        {
            var colorText = CustomColorBox.Text.Trim();
            if (string.IsNullOrEmpty(colorText)) return;
            if (!colorText.StartsWith("#")) colorText = "#" + colorText;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorText);
                _selectedColor = colorText.ToUpper();
                // Add a new circle for the custom color if not already present
                bool found = false;
                foreach (var child in ColorPanel.Children)
                {
                    if (child is Border b && b.Tag is string tag 
                        && string.Equals(tag, _selectedColor, System.StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    var customBorder = new Border
                    {
                        Width = 38, Height = 38, CornerRadius = new CornerRadius(19),
                        Background = new SolidColorBrush(color),
                        Margin = new Thickness(0, 0, 10, 10),
                        Cursor = Cursors.Hand,
                        Tag = _selectedColor,
                        BorderBrush = Brushes.Transparent,
                        BorderThickness = new Thickness(3)
                    };
                    customBorder.MouseLeftButtonDown += Color_Click;
                    ColorPanel.Children.Add(customBorder);
                }
                UpdateColorSelection();
            }
            catch
            {
                ConfirmDialog.Show(this, "提示", "请输入有效的颜色值，如 #FF5500", "确定");
            }
        }
    }
}
