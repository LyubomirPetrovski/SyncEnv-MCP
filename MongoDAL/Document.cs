namespace Synergy.Model
{
    public class Document : IDocument
    {
        public string id { get; set; }
    }

    public class DocumentRef : IDocument
    {
        public string id { get; set; }
        
        public DocumentRef()
        {
        }

        public DocumentRef(string id)
        {
            this.id = id;
        }

        public static implicit operator DocumentRef(Document document)
        {
            if (document == null) return null;
            return new DocumentRef
            {
                id = document.id,
            };
        }
    }
}
