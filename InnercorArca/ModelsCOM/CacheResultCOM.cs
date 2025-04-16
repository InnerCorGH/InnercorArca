using System;

namespace InnercorArca.V1.ModelsCOM
{
    public class CacheResultCOM
    {
        public class CacheResult
        {
            public string Service { get; set; }
            public DateTime GeneratedTime { get; set; }
            public string Token { get; set; }
            public string Sign { get; set; }
            public DateTime ExpTime { get; set; }
        }
    }
}
