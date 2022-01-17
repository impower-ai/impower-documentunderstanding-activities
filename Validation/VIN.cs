using System;
using System.Activities;
using System.ComponentModel;
using System.Text.RegularExpressions;

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
        //from https://gist.github.com/deeja/008c2c36764f11cbc02818e7d793738a
        internal static int Transliterate(char c)
        {
            return "0123456789.ABCDEFGH..JKLMN.P.R..STUVWXYZ".IndexOf(c) % 10;
        }

        //from https://gist.github.com/deeja/008c2c36764f11cbc02818e7d793738a
        internal static char GetCheckDigit(string vin)
        {
            string map = "0123456789X";
            string weights = "8765432X098765432";
            int sum = 0;
            for (int i = 0; i < 17; ++i)
            {
                sum += Transliterate(vin[i]) * map.IndexOf(weights[i]);
            }
            return map[sum % 11];
        }

        //from https://gist.github.com/deeja/008c2c36764f11cbc02818e7d793738a
        internal static bool Validate(string vin)
        {
            if (vin.Length != 17)
            {
                throw new InvalidVinException("VIN was not 17 characters long.");
            }
            return GetCheckDigit(vin) == vin[8];
        }
    }

    [DisplayName("Validate And Clean VIN")]
    public class ValidateAndCleanVIN : CodeActivity
    {
        private readonly Regex vinRegex = new Regex(@"[^A-HJ-NPR-Za-hj-npr-z\d]");

        [DisplayName("VIN Number")]
        [RequiredArgument]
        public InOutArgument<string> VIN { get; set; }

        [DisplayName("Attempt To Fix OCR?")]
        [Description("Replace I's, O's, and Q's to attempt to fix incorrect OCR.")]
        [DefaultValue(true)]
        public InArgument<bool> FixOCR { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            string sanitizedVin = VIN.Get(context).ToUpper();
            if (FixOCR.Get(context))
            {
                sanitizedVin = sanitizedVin.Replace("I", "1").Replace("O", "0").Replace("Q", "9");
            }
            sanitizedVin = vinRegex.Replace(sanitizedVin, String.Empty);
            if (ValidationExtensions.Validate(sanitizedVin))
            {
                VIN.Set(context, sanitizedVin);
            }
            else
            {
                throw new InvalidVinException("Check digit did not match.");
            }
        }
    }
}