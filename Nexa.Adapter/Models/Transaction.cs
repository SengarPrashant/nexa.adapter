namespace Nexa.Adapter.Models
{
    public class Transaction
    {
        public string TransactionId { get; set; }

        public string Type { get; set; }

        public string TransactionType { get; set; }

        public string Status { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public string Channel { get; set; }

        public DateTime Timestamp { get; set; }

        public string SourceAccount { get; set; }

        public string DestinationAccount { get; set; }

        public AdditionalData AdditionalData { get; set; }

        public GeoLocation Geolocation { get; set; }
    }

    public class AdditionalData
    {
        public string MerchantName { get; set; }

        public string MerchantCategory { get; set; }
    }

    public class GeoLocation
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string City { get; set; }

        public string Country { get; set; }

        public string IpAddress { get; set; }
    }
}
