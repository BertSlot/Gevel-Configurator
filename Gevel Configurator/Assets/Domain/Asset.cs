using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Domain
{
    class Asset
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public AssetType Type { get; set; }
        public List<Color> Colors { get; set; }
        public Dimensions Dimensions { get; set; }
    }
}
