// (c) Giora Tamir (giora@gtamir.com), 2005
namespace Nikse.SubtitleEdit.Logic.ContainerFormats
{

    using System;
    using System.IO;
    using System.Runtime.Serialization;

    #region RiffParserException

    [Serializable]
    public class RiffParserException : ApplicationException
    {
        public RiffParserException()
        {
        }

        public RiffParserException(string message)
            : base(message)
        {
        }

        public RiffParserException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public RiffParserException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion RiffParserException

    public class RiffParser : IDisposable
    {
        #region CONSTANTS

        public const int Dwordsize = 4;
        public const int Twodwordssize = 8;
        public const string Riff4Cc = "RIFF";
        public const string Rifx4Cc = "RIFX";
        public const string List4Cc = "LIST";

        // Known file types
        public static readonly int CkidAvi = ToFourCC("AVI ");
        public static readonly int CkidWav = ToFourCC("WAVE");
        public static readonly int CkidRmid = ToFourCC("RMID");

        #endregion CONSTANTS

        #region private members

        private string mFilename;
        private string mShortname;
        private long mFilesize;
        private int mDatasize;
        private FileStream mStream;
        private int mFileriff;
        private int mFiletype;

        // For non-thread-safe memory optimization
        private readonly byte[] mEightBytes = new byte[Twodwordssize];
        private readonly byte[] mFourBytes = new byte[Dwordsize];

        #endregion private members

        #region Delegates

        /// <summary>
        /// Method to be called when a list element is found
        /// </summary>
        /// <param name="fourCcType"></param>
        /// <param name="length"></param>
        public delegate void ProcessListElement(RiffParser rp, int fourCcType, int length);

        /// <summary>
        /// Method to be called when a chunk element is found
        /// </summary>
        /// <param name="fourCcType"></param>
        /// <param name="unpaddedLength"></param>
        /// <param name="paddedLength"></param>
        public delegate void ProcessChunkElement(RiffParser rp, int fourCcType, int unpaddedLength, int paddedLength);

        #endregion Delegates

        #region public Members

        /// <summary>
        /// RIFF data segment size
        /// </summary>
        public int DataSize
        {
            get
            {
                return mDatasize;
            }
        }

        /// <summary>
        /// Current file name
        /// </summary>
        public string FileName
        {
            get
            {
                return mFilename;
            }
        }

        /// <summary>
        /// Current short (name only) file name
        /// </summary>
        public string ShortName
        {
            get
            {
                return mShortname;
            }
        }

        /// <summary>
        /// Return the general file type (RIFF or RIFX);
        /// </summary>
        public int FileRiff
        {
            get
            {
                return mFileriff;
            }
        }

        /// <summary>
        /// Return the specific file type (AVI/WAV...)
        /// </summary>
        public int FileType
        {
            get
            {
                return mFiletype;
            }
        }

        #endregion public Members

