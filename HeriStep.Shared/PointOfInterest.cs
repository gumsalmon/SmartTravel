using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeriStep.Shared
{
    public class PointOfInterest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        public string ImageUrl { get; set; }
        public string AudioUrl { get; set; }
        public int Priority { get; set; }
    }
}