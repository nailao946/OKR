using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ME.Core;
using ME.Models;

namespace ME.ViewModels
{
    public class ReviewViewModel : ViewModelBase
    {
        private ObservableCollection<Review> _reviews;
        private Review _selectedReview;
        private int _reviewType;

        public ObservableCollection<Review> Reviews
        {
            get => _reviews;
            set => SetProperty(ref _reviews, value);
        }

        public Review SelectedReview
        {
            get => _selectedReview;
            set => SetProperty(ref _selectedReview, value);
        }

        public int ReviewType
        {
            get => _reviewType;
            set => SetProperty(ref _reviewType, value);
        }

        public ICommand GenerateWeeklyCommand { get; }
        public ICommand GenerateMonthlyCommand { get; }
        public ICommand SaveReviewCommand { get; }

        private readonly Services.ReviewService _reviewService;

        public ReviewViewModel()
        {
            _reviewService = new Services.ReviewService();
            Reviews = new ObservableCollection<Review>();
            LoadReviews();

            GenerateWeeklyCommand = new RelayCommand(_ => GenerateWeekly());
            GenerateMonthlyCommand = new RelayCommand(_ => GenerateMonthly());
            SaveReviewCommand = new RelayCommand(_ => SaveReview(), _ => SelectedReview != null);
        }

        public void LoadReviews()
        {
            Reviews.Clear();
            var reviews = _reviewService.GetAllReviews();
            foreach (var r in reviews)
                Reviews.Add(r);
        }

        private void GenerateWeekly()
        {
            var review = _reviewService.GenerateWeeklyReview();
            var id = _reviewService.SaveReview(review);
            review.Id = id;
            Reviews.Insert(0, review);
            SelectedReview = review;
        }

        private void GenerateMonthly()
        {
            var review = _reviewService.GenerateWeeklyReview();
            review.Type = Models.ReviewType.Monthly;
            var id = _reviewService.SaveReview(review);
            review.Id = id;
            Reviews.Insert(0, review);
            SelectedReview = review;
        }

        private void SaveReview()
        {
            if (SelectedReview != null)
            {
                _reviewService.SaveReview(SelectedReview);
            }
        }
    }
}