        /// <summary>
        /// Determine if the file is a valid RIFF file
        /// </summary>
        /// <param name="filename">File to examine</param>
        /// <returns>True if file is a RIFF file</returns>
        public void OpenFile(string filename)
        {
            // Sanity check
            if (null != mStream)
            {
                throw new RiffParserException("RIFF file already open " + FileName);
            }

            bool errorOccured = false;

            // Opening a new file
            try
            {
                FileInfo fi = new FileInfo(filename);
                mFilename = fi.FullName;
                mShortname = fi.Name;
                mFilesize = fi.Length;
                //fi = null;

                //Console.WriteLine(ShortName + " is a valid file.");

                // Read the RIFF header
                mStream = new FileStream(mFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                int fourCc;
                int datasize;
                int fileType;

                ReadTwoInts(out fourCc, out datasize);
                ReadOneInt(out fileType);

                mFileriff = fourCc;
                mFiletype = fileType;

                // Check for a valid RIFF header
                string riff = FromFourCc(fourCc);
                if (riff == Riff4Cc || riff == Rifx4Cc)
                {
                    // Good header. Check size
                    //Console.WriteLine(ShortName + " has a valid type \"" + riff + "\"");
                    //Console.WriteLine(ShortName + " has a specific type of \"" + FromFourCC(fileType) + "\"");

                    mDatasize = datasize;
                    if (mFilesize >= mDatasize + Twodwordssize)
                    {
                        //Console.WriteLine(ShortName + " has a valid size");
                    }
                    else
                    {
                        mStream.Close(); mStream = null;
                        throw new RiffParserException("Error. Truncated file " + FileName);
                    }
                }
                else
                {
                    mStream.Close();
                    mStream.Dispose();
                    mStream = null;
                    throw new RiffParserException("Error. Not a valid RIFF file " + FileName);
                }
            }

            catch (RiffParserException)
            {
                errorOccured = true;
                throw;
            }
            catch (Exception exception)
            {
                errorOccured = true;
                throw new RiffParserException("Error. Problem reading file " + FileName, exception);
            }
            finally
            {
                if (errorOccured && (null != mStream))
                {
                    mStream.Close();
                    mStream.Dispose();
                    mStream = null;
                }
            }
        }

        /// <summary>
        /// Read the next RIFF element invoking the correct delegate.
        /// Returns true if an element can be read
        /// </summary>
        /// <param name="bytesleft">Reference to number of bytes left in the current list</param>
        /// <param name="chunk">Method to invoke if a chunk is found</param>
        /// <param name="list">Method to invoke if a list is found</param>
        /// <returns></returns>
        public bool ReadElement(ref int bytesleft, ProcessChunkElement chunk, ProcessListElement list)
        {
            // Are we done?
            if (Twodwordssize > bytesleft)
            {
                return false;
            }

            //Console.WriteLine(m_stream.Position.ToString() + ", " + bytesleft.ToString());

            // We have enough bytes, read
            int fourCc;
            int size;

            ReadTwoInts(out fourCc, out size);

            // Reduce bytes left
            bytesleft -= Twodwordssize;

            // Do we have enough bytes?
            if (bytesleft < size)
            {
                // Skip the bad data and throw an exception
                SkipData(bytesleft);
                bytesleft = 0;
                throw new RiffParserException("Element size mismatch for element " + FromFourCc(fourCc)
                + " need " + size + " but have only " + bytesleft);
            }

            // Examine the element, is it a list or a chunk
            string type = FromFourCc(fourCc);
            if (type == List4Cc)
            {
                // We have a list
                ReadOneInt(out fourCc);

                if (null == list)
                {
                    SkipData(size - 4);
                }
                else
                {
                    // Invoke the list method
                    list(this, fourCc, size - 4);
                }

                // Adjust size
                bytesleft -= size;
            }
            else
            {
                // Calculated padded size - padded to WORD boundary
                int paddedSize = size;
                if (0 != (size & 1)) ++paddedSize;

                if (null == chunk)
                {
                    SkipData(paddedSize);
                }
                else
                {
                    chunk(this, fourCc, size, paddedSize);
                }

                // Adjust size
                bytesleft -= paddedSize;
            }

            return true;
        }

        #region Stream access

        /// <summary>
        /// Non-thread-safe method to read two ints from the stream
        /// </summary>
        /// <param name="FourCC">Output FourCC int</param>
        /// <param name="size">Output chunk/list size</param>
        public unsafe void ReadTwoInts(out int FourCC, out int size)
        {
            try
            {
                int readsize = mStream.Read(mEightBytes, 0, Twodwordssize);

                if (Twodwordssize != readsize)
                {
                    throw new RiffParserException("Unable to read. Corrupt RIFF file " + FileName);
                }

                fixed (byte* bp = &mEightBytes[0])
                {
                    FourCC = *((int*)bp);
                    size = *((int*)(bp + Dwordsize));
                }
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem accessing RIFF file " + FileName, ex);
            }
        }

        /// <summary>
        /// Non-thread-safe read a single int from the stream
        /// </summary>
        /// <param name="FourCC">Output int</param>
        public unsafe void ReadOneInt(out int FourCC)
        {
            try
            {
                int readsize = mStream.Read(mFourBytes, 0, Dwordsize);

                if (Dwordsize != readsize)
                {
                    throw new RiffParserException("Unable to read. Corrupt RIFF file " + FileName);
                }

                fixed (byte* bp = &mFourBytes[0])
                {
                    FourCC = *((int*)bp);
                }
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem accessing RIFF file " + FileName, ex);
            }
        }

        /// <summary>
        /// Skip the specified number of bytes
        /// </summary>
        /// <param name="skipBytes">Number of bytes to skip</param>
        public void SkipData(int skipBytes)
        {
            try
            {
                mStream.Seek(skipBytes, SeekOrigin.Current);
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem seeking in file " + FileName, ex);
            }
        }

        /// <summary>
        /// Read the specified length into the byte array at the specified
        /// offset in the array
        /// </summary>
        /// <param name="data">Array of bytes to read into</param>
        /// <param name="offset">Offset in the array to start from</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>Number of bytes actually read</returns>
        public int ReadData(Byte[] data, int offset, int length)
        {
            try
            {
                return mStream.Read(data, offset, length);
            }
            catch (Exception ex)
            {
                throw new RiffParserException("Problem reading data in file " + FileName, ex);
            }
        }

        /// <summary>
        /// Close the RIFF file
        /// </summary>
        public void CloseFile()
        {
            if (null == mStream)
            {
                return;
            }

            mStream.Close();
            mStream = null;
        }

        #endregion Stream access

        #region FourCC conversion methods

        public static string FromFourCc(int fourCc)
        {
            char[] chars = new char[4];
            chars[0] = (char)(fourCc & 0xFF);
            chars[1] = (char)((fourCc >> 8) & 0xFF);
            chars[2] = (char)((fourCc >> 16) & 0xFF);
            chars[3] = (char)((fourCc >> 24) & 0xFF);

            return new string(chars);
        }

        public static int ToFourCC(string fourCc)
        {
            if (fourCc.Length != 4)
            {
                throw new Exception("FourCC strings must be 4 characters long " + fourCc);
            }

            int result = ((int)fourCc[3]) << 24
                        | ((int)fourCc[2]) << 16
                        | ((int)fourCc[1]) << 8
                        | ((int)fourCc[0]);

            return result;
        }

        public static int ToFourCC(char[] fourCc)
        {
            if (fourCc.Length != 4)
            {
                throw new Exception("FourCC char arrays must be 4 characters long " + new string(fourCc));
            }

            int result = ((int)fourCc[3]) << 24
                        | ((int)fourCc[2]) << 16
                        | ((int)fourCc[1]) << 8
                        | ((int)fourCc[0]);

            return result;
        }

        public static int ToFourCC(char c0, char c1, char c2, char c3)
        {
            int result = ((int)c3) << 24
                        | ((int)c2) << 16
                        | ((int)c1) << 8
                        | ((int)c0);

            return result;
        }

        #endregion FourCC conversion methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (mStream != null)
                {
                    mStream.Dispose();
                    mStream = null;
                }
            }
        }
    }
}