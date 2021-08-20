using System.Collections.Generic;
using System.Text;
using CitySimulation.Entities;
using CitySimulation.Tools;

namespace CitySimulation.Behaviour
{
    public interface IPersonWithWork
    {
        Facility WorkPlace { get; }

        void SetWorkplace(Facility workplace, Range workTime);
    }
}
