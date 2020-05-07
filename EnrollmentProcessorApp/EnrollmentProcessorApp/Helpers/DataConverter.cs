using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnrollmentProcessorApp.Helpers
{
    public class DataConverter
    {
        #region Convert String to Date
        /// <summary>
        /// Converts string to date as per given format
        /// </summary>
        /// <param name="date">Input date string</param>
        /// <param name="format">Format of the input date string</param>
        /// <returns></returns>
        public DateTime? ConvertStringToDate(string date, string format)
        {
            try
            {
                return DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
        #endregion

    }
}