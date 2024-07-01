using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Task1.Models
{
    public class UploadFileModel
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Telephone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public DateTime DoB { get; set; }
        public int GrossSalaryFY2019_20 { get; set; }
        public int GrossSalaryFY2020_21 { get; set; }
        public int GrossSalaryFY2021_22 { get; set; }
        public int GrossSalaryFY2022_23 { get; set; }
        public int GrossSalaryFY2023_24 { get; set; }
    }
}