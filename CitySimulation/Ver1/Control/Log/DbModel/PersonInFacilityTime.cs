using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Control.Log.DbModel
{
    public class PersonInFacilityTime
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int StartDay { get; set; }
        public int StartMin { get; set; }
        public int EndDay { get; set; }
        public int EndMin { get; set; }

        public string Person { get; set; }
        public string Facility { get; set; }
    }
}
