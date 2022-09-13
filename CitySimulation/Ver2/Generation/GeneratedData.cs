using CitySimulation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitySimulation.Tools;

namespace CitySimulation.Ver2.Generation
{
    public class GeneratedData
    {
        public List<Facility> Facilities { get; set; }
        public List<Person> Persons { get; set; }
        public Dictionary<string, List<Station>> Routes { get; set; }
    }
}
