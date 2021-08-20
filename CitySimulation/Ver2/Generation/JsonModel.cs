using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CitySimulation.Generation.Model2
{
 
    public partial class JsonModel
    {
        [JsonProperty("seed")]
        public int Seed { get; set; }

        [JsonProperty("total_time")]
        public int TotalTime { get; set; }

        [JsonProperty("step")]
        public double Step { get; set; }

        [JsonProperty("print_step", NullValueHandling = NullValueHandling.Ignore)]
        public double? PrintStep { get; set; }
        [JsonProperty("trace_step", NullValueHandling = NullValueHandling.Ignore)]
        public double? TraceStep { get; set; }

        [JsonProperty("num_threads")]
        public int NumThreads { get; set; }

        [JsonProperty("location_types")]
        public Dictionary<string, LocationType> LocationTypes { get; set; }

        [JsonProperty("people_types")]
        public Dictionary<string, PeopleType> PeopleTypes { get; set; }

        [JsonProperty("link_loc_people_types")]
        public List<LinkLocPeopleType> LinkLocPeopleTypes { get; set; }
    }

    public partial class LinkLocPeopleType
    {
        [JsonProperty("people_type")]
        public string PeopleType { get; set; }

        [JsonProperty("location_type")]
        public string LocationType { get; set; }

        [JsonProperty("workdays_mean")]
        public double WorkdaysMean { get; set; }

        [JsonProperty("workdays_std")]
        public double WorkdaysStd { get; set; }

        [JsonProperty("holiday_mean")]
        public double HolidayMean { get; set; }

        [JsonProperty("holiday_std")]
        public double HolidayStd { get; set; }

        [JsonProperty("ispermanent")]
        public long Ispermanent { get; set; }

        [JsonProperty("start_mean")]
        public double StartMean { get; set; }

        [JsonProperty("start_std")]
        public double StartStd { get; set; }

        [JsonProperty("duration_mean")]
        public double DurationMean { get; set; }

        [JsonProperty("duration_std")]
        public double DurationStd { get; set; }

        [JsonProperty("income", NullValueHandling = NullValueHandling.Ignore )]
        public List<Income> Income { get; set; }

        public override string ToString()
        {
            return PeopleType + " - " + LocationType;
        }
    }

    public partial class Income
    {
        public const string RatePerFact = "per fact";
        public const string RatePerDay = "per day";
        public const string RatePerMinute = "per minute";
        public const string RatePerHour = "per hour";

        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("summ")]
        public int Summ { get; set; }

        [JsonProperty("rate")]
        public string Rate { get; set; }

    }

    public partial class LocationType
    {
        [JsonProperty("num")]
        public int Num { get; set; }

        [JsonProperty("people_mean")]
        public double PeopleMean { get; set; }

        [JsonProperty("people_std")]
        public double PeopleStd { get; set; }

        [JsonProperty("infection_probability")]
        public double InfectionProbability { get; set; }
    }

    public partial class PeopleType
    {
        [JsonProperty("fraction")]
        public double Fraction { get; set; }
    }
}
