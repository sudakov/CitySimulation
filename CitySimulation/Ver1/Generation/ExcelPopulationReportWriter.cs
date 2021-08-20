using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CitySimulation.Behaviour;
using CitySimulation.Entities;
using CitySimulation.Tools;
using ClosedXML.Excel;

namespace CitySimulation.Generation
{
    public class ExcelPopulationReportWriter
    {
        public string FileName;
        public string SheetName;
        public string AgeRange { get; set; }
        public string SingleMaleCount { get; set; }
        public string FamiliesByMaleAgeCount { get; set; }

        public string Families0ChildrenByMaleAgeCount { get; set; }
        public string Families1ChildrenByMaleAgeCount { get; set; }
        public string Families2ChildrenByMaleAgeCount { get; set; }
        public string Families3ChildrenByMaleAgeCount { get; set; }
        public string FamiliesWithElderByMaleAgeCount { get; set; }

        public string SingleFemaleCount { get; set; }
        public string FemaleWith1ChildrenByFemaleAgeCount { get; set; }
        public string FemaleWith2ChildrenByFemaleAgeCount { get; set; }
        public string FemaleWithElderByFemaleAgeCount { get; set; }


        public (string, Range)[] AgesCount { get; set; }

        public void WriteReport(List<Person> persons)
        {
            var families = persons.Select(x => x.Family).Distinct().ToList();

            using var stream = File.Open(FileName, FileMode.Open, FileAccess.ReadWrite);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault(x => x.Name == SheetName) ?? workbook.Worksheets.FirstOrDefault(x => x.Name.Contains(SheetName));


            List<Range> ranges = new List<Range>();
            foreach (var cell in worksheet.Cells(AgeRange))
            {
                var range = cell.GetString().Split('-').ConvertArray(x=> new Range(int.Parse(x[0].Trim()), int.Parse(x[1].Trim())));
                ranges.Add(range);
            }

            //Solo Male
            foreach (var (i, cell) in worksheet.Cells(SingleMaleCount).Number())
            {
                cell.SetValue(persons.Count(x => x.Gender == Gender.Male && (x.Family.Female == null && x.Family.Male == x) && ranges[i].InRange(x.Age, true, true)));
            }


            //Full Families
            foreach (var (i, cell) in worksheet.Cells(FamiliesByMaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Male != null && x.Female != null && ranges[i].InRange(x.Male.Age, true, true)));
            }

            foreach (var (i, cell) in worksheet.Cells(Families0ChildrenByMaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Children.Count == 0 && x.Male != null && x.Female != null && ranges[i].InRange(x.Male.Age, true, true)));
            }

            foreach (var (i, cell) in worksheet.Cells(Families1ChildrenByMaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Children.Count == 1 && x.Male != null && x.Female != null && ranges[i].InRange(x.Male.Age, true, true)));
            }

            foreach (var (i, cell) in worksheet.Cells(Families2ChildrenByMaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Children.Count == 2 && x.Male != null && x.Female != null && ranges[i].InRange(x.Male.Age, true, true)));
            }
            
            foreach (var (i, cell) in worksheet.Cells(Families3ChildrenByMaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Children.Count == 3 && x.Male != null && x.Female != null && ranges[i].InRange(x.Male.Age, true, true)));
            }

            foreach (var (i, cell) in worksheet.Cells(FamiliesWithElderByMaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Elderly.Count > 0 && x.Male != null && x.Female != null && ranges[i].InRange(x.Male.Age, true, true)));
            }


            //Single Female
            foreach (var (i, cell) in worksheet.Cells(SingleFemaleCount).Number())
            {
                cell.SetValue(persons.Count(x => x.Gender == Gender.Female && (x.Family.Male == null && x.Family.Female == x) && ranges[i].InRange(x.Age, true, true)));
            }

            foreach (var (i, cell) in worksheet.Cells(FemaleWith1ChildrenByFemaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Children.Count == 1 && x.Male == null && x.Female != null && ranges[i].InRange(x.Female.Age, true, true)));
            }

            foreach (var (i, cell) in worksheet.Cells(FemaleWith2ChildrenByFemaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Children.Count == 2 && x.Male == null && x.Female != null && ranges[i].InRange(x.Female.Age, true, true)));
            }

            foreach (var (i, cell) in worksheet.Cells(FemaleWithElderByFemaleAgeCount).Number())
            {
                cell.SetValue(families.Count(x => x.Elderly.Count > 0 && x.Male == null && x.Female != null && ranges[i].InRange(x.Female.Age, true, true)));
            }

            foreach (var (cellName, age) in AgesCount)
            {
                var cells = worksheet.Cells(cellName).ToArray();
                cells[0].SetValue(persons.Count(x => age.InRange(x.Age)));
                if (cells.Length > 2)
                {
                    cells[1].SetValue(persons.Count(x => age.InRange(x.Age) && x.Behaviour is IPersonWithWork));
                    cells[2].SetValue(persons.Count(x => age.InRange(x.Age) && !(x.Behaviour is IPersonWithWork)));
                }
            }

            workbook.Save();
        }
    }
}
