using System;
using System.Collections.Generic;
using System.Linq;
using ME.Models;

namespace ME.Data
{
    public class ReviewRepository
    {
        private const string FileName = "reviews";

        public List<Review> GetAllReviews()
        {
            return JsonStore.Load<Review>(FileName)
                .OrderByDescending(r => r.ReviewDate).ToList();
        }

        public int InsertReview(Review review)
        {
            var reviews = JsonStore.Load<Review>(FileName);
            var maxId = reviews.Count > 0 ? reviews.Max(r => r.Id) : 0;
            review.Id = maxId + 1;
            review.CreatedAt = DateTime.Now;
            reviews.Add(review);
            JsonStore.Save(FileName, reviews);
            return review.Id;
        }
    }
}
