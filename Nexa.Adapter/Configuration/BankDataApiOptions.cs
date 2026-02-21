namespace Nexa.Adapter.Configuration
{
    public class BankDataApiOptions
    {
        /// <summary>
        /// config section name
        /// </summary>
        public const string configSectionName = "BankDataApi";
        /// <summary>
        /// Bank Api client id
        /// </summary>
        public string ClientId { get; set; } = default!;
        /// <summary>
        /// Bank Api client secret
        /// </summary>
        public string ClientSecret { get; set; } = default!;

        /// <summary>
        /// 
        /// </summary>
        public string BaseUrl { get; set; } = default!;
    }
}