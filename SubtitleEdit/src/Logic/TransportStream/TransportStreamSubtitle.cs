namespace Nikse.SubtitleEdit.Logic.TransportStream
{
    using System.Drawing;

    public class TransportStreamSubtitle
    {
        private ulong startMilliseconds;
        private ulong endMilliseconds;
        private readonly BluRaySup.BluRaySupParser.PcsData bdSup;

        public ulong StartMilliseconds
        {
            get
            {
                if (startMilliseconds < OffsetMilliseconds)
                {
                    return 0;
                }

                return startMilliseconds - OffsetMilliseconds;
            }

            set
            {
                startMilliseconds = value + OffsetMilliseconds;
            }
        }

        public ulong EndMilliseconds
        {
            get
            {
                if (endMilliseconds < OffsetMilliseconds)
                {
                    return 0;
                }

                return endMilliseconds - OffsetMilliseconds;
            }
            set
            {
                endMilliseconds = value + OffsetMilliseconds;
            }
        }

        public ulong OffsetMilliseconds { get; set; }
      
        public DvbSubPes Pes { get; set; }
      
        public int? ActiveImageIndex { get; set; }

        public bool IsBluRaySup
        {
            get
            {
                return bdSup != null;
            }
        }

        public bool IsDvbSub
        {
            get
            {
                return Pes != null;
            }
        }

        public TransportStreamSubtitle(BluRaySup.BluRaySupParser.PcsData bdSup, ulong startMilliseconds, ulong endMilliseconds)
        {
            this.bdSup = bdSup;
            StartMilliseconds = startMilliseconds;
            EndMilliseconds = endMilliseconds;
        }

        public TransportStreamSubtitle(BluRaySup.BluRaySupParser.PcsData bdSup, ulong startMilliseconds, ulong endMilliseconds, ulong offset)
        {
            this.bdSup = bdSup;
            StartMilliseconds = startMilliseconds;
            EndMilliseconds = endMilliseconds;
            OffsetMilliseconds = offset;
        }

        public TransportStreamSubtitle()
        {
        }

        /// <summary>
        /// Gets full image if 'ActiveImageIndex' not set, otherwise only gets image by index
        /// </summary>
        /// <returns></returns>
        public Bitmap GetActiveImage()
        {
            if (bdSup != null)
            {
                return bdSup.GetBitmap();
            }

            if (ActiveImageIndex.HasValue && ActiveImageIndex >= 0 && ActiveImageIndex < Pes.ObjectDataList.Count)
            {
                return (Bitmap)Pes.GetImage(Pes.ObjectDataList[ActiveImageIndex.Value]).Clone();
            }

            return Pes.GetImageFull();
        }

        public int NumberOfImages
        {
            get { return Pes != null ? Pes.ObjectDataList.Count : bdSup.BitmapObjects.Count; }
        }
    }
}