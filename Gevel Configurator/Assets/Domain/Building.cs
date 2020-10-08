using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Domain
{
    class Building
    {
        public long Id { get; set; }
        public BuildingType Type { get; set; }
        public List<ConnectedAsset> ConnectedAssets { get; set; }
    }
}
