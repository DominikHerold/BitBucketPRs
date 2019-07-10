namespace BitBucketPRs.Configuration
{
    public class PrConfiguration
    {
        public string Host { get; set; }

        public string ProjectKey { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string[] BlockedLinks { get; set; }
    }
}
