using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnrollmentProcessorApp.Models
{
    public class EnrollmentInformation
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public double Age { get; set; }
        public string PlanType { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Status { get; set; }
    }
}