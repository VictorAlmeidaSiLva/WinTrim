namespace PcToolkit
{
    public class ServiceCatalogEntry
    {
        public string DescriptionKey;
        public bool Critical;

        public string Description { get { return Loc.T(DescriptionKey); } }

        public ServiceCatalogEntry(string descriptionKey, bool critical) { DescriptionKey = descriptionKey; Critical = critical; }
    }
}
