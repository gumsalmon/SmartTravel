using System;
using System.Collections.Generic;

namespace HeriStep.Shared
{
    public class Tour
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<PointOfInterest> Points { get; set; } = new List<PointOfInterest>();
    }
}   