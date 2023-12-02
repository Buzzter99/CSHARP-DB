using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Medicines.ImportPharmacyDto
{
    [XmlType("Pharmacy")]
    public class ImportPharmaciesDto
    {
        [XmlAttribute("non-stop")]
        [Required]
        public string IsNonStop {  get; set; }
        [XmlElement("Name")]
        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string Name { get; set; }
        [XmlElement("PhoneNumber")]
        [RegularExpression("^\\([0-9]{3}\\)\\s{1}[0-9]{3}-[0-9]{4}$")]
        [MinLength(14)]
        [MaxLength(14)]
        public string PhoneNumber { get; set; }
        [XmlArray("Medicines")]
        public List<ImportMedicinesDto> Medicines { get; set;} = new List<ImportMedicinesDto>();
    }
}
