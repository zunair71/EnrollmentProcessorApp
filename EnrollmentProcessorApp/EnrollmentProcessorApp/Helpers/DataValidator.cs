using EnrollmentProcessorApp.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnrollmentProcessorApp.Helpers
{
    public class DataValidator
    {
        private readonly DataConverter _dataConverter;
        public DataValidator(DataConverter dataConverter)
        {
            _dataConverter = dataConverter;
        }

        #region Set the status of the enrollment record
        public void SetRecordStatus(EnrollmentInformation info)
        {
            /// <summary>
            /// If record follows age requriement and effective date range, then declare status as accept, else declare as rejected
            /// </summary>
            /// <param name="dob"></param>
            /// <returns></returns>
            
            string acceptedStatus = ConfigurationManager.AppSettings["RecordAccepted"].ToString();
            string rejectedStatus = ConfigurationManager.AppSettings["RecordRejected"].ToString();
            string ageMinimum = ConfigurationManager.AppSettings["AgeRequirement"].ToString();
            double ageRequirement = double.Parse(ageMinimum);
            // Could use app.cong variable or constant variable
            //const string ACCEPTED_STATUS = "Accepted";
            //const string REJECTED_STATUS = "Rejected";
 
            if (info.Age >= ageRequirement && IsEffectiveDateInRange(info.EffectiveDate))
            {
                info.Status = acceptedStatus;
            }
            else
            {
                info.Status = rejectedStatus;
            }
        }
        #endregion

        #region Validate if record is valid to process
        public bool IsRecordValidToProcess(string[] values)
        {
            /// <summary>
            /// Checks to see if record is valid to process based on criteria
            /// </summary>
            /// <param name="dob"></param>
            /// <returns></returns>
            
            const int VALUES_IN_CSV = 5;
            const string DATE_FORMAT = "MMddyyyy";

            if (values.Length != VALUES_IN_CSV)
                return false;

            if (values.Any(s => string.IsNullOrEmpty(s)))
                return false;

            if (_dataConverter.ConvertStringToDate(values[2], DATE_FORMAT) == null)
                return false;

            if (!IsValidPlanType(values[3]))
                return false;

            if (_dataConverter.ConvertStringToDate(values[4], DATE_FORMAT) == null)
                return false;

            return true;
        }
        #endregion

        #region Calculate Age
        public double CalculateAge(DateTime birthday)
        {
            /// <summary>
            /// Approach of calculating the age of the individual.
            /// </summary>
            /// <param name="dob"></param>
            /// <returns></returns>
            
            const int DAYS_IN_A_YEAR = 365;

            if (birthday < DateTime.Today)
            {
                double years = (DateTime.Today - birthday).TotalDays / DAYS_IN_A_YEAR;

                return years;
            }

            return 0;
        }
        #endregion

        #region Determine if the effective date is in range
        /// <summary>
        /// The plan's Effective Date may not be more than 30 days in the future
        /// </summary>
        /// <param name="dob"></param>
        /// <returns></returns>
        public static bool IsEffectiveDateInRange(DateTime effectiveDate)
        {
            return effectiveDate <= DateTime.Today.AddDays(30);
        }
        #endregion

        #region Validate Plan Type
        /// <summary>
        /// Plan Type must contain the following: HSA, HRA, FSA
        /// </summary>
        /// <param name="planType"></param>
        /// <returns></returns>
        private static bool IsValidPlanType(string planType)
        {
            List<string> planTypeOptions = new List<string>() { "HSA", "HRA", "FSA" };

            if (!string.IsNullOrEmpty(planType) && planTypeOptions.Contains(planType))
                return true;

            return false;
        }
        #endregion

    }
}