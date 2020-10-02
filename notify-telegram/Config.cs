using System.Collections.Generic;

namespace notify_telegram
{
    class Config
    {
        public string ApiToken { get; set; } = "";
        public List<long> ChatMembers { get; set; } = new List<long>();
    }
}
