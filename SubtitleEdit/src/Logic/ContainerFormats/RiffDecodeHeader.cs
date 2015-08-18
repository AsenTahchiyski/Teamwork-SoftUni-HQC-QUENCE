// (c) Giora Tamir (giora@gtamir.com), 2005
namespace Nikse.SubtitleEdit.Logic.ContainerFormats
{
    using System;
    using System.Text;

    public class RiffDecodeHeader
    {

        #region private members

        private readonly RiffParser mParser;

        private double mFrameRate;
        private int mMaxBitRate;
        private int mTotalFrames;
        private int mNumStreams;
        private int mWidth;
        private int mHeight;

        private double mVidDataRate;
        private string mVidHandler;
        private double mAudDataRate;
        private string mAudHandler;

        private int mNumChannels;
        private int mSamplesPerSec;
        private int mBitsPerSec;
        private int mBitsPerSample;

        #endregion private members

        #region public members

        /// <summary>
        /// Access the internal parser object
        /// </summary>
        public RiffParser Parser
        {
            get
            {
                return mParser;
            }
        }

        public double FrameRate
        {
            get
            {
                double rate = 0.0;
                if (mFrameRate > 0.0)
                {
                    rate = 1000000.0 / mFrameRate;
                }

                return rate;
            }
        }

        public string MaxBitRate
        {
            get
            {
                return String.Format("{0:N} Kb/Sec", mMaxBitRate / 128);
            }
        }

        public int TotalFrames
        {
            get
            {
                return mTotalFrames;
            }
        }

        public double TotalMilliseconds
        {
            get
            {
                double totalTime = 0.0;
                if (mFrameRate > 0.0)
                {
                    totalTime = mTotalFrames * mFrameRate / TimeCode.BaseUnit;
                }

                return totalTime;
            }
        }

        public string NumStreams
        {
            get
            {
                return String.Format("Streams in file: {0:G}", mNumStreams);
            }
        }

        public string FrameSize
        {
            get
            {
                return String.Format("{0:G} x {1:G} pixels per frame", mWidth, mHeight);
            }
        }

        public int Width
        {
            get
            {
                return mWidth;
            }
        }

        public int Height
        {
            get
            {
                return mHeight;
            }
        }

        public string VideoDataRate
        {
            get
            {
                return String.Format("Video rate {0:N2} frames/Sec", mVidDataRate);
            }
        }

        public string AudioDataRate
        {
            get
            {
                return String.Format("Audio rate {0:N2} Kb/Sec", mAudDataRate / TimeCode.BaseUnit);
            }
        }

        public string VideoHandler
        {
            get
            {
                return mVidHandler;
            }
        }

        public string AudioHandler
        {
            get
            {
                return String.Format("Audio handler 4CC code: {0}", mAudHandler);
            }
        }

        public string Isft { get; private set; }

        public string NumChannels
        {
            get
            {
                return String.Format("Audio channels: {0}", mNumChannels);
            }
        }

        public string SamplesPerSec
        {
            get
            {
                return String.Format("Audio rate: {0:N0} Samples/Sec", mSamplesPerSec);
            }
        }

        public string BitsPerSec
        {
            get
            {
                return String.Format("Audio rate: {0:N0} Bytes/Sec", mBitsPerSec);
            }
        }

        public string BitsPerSample
        {
            get
            {
                return String.Format("Audio data: {0:N0} bits/Sample", mBitsPerSample);
            }
        }

        #endregion public members

        #region Constructor

        public RiffDecodeHeader(RiffParser rp)
        {
            mParser = rp;
        }

        private void Clear()
        {
            mFrameRate = 0;
            mHeight = 0;
            mMaxBitRate = 0;
            mNumStreams = 0;
            mTotalFrames = 0;
            mWidth = 0;

            Isft = String.Empty;

            mVidDataRate = 0;
            mAudDataRate = 0;
            mVidHandler = String.Empty;
            mAudHandler = String.Empty;

            mNumChannels = 0;
            mSamplesPerSec = 0;
            mBitsPerSample = 0;
            mBitsPerSec = 0;
        }

        #endregion Constructor

        #region Default element processing

        /// <summary>
        /// Default list element handler - skip the entire list
        /// </summary>
        /// <param name="rp"></param>
        /// <param name="fourCc"></param>
        /// <param name="length"></param>
        private void ProcessList(RiffParser rp, int fourCc, int length)
        {
            rp.SkipData(length);
        }

        #endregion Default element processing

        #region Decode AVI

        /// <summary>
        /// Handle chunk elements found in the AVI file. Ignores unknown chunks and
        /// </summary>
        /// <param name="rp"></param>
        /// <param name="fourCc"></param>
        /// <param name="unpaddedLength"></param>
        /// <param name="paddedLength"></param>
        private void ProcessAviChunk(RiffParser rp, int fourCc, int unpaddedLength, int paddedLength)
        {
            if (AviRiffData.CkidMainAviHeader == fourCc)
            {
                // Main AVI header
                DecodeAviHeader(rp, paddedLength);
            }
            else if (AviRiffData.CkidAviStreamHeader == fourCc)
            {
                // Stream header
                DecodeAviStream(rp, paddedLength);
            }
            else if (AviRiffData.CkidAviisft == fourCc)
            {
                Byte[] ba = new byte[paddedLength];
                rp.ReadData(ba, 0, paddedLength);
                StringBuilder sb = new StringBuilder(unpaddedLength);
                for (int i = 0; i < unpaddedLength; ++i)
                {
                    if (0 != ba[i]) sb.Append((char)ba[i]);
                }

                Isft = sb.ToString();
            }
            else
            {
                // Unknon chunk - skip
                rp.SkipData(paddedLength);
            }
        }

