namespace Medicines.DataProcessor
{
    using Medicines.Data;
    using Medicines.Data.Models;
    using Medicines.Data.Models.Enums;
    using Medicines.ImportPharmacyDto;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Text;
    using System.Xml.Serialization;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid Data!";
        private const string SuccessfullyImportedPharmacy = "Successfully imported pharmacy - {0} with {1} medicines.";
        private const string SuccessfullyImportedPatient = "Successfully imported patient - {0} with {1} medicines.";

        public static string ImportPatients(MedicinesContext context, string jsonString)
        {
            var patients = JsonConvert.DeserializeObject<ImportPatientsDto[]>(jsonString);
            List<Patient> patientsList = new List<Patient>();
            StringBuilder sb = new StringBuilder();
            foreach (var patient in patients)
            {
                if (!IsValid(patient))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
                var importPatient = new Patient() 
                { 
                    FullName = patient.FullName,
                    AgeGroup = (AgeGroup)patient.AgeGroup,
                    Gender = (Gender)patient.Gender
                };
                List<PatientMedicine> patientMedicines = new List<PatientMedicine>();
                foreach (int medicineId in patient.Medicines)
                {
                    if (patientMedicines.Any(x => x.MedicineId == medicineId))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                    var patientMed = new PatientMedicine()
                    {
                        MedicineId = medicineId,
                        Patient = importPatient
                    };
                    patientMedicines.Add(patientMed);
                }
                importPatient.PatientsMedicines = patientMedicines;
                patientsList.Add(importPatient);
                sb.AppendLine(string.Format(SuccessfullyImportedPatient,importPatient.FullName,importPatient.PatientsMedicines.Count));
            }
            context.Patients.AddRange(patientsList);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }

        public static string ImportPharmacies(MedicinesContext context, string xmlString)
        {
            var entities = Deserialize<ImportPharmaciesDto[]>(xmlString, "Pharmacies");
            List<Pharmacy> pharmacies = new List<Pharmacy>();
            StringBuilder sb = new StringBuilder();
            foreach (var pharmacy in entities)
            {
                if (!IsValid(pharmacy))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
                if (pharmacy.IsNonStop.ToLower() != "true" && pharmacy.IsNonStop.ToLower() != "false")
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
                var importPharmacy = new Pharmacy()
                {
                    Name = pharmacy.Name,
                    PhoneNumber = pharmacy.PhoneNumber,
                    IsNonStop = bool.Parse(pharmacy.IsNonStop),
                };
                List<Medicine> medicines = new List<Medicine>();
                foreach (var item in pharmacy.Medicines)
                {
                    if (item.ProductionDate == string.Empty || item.ExpiryDate == string.Empty)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                    DateTime productionDate = DateTime.ParseExact(item.ProductionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime expiryDate = DateTime.ParseExact(item.ExpiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    bool canAddMedicine = medicines.FirstOrDefault(x => x.Name == item.Name && x.Producer == item.Producer) == null;
                    if (!IsValid(item) || productionDate >= expiryDate || !canAddMedicine)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                    var medicine = new Medicine()
                    {
                        Name = item.Name,
                        Price = item.Price,
                        ProductionDate = productionDate,
                        ExpiryDate = expiryDate,
                        Producer = item.Producer,
                        Category = (Category)item.Category
                    };
                    medicines.Add(medicine);
                }
                context.Medicines.AddRange(medicines);
                importPharmacy.Medicines = medicines;
                pharmacies.Add(importPharmacy);
                sb.AppendLine(string.Format(SuccessfullyImportedPharmacy,importPharmacy.Name,importPharmacy.Medicines.Count));
            }
            context.Pharmacies.AddRange(pharmacies);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
        private static T Deserialize<T>(string inputXml, string rootName)
        {
            XmlRootAttribute root = new XmlRootAttribute(rootName);
            XmlSerializer serializer = new XmlSerializer(typeof(T), root);

            using StringReader reader = new StringReader(inputXml);

            T dtos = (T)serializer.Deserialize(reader);
            return dtos;
        }
    }
}
