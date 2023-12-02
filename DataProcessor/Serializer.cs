namespace Medicines.DataProcessor
{
    using Medicines.Data;
    using Medicines.Data.Models.Enums;
    using Medicines.ExportDto;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using System.Linq;

    public class Serializer
    {
        public static string ExportPatientsWithTheirMedicines(MedicinesContext context, string date)
        {
            DateTime inputDate = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var patients = context.Patients
            .Where(x => x.PatientsMedicines.Any(x => x.Medicine.ProductionDate > inputDate))
            .Include(x => x.PatientsMedicines).ThenInclude(x => x.Medicine)
            .AsEnumerable()
            .Select(c => new ExportPatientDto()
            {
                Gender = c.Gender.ToString().ToLower(),
                Name = c.FullName,
                AgeGroup = c.AgeGroup.ToString(),
                Medicines = c.PatientsMedicines.Where(x => x.Medicine.ProductionDate > inputDate).OrderByDescending(x => x.Medicine.ExpiryDate)
            .ThenBy(x => x.Medicine.Price)
            .Select(b => new ExportMedicineDto()
            {
                Category = b.Medicine.Category.ToString().ToLower(),
                Name = b.Medicine.Name,
                Price = b.Medicine.Price.ToString("f2"),
                Producer = b.Medicine.Producer.ToString(),
                BestBefore = b.Medicine.ExpiryDate.ToString("yyyy-MM-dd"),
            }).ToList()
            }).OrderByDescending(x => x.Medicines.Count).ThenBy(x => x.Name).ToList();
            return Serialize<List<ExportPatientDto>>(patients, "Patients");
        }

        public static string ExportMedicinesFromDesiredCategoryInNonStopPharmacies(MedicinesContext context, int medicineCategory)
        {
            Category category = (Category)medicineCategory;
            string result = category.ToString();
            var medicines = context.Medicines
            .Where(x => x.Category == category && x.Pharmacy.IsNonStop == true)
            .OrderBy(x => x.Price)
            .ThenBy(x => x.Name)
            .Select(c => new
            {
                Name = c.Name,
                Price = c.Price.ToString("0.00"),
                Pharmacy = new
                {
                    Name = c.Pharmacy.Name,
                    PhoneNumber = c.Pharmacy.PhoneNumber
                }
            })
            .ToList();
            return JsonConvert.SerializeObject(medicines, Newtonsoft.Json.Formatting.Indented);
        }
        private static string Serialize<T>(T dataTransferObjects, string xmlRootAttributeName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), new XmlRootAttribute(xmlRootAttributeName));

            StringBuilder sb = new StringBuilder();
            using var write = new StringWriter(sb);

            XmlSerializerNamespaces xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add(string.Empty, string.Empty);

            serializer.Serialize(write, dataTransferObjects, xmlNamespaces);

            return sb.ToString();
        }
    }
}
