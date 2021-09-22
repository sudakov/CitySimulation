using System;
using System.Collections.Generic;
using CitySimulation.Entities;
using CitySimulation.Health;
using CitySimulation.Tools;
using CitySimulation.Ver2.Entity.Behaviour;

namespace CitySimulation.Ver2.Entity
{
    public class FacilityConfigurable : Facility
    {



        public FacilityConfigurable(string name) : base(name)
        {

        }

        /// <summary>
        /// Помимо обычного перемещения в локацию есть это, которое позволяет добавить персонажа единожды, чтобы расчёт заражения не вызывался повторно
        /// </summary>
        /// <param name="person"></param>
        // public void AddPersonInf(Person person)
        // {
        //    
        // }
        //
        // public void RemovePersonInf(Person person)
        // {
        //     
        // }

        //Процессы изменения состояния здоровья происходят на стадии Process, таким образом не пересекаются с этапом заражения
        public override void PostProcess()
        {
            base.PostProcess();
        }

        public override string ToLogString()
        {
            return $"{Id} ({Type})";
        }
    }
}
