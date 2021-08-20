using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Tools
{
    public class EntityComparer : IComparer<Entities.EntityBase>
    {
        public static EntityComparer Instance = new EntityComparer();
        public int Compare(Entities.EntityBase x, Entities.EntityBase y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return x.Id.CompareTo(y.Id);
        }
    }
}
