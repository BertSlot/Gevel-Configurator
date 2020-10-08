using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Domain
{
    class Project
    {
        public long Id { get; set; }
        public string Name { get; set; }
        internal ProjectStatus Status { get; set; }
        internal Lot Lot { get; set; }
    }
}
