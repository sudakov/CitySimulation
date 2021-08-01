using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Tools
{
    public class EntityComparer : IComparer<Entity.Entity>
    {
        public static EntityComparer Instance = new EntityComparer();
        public int Compare(Entity.Entity x, Entity.Entity y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return x.Id.CompareTo(y.Id);
        }
    }
}