        /// <summary>
        /// Handle List elements found in the AVI file. Ignores unknown lists and recursively looks
        /// at the content of known lists.
        /// </summary>
        /// <param name="rp"></param>
        /// <param name="fourCc"></param>
        /// <param name="length"></param>
        private void ProcessAviList(RiffParser rp, int fourCc, int length)
        {
            RiffParser.ProcessChunkElement pac = ProcessAviChunk;
            RiffParser.ProcessListElement pal = ProcessAviList;

            // Is this the header?
            if ((AviRiffData.CkidAviHeaderList == fourCc)
                || (AviRiffData.CkidAviStreamList == fourCc)
                || (AviRiffData.CkidInfoList == fourCc))
            {
                while (length > 0)
                {
                    if (false == rp.ReadElement(ref length, pac, pal))
                    {
                        break;
                    }
                }
            }
            else
            {
                // Unknown lists - ignore
                rp.SkipData(length);
            }
        }

        public void ProcessMainAvi()
        {
            Clear();
            int length = Parser.DataSize;

            RiffParser.ProcessChunkElement pdc = ProcessAviChunk;
            RiffParser.ProcessListElement pal = ProcessAviList;

            while (length > 0)
            {
                if (false == Parser.ReadElement(ref length, pdc, pal))
                {
                    break;
                }
            }
        }

        private unsafe void DecodeAviHeader(RiffParser rp, int length)
        {
            //if (length < sizeof(AVIMAINHEADER))
            //{
            //  throw new RiffParserException(String.Format("Header size mismatch. Needed {0} but only have {1}",
            //      sizeof(AVIMAINHEADER), length));
            //}

            byte[] ba = new byte[length];

            if (rp.ReadData(ba, 0, length) != length)
            {
                throw new RiffParserException("Problem reading AVI header.");
            }

            fixed (Byte* bp = &ba[0])
            {
                Avimainheader* avi = (Avimainheader*)bp;
                mFrameRate = avi->dwMicroSecPerFrame;
                mHeight = avi->dwHeight;
                mMaxBitRate = avi->dwMaxBytesPerSec;
                mNumStreams = avi->dwStreams;
                mTotalFrames = avi->dwTotalFrames;
                mWidth = avi->dwWidth;
            }
        }

        private unsafe void DecodeAviStream(RiffParser rp, int length)
        {
            byte[] ba = new byte[length];

            if (rp.ReadData(ba, 0, length) != length)
            {
                throw new RiffParserException("Problem reading AVI header.");
            }

            fixed (Byte* bp = &ba[0])
            {
                Avistreamheader* avi = (Avistreamheader*)bp;

                if (AviRiffData.StreamtypeVideo == avi->fccType)
                {
                    mVidHandler = RiffParser.FromFourCc(avi->fccHandler);
                    if (avi->dwScale > 0)
                    {
                        mVidDataRate = (double)avi->dwRate / (double)avi->dwScale;
                    }
                    else
                    {
                        mVidDataRate = 0.0;
                    }
                }
                else if (AviRiffData.StreamtypeAudio == avi->fccType)
                {
                    mAudHandler = AviRiffData.CkidMp3 == avi->fccHandler ? "MP3" : RiffParser.FromFourCc(avi->fccHandler);
                    if (avi->dwScale > 0)
                    {
                        mAudDataRate = 8.0 * avi->dwRate / avi->dwScale;
                        if (avi->dwSampleSize > 0)
                        {
                            mAudDataRate /= avi->dwSampleSize;
                        }
                    }
                    else
                    {
                        mAudDataRate = 0.0;
                    }
                }
            }
        }

        #endregion Decode AVI

        #region WAVE processing

        private void ProcessWaveChunk(RiffParser rp, int fourCc, int unpaddedLength, int length)
        {
            // Is this a 'fmt' chunk?
            if (AviRiffData.CkidWaveFmt == fourCc)
            {
                DecodeWave(rp, length);
            }
            else
            {
                rp.SkipData(length);
            }
        }

        private unsafe void DecodeWave(RiffParser rp, int length)
        {
            byte[] ba = new byte[length];
            rp.ReadData(ba, 0, length);

            fixed (byte* bp = &ba[0])
            {
                Waveformatex* wave = (Waveformatex*)bp;
                mNumChannels = wave->nChannels;
                mBitsPerSec = wave->nAvgBytesPerSec;
                mBitsPerSample = wave->wBitsPerSample;
                mSamplesPerSec = wave->nSamplesPerSec;
            }
        }

        public void ProcessMainWave()
        {
            Clear();
            int length = Parser.DataSize;

            RiffParser.ProcessChunkElement pdc = ProcessWaveChunk;
            RiffParser.ProcessListElement pal = ProcessList;

            while (length > 0)
            {
                if (false == Parser.ReadElement(ref length, pdc, pal)) break;
            }
        }

        #endregion WAVE processing

    }
}