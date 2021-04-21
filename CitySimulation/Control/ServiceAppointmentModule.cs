using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitySimulation.Entity;
using CitySimulation.Tools;

namespace CitySimulation.Control
{
    public class ServiceAppointmentModule : Module
    {
        private int _lastDay = -1;
        public override void PreProcess()
        {
            if (_lastDay != Controller.CurrentTime.Day)
            {
                _lastDay = Controller.CurrentTime.Day;

                foreach (var person in Controller.City.Persons.Where(x => x.Behaviour != null))
                {
                    person.Behaviour.SortAppointments();
                }

                var services = Controller.City.Facilities.Values.OfType<Service>().Where(x => x.VisitorsPerMonth != 0).Shuffle(Controller.Random);

                var adm = services.OfType<AdministrativeService>().Select(x => new PairObj<AdministrativeService, int>(x, x.VisitorsPerMonth / 30)).ToList();
                var adults = Controller.City.Persons.Where(x => (x.Age >= 18 || x.Age < 65) && x.Behaviour != null).Shuffle(Controller.Random).ToList();

                if (adm.Any())
                {
                    foreach (var pair in adm)
                    {
                        List<Person> persons = adults.PopItems(pair.Item2);

                        foreach (Person person in persons)
                        {
                            person.Behaviour.AppointVisit(pair.Item1, new LogCityTime(_lastDay, pair.Item1.WorkTime.Random(Controller.Random)), pair.Item1.VisitDuration, true);
                        }
                    }
                }



                var stores = services.OfType<Store>().Select(x => new PairObj<Store, int>(x, x.VisitorsPerMonth / 30)).ToList();

                if (stores.Any())
                {
                    List<Family> families = Controller.City.Persons.Select(x => x.Family).Distinct().ToList();

                    int k = 0;
                    Service store = stores[k].Item1;
                    foreach (var family in families)
                    {
                        if (stores.Count == 0)
                        {
                            break;
                        }

                        var members = family.Members.Where(x => x.Age >= 14 && x.Age < 75 && x.Behaviour != null).Shuffle(Controller.Random).ToArray();
                        for (int i = 0; i < members.Length; i++)
                        {
                            var time = members[i].Behaviour.GetFreeTime(_lastDay, store.WorkTime);
                            if (time.HasValue)
                            {
                                if (members[i].Behaviour.AppointVisit(store, new LogCityTime(_lastDay, time.Value), store.VisitDuration, true))
                                {
                                    if (--stores[k].Item2 == 0)
                                    {
                                        stores.RemoveAt(k);
                                        k--;
                                    }

                                    k++;
                                    if (k >= stores.Count)
                                    {
                                        k = 0;
                                    }

                                    if (stores.Count != 0)
                                    {
                                        store = stores[k].Item1;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }



                var other = services.Where(x=>x is RecreationService || x is HouseholdService).Select(x => new PairObj<Service, int>(x, x.VisitorsPerMonth / 30)).ToList();

                if (other.Any())
                {
                    int k = 0;

                    for (int i = 0; i < 10; i++)
                    {
                        foreach (Person person in Controller.City.Persons.Where(x => x.Age >= 14 || x.Age < 75))
                        {
                            if (other.Count == 0)
                            {
                                break;
                            }

                            PairObj<Service, int> current = other[k];
                            Service service = current.Item1;


                            if (person.Behaviour?.AppointVisit(service, new LogCityTime(_lastDay, service.WorkTime.Random(Controller.Random)),
                                service.VisitDuration, service.ForceAppointment) == true)
                            {
                                if (--current.Item2 == 0)
                                {
                                    other.RemoveAt(k);
                                    k--;
                                }

                                if (++k >= other.Count)
                                {
                                    k = 0;
                                    if (stores.Count == 0)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var person in Controller.City.Persons.Where(x=>x.Behaviour != null))
                {
                    person.Behaviour.SortAppointments();
                }
            }
        }
    }
}
