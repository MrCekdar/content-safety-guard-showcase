using System.Collections.Generic;

namespace ContentSafetyGuard.View__Interface_.Cards.DNS { 
    public class DnsProviderPresetState
    {
        public string Name { get; set; } = "";
        public List<string> Servers { get; set; } = new List<string>();

        // Gibt nur den Namen von meiner ComboBoxListe 
        public override string ToString()
        {
            return Name;
        }
    }
}
