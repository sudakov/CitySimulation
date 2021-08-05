using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CitySimulation.Entity;

namespace CitySimulation.Control
{
    public class AsyncModuleSimple: AsyncModule
    {
        private List<Person> persons;
        private List<Facility> facilities;
        private Context context;
        public AsyncModuleSimple(List<Person> persons, List<Facility> facilities, Controller controller)
        {
            this.persons = persons;
            this.facilities = facilities;

            //Создаём отдельный контекст выполнения, чтобы результаты работы не зависили от порядка выполнения потоков 

            context = new Context()
            {
                Random = new Random(controller.Context.Random.Next()),
                CurrentTime = new CityTime(controller.Context.CurrentTime),
                Logger = controller.Context.Logger,
                Routes = controller.Context.Routes
            };

            persons.ForEach(x=>x.Context = context);
            facilities.ForEach(x=>x.Context = context);
        }

        public override void RunAsync(Barrier barrier)
        {
            while (Controller.IsRunning)
            {

                for (var i = 0; i < persons.Count; i++)
                {
                    persons[i].PreProcess();
                }

                barrier.SignalAndWait();

                for (var i = 0; i < persons.Count; i++)
                {
                    persons[i].Process();
                }

                barrier.SignalAndWait();

                for (var i = 0; i < facilities.Count; i++)
                {
                    facilities[i].PostProcess();
                }

                barrier.SignalAndWait();

                context.CurrentTime.AddMinutes(Controller.Instance.DeltaTime);
            }
        }
    }
}
