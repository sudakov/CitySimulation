using CitySimulation.Ver2.Control;

namespace CitySimulation.Ver2.Generation
{
    public class RunConfig
    {
        public int Seed;
        public int NumThreads;
        public int DurationDays;
        public int DeltaTime;
        public int? LogDeltaTime;
        public int? TraceDeltaTime;
        public bool TraceConsole;
        public bool PrintConsole;
        public string TilesDirectory;

        public ConfigParamsSimple Params;
        public int? PersonsCountDeltaTime;
    }
}