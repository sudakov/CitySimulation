using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;
using ClosedXML;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;

namespace CitySimulation.Generation
{
    public class ExcelPopulationGenerator
    {
        public string FileName;
        public string SheetName;
        public string AgentsCount;
        public string AgeDistributionMale;
        public string AgeDistributionFemale;
        public string SingleDistributionMale;
        public string CountOfFamiliesWith1Children;
        public string CountOfFamiliesWith2Children;
        public string CountOfFamiliesWith3Children;

        public (int,int) WifeAgeDifference = (-7, 2);

        public int ElderlyAge = 60;
        public int VeryElderlyAge = 80;

        public List<Person> Generate()
        {
            using var stream = File.Open(FileName, FileMode.Open, FileAccess.Read);
            using var workbook = new XLWorkbook(stream);

            int p_index = 0;

            List<Person> male = new List<Person>();
            List<Person> female = new List<Person>();
            List<Person> persons = new List<Person>();

            var worksheet = workbook.Worksheets.FirstOrDefault(x=>x.Name == SheetName) ?? workbook.Worksheets.FirstOrDefault(x=>x.Name.Contains(SheetName));

            {
                int age = 0;
                foreach (var cell in worksheet.Cells(AgeDistributionMale))
                {
                    int count = cell.GetValue<int>();

                    for (int i = 0; i < count; i++)
                    {
                        male.Add(new Person("p_" + p_index++)
                        {
                            Age = age,
                            Gender = Gender.Male
                        });
                    }
                    age++;
                }

                male = male.Shuffle(Controller.Random).ToList();
            }

            {
                int age = 0;
                foreach (var cell in worksheet.Cells(AgeDistributionFemale))
                {
                    int count = cell.GetValue<int>();

                    for (int i = 0; i < count; i++)
                    {
                        female.Add(new Person("p_" + p_index++)
                        {
                            Age = age,
                            Gender = Gender.Female
                        });
                    }
                    age++;
                }
                female = female.Shuffle(Controller.Random).ToList();
            }

            persons = male.Concat(female).ToList();

            List<Family> families = new List<Family>();

            {
                var maleAgeStacks = male.GroupBy(x => x.Age).ToDictionary(x=>x.Key,x=>x.ToList());

                Dictionary<int, int> singleMale = worksheet.Cells(SingleDistributionMale).Number(start: 18)
                    .ToDictionary(x => x.Item1, x => x.Item2.GetValue<int>());


                foreach (var (age, singles) in singleMale)
                {
                    foreach (Person notSingleMale in maleAgeStacks.GetValueOrDefault(age, new List<Person>(0)).Skip(singles))
                    {
                        var wife = female.FirstOrDefault(x =>
                            x.Age >= 18
                            && x.Family == null
                            && WifeAgeDifference.Item1 <= x.Age - notSingleMale.Age
                            && x.Age - notSingleMale.Age <= WifeAgeDifference.Item2);

                        if (wife == null)
                        {
                            throw new Exception("Not enough females for families");
                        }

                        families.Add(Family.Unite(notSingleMale, wife));
                    }
                }

                families = families.Shuffle(Controller.Random).ToList();
            }


            {
                int familiesWith1Children = worksheet.Cell(CountOfFamiliesWith1Children).GetValue<int>();
                int familiesWith2Children = worksheet.Cell(CountOfFamiliesWith2Children).GetValue<int>();
                int familiesWith3Children = worksheet.Cell(CountOfFamiliesWith3Children).GetValue<int>();

                var children = persons.Where(x => x.Family == null && x.Age < 18).OrderBy(x => x.Age).ToList();
                foreach (Person child in children)
                {
                    IEnumerable<Family> fam = families.Where(x=>x.Female.Age >= 19 && child.Age <= x.Female.Age - 18);

                    if (familiesWith1Children > 0)
                    {
                        fam = fam.Where(x => x.Children.Count == 0 && x.Male != null);
                        familiesWith1Children--;
                    }
                    else if(familiesWith2Children > 0)
                    {
                        fam = fam.Where(x => x.Children.Count == 1 && x.Male != null);
                        familiesWith2Children--;
                        familiesWith1Children++;
                    }
                    else if(familiesWith3Children > 0)
                    {
                        fam = fam.Where(x => x.Children.Count == 2 && x.Male != null);
                        familiesWith3Children--;
                        familiesWith2Children++;
                    }
                    else
                    {
                        Family f = Family.Solo(female.First(x => x.Family == null && x.Age >= 19 && child.Age <= x.Age - 18));
                        families.Add(f);
                        fam = new List<Family>{f};
                    }

                    Family family = fam.FirstOrDefault();

                    family.AddChild(child);

                }

                if (familiesWith1Children > 1 || familiesWith2Children > 1 || familiesWith3Children > 1)
                {
                    Debug.Write("Not enough children");
                }

            }

            families = families.Shuffle(Controller.Random).ToList();

            foreach (Family family in families.Take(families.Count / 4))
            {
                Person eld = persons.FirstOrDefault(x => x.Age > VeryElderlyAge && x.Family == null) ?? persons.FirstOrDefault(x=>x.Age > ElderlyAge && x.Family == null);
                if (eld != null)
                {
                    family.AddElderly(eld);
                }
                else
                {
                    break;
                }
            }

            foreach (Person eld in persons.Where(x => x.Age > VeryElderlyAge && x.Family == null))
            {
                families.GetRandom(Controller.Random).AddElderly(eld);
            }


            foreach (Person person in persons.Where(x=>x.Family == null))
            {
                families.Add(Family.Solo(person));
            }

            return persons;
        }
    }
}
