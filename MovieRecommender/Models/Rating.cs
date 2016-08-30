using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MovieRecommender.Models
{
    public class Rating
    {
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public float Preference { get; set; }

    }
}