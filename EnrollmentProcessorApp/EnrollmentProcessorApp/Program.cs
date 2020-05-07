using EnrollmentProcessorApp.Helpers;
using EnrollmentProcessorApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EnrollmentProcessorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Variables
                DataConverter dataConverter = new DataConverter();

                DataValidator dataValidator = new DataValidator(dataConverter);

                EnrollmentFileManager fileManager = new EnrollmentFileManager(dataValidator, dataConverter);

                string inbox = ConfigurationManager.AppSettings["Inbox"].ToString();

                var listOfEnrollments = fileManager.ProcessEnrollmentData(inbox);

                // If list of enrollment information is not null and more than 0, then proceed to printing the results
                if (listOfEnrollments != null && listOfEnrollments.Count > 0)
                {
                    // Header was not required, but could be a nice addition.
                    Console.WriteLine($"Status | First Name | Last Name | DOB | Plan Type | Effective Date | \n");

                    foreach (var enrollmentInfo in listOfEnrollments)
                    {
                        // Print enrollment records to the console.
                        EnrollmentFileManager.PrintEnrollmentRecordsToConsole(enrollmentInfo);
                    }
                }
                else
                {
                    // Print this if there are 0 records in the file.
                    Console.WriteLine("No data exist!");
                }

                EnrollmentFileManager.CloseApplication();
            }
            catch (Exception)
            {
                // We could pass in a exception's variable (ex) to print out what the exception is to the console.
                Console.WriteLine($"A record in the file failed validation. Processing has stopped.");
                Console.ReadKey();
            }
        }
    }
}