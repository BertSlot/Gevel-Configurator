using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Domain
{
    class Lot
    {
        public long Id { get; set; }
        public string LotNumber { get; set; }
        public List<Building> Buildings { get; set; }
    }
}
