using System;
using System.Collections.Generic;

namespace BitBucketPRs.Models
{
    public class PrOverviews
    {
        public List<PrOverview> Prs { get; set; }

        public DateTime LastUpdated { get; set; }

        public bool NewPrs { get; set; }
    }
}
