using System.Collections.Generic;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Behaviour
{
    public interface IPersonWithWork
    {
        Facility WorkPlace { get; }

        void SetWorkplace(Facility workplace, Range workTime);
    }
}
