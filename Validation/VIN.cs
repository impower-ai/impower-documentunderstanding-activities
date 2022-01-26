using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Impower.DocumentUnderstanding.Validation
{
    public class InvalidVinException : Exception
    {
        public InvalidVinException(string message) : base(message)
        {
        }
    }

    internal static class ValidationExtensions
    {
        private static Regex vinRegex = new Regex(@"[^A-HJ-NPR-Za-hj-npr-z\d]");
        public static readonly string[] VinQueryFilter = {
                "Error Code",
                "Error Text",
                "Make",
                "Manufacturer Name",
                "Model",
                "Model Year",
                "Trim",
                "Vehicle Type",
                "Doors",
                "Body Class"
        };
        internal static JObject GetVinInfoFromNHTSA(string vin)
        {
            HttpWebResponse response = null;
            var url = "https://vpic.nhtsa.dot.gov/api/vehicles/decodevin/" + HttpUtility.UrlEncode(vin) + "?format=json";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Accept = "*/*";
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(
                retryCount: 3, sleepDurationProvider: _ => TimeSpan.FromSeconds(3)
            );
            retryPolicy.Execute(() =>
            {
                Console.WriteLine($"Validating VIN ({vin}) against NHTSA database...");
                response = (HttpWebResponse)httpWebRequest.GetResponse();
            });
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                return JObject.Parse(streamReader.ReadToEnd());
            }
        }
        public static bool ValidateVin(string vin)
        {
            return ValidateVinWithNHTSA(ref vin, out _, false);
        }
        public static bool ValidateVinWithNHTSA(ref string vin, out Dictionary<string, string> properties, bool fixOcr = false)
        {
            if (fixOcr)
            {
                vin = vin.Replace("I", "1").Replace("O", "0").Replace("Q", "9");
            }
            vin = vinRegex.Replace(vin, String.Empty);
            if (vin.Length != 17) {
                properties = null;
                return false;
            }
            JObject response = GetVinInfoFromNHTSA(vin);
            JArray values = response["Results"] as JArray;
            properties = values.Select(
                jToken => jToken as JObject
            ).Where(
                jObject => jObject.ContainsKey("Variable")
            ).Where(
                jObject => VinQueryFilter.Contains(jObject["Variable"].ToString())
            ).ToDictionary(
                jObject => jObject["Variable"].ToString(),
                jObject => jObject["Value"].ToString()
            );
            return properties["Error Code"].ToString() == "0";
        }
    }

    [DisplayName("Validate And Clean VIN")]
    public class ValidateAndCleanVIN : CodeActivity
    {

        [Category("Input")]
        [DisplayName("VIN Number")]
        [RequiredArgument]
        public InOutArgument<string> VIN { get; set; }

        [Category("Input")]
        [DisplayName("Attempt To Fix OCR?")]
        [Description("Replace I's, O's, and Q's to attempt to fix incorrect OCR.")]
        [DefaultValue(true)]
        public InArgument<bool> FixOCR { get; set; }

        [Category("Output")]
        [DisplayName("VIN Properties")]
        [Description("List of properties associated with VIN, per NHTSA")]
        public OutArgument<Dictionary<string,string>> Properties { get; set; }

        [Category("Output")]
        [DisplayName("Valid VIN?")]
        [Description("Indicates if the VIN was validated.")]
        public OutArgument<bool> Valid { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            Dictionary<string, string> properties;
            string sanitizedVin = VIN.Get(context).ToUpper();
            bool fixOcr = FixOCR.Get(context);
            bool valid = ValidationExtensions.ValidateVinWithNHTSA( ref sanitizedVin, out properties, fixOcr);
            VIN.Set(context, sanitizedVin);
            Properties.Set(context, properties);
            Valid.Set(context, valid);
        }
    }
}