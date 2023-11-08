
using Newtonsoft.Json;

namespace AudioGenerationService
{
    internal class EventMessage
    {

        private string subject;

        private string predicateVerb;

        private string objectJSON;

        private string where;

        private string when;

        private Dictionary<string, object> additional;

        [JsonProperty("subject")]
        public string Subject { get => subject; set => subject = value; }

        [JsonProperty("predicateVerb")]
        public string PredicateVerb { get => predicateVerb; set => predicateVerb = value; }

        [JsonProperty("objectJSON")]
        public string ObjectJSON { get => objectJSON; set => objectJSON = value; }

        [JsonProperty("where")]
        public string Where { get => where; set => where = value; }

        [JsonProperty("when")]
        public string When { get => when; set => when = value; }

        [JsonProperty("additional")]
        public Dictionary<string, object> Additional { get => additional; set => additional = value; }
    }
}
