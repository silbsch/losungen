using System;
using System.Xml.Linq;

namespace Losungen.Standard
{
    public class LosungItem
    {
        public LosungItem(XElement xElement)
        {
            Day=DateTime.Parse(xElement.Element("Datum")?.Value??"");
            Losungstext = xElement.Element("Losungstext")?.Value;
            Losungsvers = xElement.Element("Losungsvers")?.Value;
            Lehrtext = xElement.Element("Lehrtext")?.Value;
            Lehrtextvers = xElement.Element("Lehrtextvers")?.Value;

            Sonntag = xElement.Element("Sonntag")?.Value;
        }

        public DateTime Day { get; }
    
        public string Losungstext { get; }
        public string Losungsvers { get; }

        public string Lehrtext { get; }
        public string Lehrtextvers { get; }

        public string Sonntag { get; }

        /*
    <Losungstext>Nicht hat euch der HERR angenommen und euch erwählt, weil ihr größer wäret als alle Völker – denn du bist das kleinste unter allen Völkern –, sondern weil er euch geliebt hat.</Losungstext>
    <Losungsvers>5.Mose 7,7-8</Losungsvers>
    <Lehrtext>Was gering ist vor der Welt und was verachtet ist, das hat Gott erwählt, was nichts ist, damit er zunichtemache, was etwas ist, auf dass sich kein Mensch vor Gott rühme.</Lehrtext>
    <Lehrtextvers>1.Korinther 1,28-29</Lehrtextvers>
    */
    }
}
