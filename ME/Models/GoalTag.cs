using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ME.Models
{
    public class GoalTag : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int _id;
        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }

        private string _name;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _color;
        /// <summary>
        /// Hex color string, e.g. "#FF3B30"
        /// </summary>
        public string Color { get => _color; set { _color = value; OnPropertyChanged(); } }

        private int _sortOrder;
        public int SortOrder { get => _sortOrder; set { _sortOrder = value; OnPropertyChanged(); } }

        private DateTime _createdAt;
        public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); } }
    }
}
