﻿namespace BitBucketPRs.Models
{
    public class RootObject
    {
        public Value[] Values { get; set; }
    }

    public class Value
    {
        public string Slug { get; set; }

        public Links Links { get; set; }

        public string Title { get; set; }
    }

    public class Links
    {
        public Self[] Self { get; set; }
    }

    public class Self
    {
        public string Href { get; set; }
    }
}