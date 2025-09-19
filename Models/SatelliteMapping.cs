using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Get_TLE.Models
{
    public class SatelliteMapping
    {
        public int NoradId { get; set; }            // основной ID
        public string DisplayName { get; set; }      // имя

        public bool IsFake { get; set; }             // флаг подложного ID
        public int? FakeId { get; set; }             // новый ID, если Fake
        public string FakeName { get; set; }         // новое имя, если Fake
    }

}
