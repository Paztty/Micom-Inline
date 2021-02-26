using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Micom_Inline
{
    class Model
    {
        // value
        public string MasterData { get; set; } = "";
        public string Name { get; set; } = "";
        public string WritingArea { get; set; } = "";
        public string AssyCode { get; set; } = "";
        public string PBACode { get; set; } = "";
        public string PCBCode { get; set; } = "";

        public List<ROM> ROMs { get; set; } = new List<ROM>();

        public class ROM
        {
            public string AssyMicomCode { get; set; } = "";
            public string MicomName { get; set; } = "";
            public string Checksum { get; set; } = "";
            public string Version { get; set; } = "";
            public string DateApply { get; set; } = "";
        }

        public Model()
        {
            ROMs.Add(new ROM());
            ROMs.Add(new ROM());
        }
    }
    }
