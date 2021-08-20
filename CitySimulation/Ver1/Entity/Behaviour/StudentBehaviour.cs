using System;
using System.Collections.Generic;
using System.Text;
using CitySimulation.Behaviour.Action;
using CitySimulation.Ver1.Entity;

namespace CitySimulation.Behaviour
{
    public class StudentBehaviour : RegularAttendBehaviour, IStudent
    {
        public School StudyPlace => (School)attendPlace;
        public void SetStudyPlace(School studyPlace)
        {
            attendPlace = studyPlace;
            attendTime = studyPlace.StudyTime;
        }
    }
}
