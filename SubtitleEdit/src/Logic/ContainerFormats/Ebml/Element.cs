namespace Nikse.SubtitleEdit.Logic.ContainerFormats.Ebml
{
    internal class Element
    {
        private readonly ElementId id;
        private readonly long dataPosition;
        private readonly long dataSize;

        public Element(ElementId id, long dataPosition, long dataSize)
        {
            this.id = id;
            this.dataPosition = dataPosition;
            this.dataSize = dataSize;
        }

        public ElementId Id
        {
            get
            {
                return id;
            }
        }

        public long DataPosition
        {
            get
            {
                return dataPosition;
            }
        }

        public long DataSize
        {
            get
            {
                return dataSize;
            }
        }

        public long EndPosition
        {
            get
            {
                return dataPosition + dataSize;
            }
        }

        public override string ToString()
        {
            return string.Format(@"{0} ({1})", id, dataSize);
        }
    }
}