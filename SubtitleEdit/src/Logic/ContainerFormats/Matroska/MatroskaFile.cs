﻿namespace Nikse.SubtitleEdit.Logic.ContainerFormats.Matroska
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Core;
    using Ebml;

    internal class MatroskaFile : IDisposable
    {
        public delegate void LoadMatroskaCallback(long position, long total);

        private readonly string path;
        private readonly FileStream stream;
        private readonly bool valid;
        private int pixelWidth, pixelHeight;
        private double frameRate;
        private string videoCodecId;

        private int subtitleRipTrackNumber;
        private readonly List<MatroskaSubtitle> subtitleRip = new List<MatroskaSubtitle>();
        private List<MatroskaTrackInfo> tracks;

        private readonly Element segmentElement;
        private long timecodeScale = 1000000;
        private double duration;

        public MatroskaFile(string path)
        {
            this.path = path;
            stream = new FastFileStream(path);

            // read header
            var headerElement = ReadElement();
            if (headerElement != null && headerElement.Id == ElementId.Ebml)
            {
                // read segment
                stream.Seek(headerElement.DataSize, SeekOrigin.Current);
                segmentElement = ReadElement();
                if (segmentElement != null && segmentElement.Id == ElementId.Segment)
                {
                    valid = true; // matroska file must start with ebml header and segment
                }
            }
        }

        public bool IsValid
        {
            get
            {
                return valid;
            }
        }

        public string Path
        {
            get
            {
                return path;
            }
        }

        public List<MatroskaTrackInfo> GetTracks(bool subtitleOnly = false)
        {
            ReadSegmentInfoAndTracks();

            if (tracks == null)
            {
                return new List<MatroskaTrackInfo>();
            }

            return subtitleOnly
                ? tracks.Where(t => t.IsSubtitle).ToList()
                : tracks;
        }

        /// <summary>
        /// Get first time of track
        /// </summary>
        /// <param name="trackNumber">Track number</param>
        /// <returns>Start time in milliseconds</returns>
        public long GetTrackStartTime(int trackNumber)
        {
            // go to segment
            stream.Seek(segmentElement.DataPosition, SeekOrigin.Begin);

            Element element;
            while (stream.Position < stream.Length && (element = ReadElement()) != null)
            {
                switch (element.Id)
                {
                    case ElementId.Info:
                        ReadInfoElement(element);
                        break;
                    case ElementId.Tracks:
                        ReadTracksElement(element);
                        break;
                    case ElementId.Cluster:
                        return FindTrackStartInCluster(element, trackNumber);
                }

                stream.Seek(element.EndPosition, SeekOrigin.Begin);
            }

            return 0;
        }

        private long FindTrackStartInCluster(Element cluster, int targetTrackNumber)
        {
            long clusterTimeCode = 0L;
            long trackStartTime = -1L;
            bool done = false;

            Element element;
            while (stream.Position < cluster.EndPosition && (element = ReadElement()) != null && !done)
            {
                switch (element.Id)
                {
                    case ElementId.None:
                        done = true;
                        break;
                    case ElementId.Timecode:
                        // Absolute timestamp of the cluster (based on TimecodeScale)
                        clusterTimeCode = (long)ReadUInt((int)element.DataSize);
                        break;
                    case ElementId.BlockGroup:
                        ReadBlockGroupElement(element, clusterTimeCode);
                        break;
                    case ElementId.SimpleBlock:
                        var trackNumber = (int)ReadVariableLengthUInt();
                        if (trackNumber == targetTrackNumber)
                        {
                            // Timecode (relative to Cluster timecode, signed int16)
                            trackStartTime = ReadInt16();
                            done = true;
                        }

                        break;
                }

                stream.Seek(element.EndPosition, SeekOrigin.Begin);
            }

            return (clusterTimeCode + trackStartTime) * timecodeScale / 1000000;
        }

        private void ReadVideoElement(Element videoElement)
        {
            Element element;
            while (stream.Position < videoElement.EndPosition && (element = ReadElement()) != null)
            {
                switch (element.Id)
                {
                    case ElementId.PixelWidth:
                        pixelWidth = (int)ReadUInt((int)element.DataSize);
                        break;
                    case ElementId.PixelHeight:
                        pixelHeight = (int)ReadUInt((int)element.DataSize);
                        break;
                    default:
                        stream.Seek(element.DataSize, SeekOrigin.Current);
                        break;
                }
            }
        }

        private void ReadTrackEntryElement(Element trackEntryElement)
        {
            long defaultDuration = 0;
            bool isVideo = false;
            bool isAudio = false;
            bool isSubtitle = false;
            var trackNumber = 0;
            string name = string.Empty;
            string language = string.Empty;
            string codecId = string.Empty;
            string codecPrivate = string.Empty;
            //var biCompression = string.Empty;
            int contentCompressionAlgorithm = -1;
            int contentEncodingType = -1;

            Element element;
            while (stream.Position < trackEntryElement.EndPosition && (element = ReadElement()) != null)
            {
                switch (element.Id)
                {
                    case ElementId.DefaultDuration:
                        defaultDuration = (int)ReadUInt((int)element.DataSize);
                        break;
                    case ElementId.Video:
                        ReadVideoElement(element);
                        isVideo = true;
                        break;
                    case ElementId.Audio:
                        isAudio = true;
                        break;
                    case ElementId.TrackNumber:
                        trackNumber = (int)ReadUInt((int)element.DataSize);
                        break;
                    case ElementId.Name:
                        name = ReadString((int)element.DataSize, Encoding.UTF8);
                        break;
                    case ElementId.Language:
                        language = ReadString((int)element.DataSize, Encoding.ASCII);
                        break;
                    case ElementId.CodecId:
                        codecId = ReadString((int)element.DataSize, Encoding.ASCII);
                        break;
                    case ElementId.TrackType:
                        switch (stream.ReadByte())
                        {
                            case 1:
                                isVideo = true;
                                break;
                            case 2:
                                isAudio = true;
                                break;
                            case 17:
                                isSubtitle = true;
                                break;
                        }

                        break;
                    case ElementId.CodecPrivate:
                        codecPrivate = ReadString((int)element.DataSize, Encoding.UTF8);
                        //if (codecPrivate.Length > 20)
                        //    biCompression = codecPrivate.Substring(16, 4);
                        break;
                    case ElementId.ContentEncodings:
                        contentCompressionAlgorithm = 0; // default value
                        contentEncodingType = 0; // default value

                        var contentEncodingElement = ReadElement();
                        if (contentEncodingElement != null && contentEncodingElement.Id == ElementId.ContentEncoding)
                        {
                            ReadContentEncodingElement(element, ref contentCompressionAlgorithm, ref contentEncodingType);
                        }

                        break;
                }

                stream.Seek(element.EndPosition, SeekOrigin.Begin);
            }

            tracks.Add(new MatroskaTrackInfo
            {
                TrackNumber = trackNumber,
                IsVideo = isVideo,
                IsAudio = isAudio,
                IsSubtitle = isSubtitle,
                Language = language,
                CodecId = codecId,
                CodecPrivate = codecPrivate,
                Name = name,
                ContentEncodingType = contentEncodingType,
                ContentCompressionAlgorithm = contentCompressionAlgorithm
            });

            if (!isVideo)
            {
                return;
            }

            if (defaultDuration > 0)
            {
                frameRate = 1.0 / (defaultDuration / 1000000000.0);
            }

            videoCodecId = codecId;
        }

        private void ReadContentEncodingElement(Element contentEncodingElement, ref int contentCompressionAlgorithm, ref int contentEncodingType)
        {
            Element element;
            while (stream.Position < contentEncodingElement.EndPosition && (element = ReadElement()) != null)
            {
                switch (element.Id)
                {
                    case ElementId.ContentEncodingOrder:
                        var contentEncodingOrder = ReadUInt((int)element.DataSize);
                        System.Diagnostics.Debug.WriteLine("ContentEncodingOrder: " + contentEncodingOrder);
                        break;
                    case ElementId.ContentEncodingScope:
                        var contentEncodingScope = ReadUInt((int)element.DataSize);
                        System.Diagnostics.Debug.WriteLine("ContentEncodingScope: " + contentEncodingScope);
                        break;
                    case ElementId.ContentEncodingType:
                        contentEncodingType = (int)ReadUInt((int)element.DataSize);
                        break;
                    case ElementId.ContentCompression:
                        Element compElement;
                        while (stream.Position < element.EndPosition && (compElement = ReadElement()) != null)
                        {
                            switch (compElement.Id)
                            {
                                case ElementId.ContentCompAlgo:
                                    contentCompressionAlgorithm = (int)ReadUInt((int)compElement.DataSize);
                                    break;
                                case ElementId.ContentCompSettings:
                                    var contentCompSettings = ReadUInt((int)compElement.DataSize);
                                    System.Diagnostics.Debug.WriteLine("ContentCompSettings: " + contentCompSettings);
                                    break;
                                default:
                                    stream.Seek(element.DataSize, SeekOrigin.Current);
                                    break;
                            }
                        }

                        break;
                    default:
                        stream.Seek(element.DataSize, SeekOrigin.Current);
                        break;
                }
            }
        }

        private void ReadInfoElement(Element infoElement)
        {
            Element element;
            while (stream.Position < infoElement.EndPosition && (element = ReadElement()) != null)
            {
                switch (element.Id)
                {
                    case ElementId.TimecodeScale:
                        // Timestamp scale in nanoseconds (1.000.000 means all timestamps in the segment are expressed in milliseconds)
                        timecodeScale = (int)ReadUInt((int)element.DataSize);
                        break;
                    case ElementId.Duration:
                        // Duration of the segment (based on TimecodeScale)
                        duration = element.DataSize == 4 ? ReadFloat32() : ReadFloat64();
                        duration /= timecodeScale * 1000000.0;
                        break;
                    default:
                        stream.Seek(element.DataSize, SeekOrigin.Current);
                        break;
                }
            }
        }

        private void ReadTracksElement(Element tracksElement)
        {
            tracks = new List<MatroskaTrackInfo>();

            Element element;
            while (stream.Position < tracksElement.EndPosition && (element = ReadElement()) != null)
            {
                if (element.Id == ElementId.TrackEntry)
                {
                    ReadTrackEntryElement(element);
                }
                else
                {
                    stream.Seek(element.DataSize, SeekOrigin.Current);
                }
            }
        }

        /// <param name="duration">Duration of the segment in milliseconds.</param>
        public void GetInfo(out double frameRate, out int pixelWidth, out int pixelHeight, out double duration, out string videoCodec)
        {
            ReadSegmentInfoAndTracks();

            pixelWidth = this.pixelWidth;
            pixelHeight = this.pixelHeight;
            frameRate = this.frameRate;
            duration = this.duration;
            videoCodec = videoCodecId;
        }

        private void ReadCluster(Element clusterElement)
        {
            long clusterTimeCode = 0;

            Element element;
            while (stream.Position < clusterElement.EndPosition && (element = ReadElement()) != null)
            {
                switch (element.Id)
                {
                    case ElementId.Timecode:
                        clusterTimeCode = (long)ReadUInt((int)element.DataSize);
                        break;
                    case ElementId.BlockGroup:
                        ReadBlockGroupElement(element, clusterTimeCode);
                        break;
                    case ElementId.SimpleBlock:
                        var subtitle = ReadSubtitleBlock(element, clusterTimeCode);
                        if (subtitle != null)
                        {
                            subtitleRip.Add(subtitle);
                        }
                        break;
                    default:
                        stream.Seek(element.DataSize, SeekOrigin.Current);
                        break;
                }
            }
        }

        private void ReadBlockGroupElement(Element clusterElement, long clusterTimeCode)
        {
            MatroskaSubtitle subtitle = null;

            Element element;
            while (stream.Position < clusterElement.EndPosition && (element = ReadElement()) != null)
            {
                switch (element.Id)
                {
                    case ElementId.Block:
                        subtitle = ReadSubtitleBlock(element, clusterTimeCode);
                        if (subtitle == null)
                        {
                            return;
                        }

                        subtitleRip.Add(subtitle);
                        break;
                    case ElementId.BlockDuration:
                        var duration = (long)ReadUInt((int)element.DataSize);
                        if (subtitle != null)
                        {
                            subtitle.Duration = duration;
                        }
                        break;
                    default:
                        stream.Seek(element.DataSize, SeekOrigin.Current);
                        break;
                }
            }
        }

        private MatroskaSubtitle ReadSubtitleBlock(Element blockElement, long clusterTimeCode)
        {
            var trackNumber = (int)ReadVariableLengthUInt();
            if (trackNumber != subtitleRipTrackNumber)
            {
                stream.Seek(blockElement.EndPosition, SeekOrigin.Begin);
                return null;
            }

            var timeCode = ReadInt16();

            // lacing
            var flags = (byte)stream.ReadByte();
            int frames;
            switch (flags & 6)
            {
                case 0: // 00000000 = No lacing
                    System.Diagnostics.Debug.Print("No lacing");
                    break;
                case 2: // 00000010 = Xiph lacing
                    frames = stream.ReadByte() + 1;
                    System.Diagnostics.Debug.Print("Xiph lacing ({0} frames)", frames);
                    break;
                case 4: // 00000100 = Fixed-size lacing
                    frames = stream.ReadByte() + 1;
                    for (var i = 0; i < frames; i++)
                    {
                        stream.ReadByte(); // frames
                    }

                    System.Diagnostics.Debug.Print("Fixed-size lacing ({0} frames)", frames);
                    break;
                case 6: // 00000110 = EMBL lacing
                    frames = stream.ReadByte() + 1;
                    System.Diagnostics.Debug.Print("EBML lacing ({0} frames)", frames);
                    break;
            }

            // save subtitle data
            var dataLength = (int)(blockElement.EndPosition - stream.Position);
            var data = new byte[dataLength];
            stream.Read(data, 0, dataLength);

            return new MatroskaSubtitle(data, clusterTimeCode + timeCode);
        }

        public List<MatroskaSubtitle> GetSubtitle(int trackNumber, LoadMatroskaCallback progressCallback)
        {
            subtitleRipTrackNumber = trackNumber;
            ReadSegmentCluster(progressCallback);
            return subtitleRip;
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
            }
        }

        private void ReadSegmentInfoAndTracks()
        {
            // go to segment
            stream.Seek(segmentElement.DataPosition, SeekOrigin.Begin);

            Element element;
            while (stream.Position < segmentElement.EndPosition && (element = ReadElement()) != null)
            {
                switch (element.Id)
                {
                    case ElementId.Info:
                        ReadInfoElement(element);
                        break;
                    case ElementId.Tracks:
                        ReadTracksElement(element);
                        return;
                    default:
                        stream.Seek(element.DataSize, SeekOrigin.Current);
                        break;
                }
            }
        }

        private void ReadSegmentCluster(LoadMatroskaCallback progressCallback)
        {
            // go to segment
            stream.Seek(segmentElement.DataPosition, SeekOrigin.Begin);

            Element element;
            while (stream.Position < segmentElement.EndPosition && (element = ReadElement()) != null)
            {
                if (element.Id == ElementId.Cluster)
                {
                    ReadCluster(element);
                }
                else
                {
                    stream.Seek(element.DataSize, SeekOrigin.Current);
                }

                if (progressCallback != null)
                {
                    progressCallback.Invoke(element.EndPosition, stream.Length);
                }
            }
        }

        private Element ReadElement()
        {
            var id = (ElementId)ReadVariableLengthUInt(false);
            if (id <= ElementId.None)
            {
                return null;
            }

            var size = (long)ReadVariableLengthUInt();
            return new Element(id, stream.Position, size);
        }

        private ulong ReadVariableLengthUInt(bool unsetFirstBit = true)
        {
            // Begin loop with byte set to newly read byte
            var first = stream.ReadByte();
            var length = 0;

            // Begin by counting the bits unset before the highest set bit
            var mask = 0x80;
            for (var i = 0; i < 8; i++)
            {
                // Start at left, shift to right
                if ((first & mask) == mask)
                {
                    length = i + 1;
                    break;
                }

                mask >>= 1;
            }

            if (length == 0)
            {
                return 0;
            }

            // Read remaining big endian bytes and convert to 64-bit unsigned integer.
            var result = (ulong)(unsetFirstBit ? first & (0xFF >> length) : first);
            result <<= --length * 8;
            for (var i = 1; i <= length; i++)
            {
                result |= (ulong)stream.ReadByte() << (length - i) * 8;
            }

            return result;
        }

        /// <summary>
        /// Reads a fixed length unsigned integer from the current stream and advances the current
        /// position of the stream by the integer length in bytes.
        /// </summary>
        /// <param name="length">The length in bytes of the integer.</param>
        /// <returns>A 64-bit unsigned integer.</returns>
        private ulong ReadUInt(int length)
        {
            var data = new byte[length];
            stream.Read(data, 0, length);

            // Convert the big endian byte array to a 64-bit unsigned integer.
            var result = 0UL;
            var shift = 0;
            for (var i = length - 1; i >= 0; i--)
            {
                result |= (ulong)data[i] << shift;
                shift += 8;
            }

            return result;
        }

        /// <summary>
        /// Reads a 2-byte signed integer from the current stream and advances the current position
        /// of the stream by two bytes.
        /// </summary>
        /// <returns>A 2-byte signed integer read from the current stream.</returns>
        private short ReadInt16()
        {
            var data = new byte[2];
            stream.Read(data, 0, 2);
            return (short)(data[0] << 8 | data[1]);
        }

        /// <summary>
        /// Reads a 4-byte floating point value from the current stream and advances the current
        /// position of the stream by four bytes.
        /// </summary>
        /// <returns>A 4-byte floating point value read from the current stream.</returns>
        private unsafe float ReadFloat32()
        {
            var data = new byte[4];
            stream.Read(data, 0, 4);

            var result = data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
            return *(float*)&result;
        }

        /// <summary>
        /// Reads a 8-byte floating point value from the current stream and advances the current
        /// position of the stream by eight bytes.
        /// </summary>
        /// <returns>A 8-byte floating point value read from the current stream.</returns>
        private unsafe double ReadFloat64()
        {
            var data = new byte[8];
            stream.Read(data, 0, 8);

            var lo = data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
            var hi = data[4] << 24 | data[5] << 16 | data[6] << 8 | data[7];
            var result = (uint)hi | (long)lo << 32;
            return *(double*)&result;
        }

        /// <summary>
        /// Reads a fixed length string from the current stream using the specified encoding.
        /// </summary>
        /// <param name="length">The length in bytes of the string.</param>
        /// <param name="encoding">The encoding of the string.</param>
        /// <returns>The string being read.</returns>
        private string ReadString(int length, Encoding encoding)
        {
            var buffer = new byte[length];
            stream.Read(buffer, 0, length);
            return encoding.GetString(buffer);
        }
    }
}