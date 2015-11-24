using System;

namespace NewData
{
    public class LANGMasterScorms
    {
        public int Id { get; set; }
        public int ApiUrlCrc { get; set; }
        public long AttemptId { get; set; }
        public Guid? AttemptIdGuid { get; set; }
        public string AttemptIdStr { get; set; }
        public string Data1 { get; set; }
        public string Data2 { get; set; }
        public long Date { get; set; }
        public long Key1Int { get; set; }
        public string Key1Str { get; set; }
        public long Key2Int { get; set; }
        public string Key2Str { get; set; }
        //public byte[] RowVersion { get; set; }
        public string UserId { get; set; }
    }
}
