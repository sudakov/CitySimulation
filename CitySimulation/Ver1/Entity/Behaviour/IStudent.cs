using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Ver1.Entity;

namespace CitySimulation.Behaviour
{
    public interface IStudent
    {
        School StudyPlace { get; }

        void SetStudyPlace(School studyPlace);
    }
}
