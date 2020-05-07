using EnrollmentProcessorApp.Helpers;
using EnrollmentProcessorApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnrollmentProcessorApp
{
    public class EnrollmentFileManager
    {
        private readonly DataValidator _dataValidator;
        private readonly DataConverter _dataConverter;
        const string DATE_FORMAT = "MMddyyyy";
        public EnrollmentFileManager(DataValidator dataValidator, DataConverter dataConverter)
        {
            _dataValidator = dataValidator;
            _dataConverter = dataConverter;
        }

        #region Process Enrollment Information
        public List<EnrollmentInformation> ProcessEnrollmentData(string sourceDirectory)
        {
            List<EnrollmentInformation> listOfEnrollments = new List<EnrollmentInformation>();

            if (Directory.Exists(sourceDirectory))
            {
                var directory = new DirectoryInfo(sourceDirectory);

                var files = directory.GetFiles().ToList();

                // Loop through files in the configured directory path.

                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        // Read all records in the file.
                        string[] lines = File.ReadAllLines(file.FullName);

                        if (lines != null)
                        {
                            foreach (string line in lines)
                            {
                                // Parse out enrollment record values.
                                EnrollmentInformation enrollmentInformation = ParseEnrollmentRecord(line);

                                // Will throw an exception if the record doesn't get parsed.
                                if (enrollmentInformation == null)
                                    throw new NullReferenceException();

                                // If record is parsed, we will add to our list of enrollments.
                                listOfEnrollments.Add(enrollmentInformation);
                            }
                        }
                    }
                }
            }

            return listOfEnrollments;
        }
        #endregion

        #region Parse out Enrollment Infromation
        EnrollmentInformation ParseEnrollmentRecord(string lineInCSV)
        {
            var values = lineInCSV.Split(',');

            // If the data doesn't meet requirements or standards, return null
            if (!_dataValidator.IsRecordValidToProcess(values))
                return null;

            // Associate the property with the value in the csv by location.
            EnrollmentInformation enrollmentInformation = new EnrollmentInformation();
            enrollmentInformation.FirstName = values[0];
            enrollmentInformation.LastName = values[1];
            enrollmentInformation.DateOfBirth = _dataConverter.ConvertStringToDate(values[2], DATE_FORMAT).Value;
            enrollmentInformation.PlanType = values[3];
            enrollmentInformation.EffectiveDate = _dataConverter.ConvertStringToDate(values[4], DATE_FORMAT).Value;

            // Associate Age property by calculating the age, set the record status via the information we have gathered
            enrollmentInformation.Age = _dataValidator.CalculateAge(enrollmentInformation.DateOfBirth);
            _dataValidator.SetRecordStatus(enrollmentInformation);
            return enrollmentInformation;
        }
        #endregion

        #region Print Enrollment Records
        public static void PrintEnrollmentRecordsToConsole(EnrollmentInformation enrollmentInfo)
        {
            // print out enrollment records.
            Console.WriteLine($"{enrollmentInfo.Status} | {enrollmentInfo.FirstName} | {enrollmentInfo.LastName} | " +
                              $"{enrollmentInfo.DateOfBirth.ToShortDateString()} | {enrollmentInfo.PlanType} | {enrollmentInfo.EffectiveDate} | \n");
        }
        #endregion

        #region Close Application
        public static void CloseApplication()
        {
            // Close application by pressing any key.
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
        #endregion
    }
}