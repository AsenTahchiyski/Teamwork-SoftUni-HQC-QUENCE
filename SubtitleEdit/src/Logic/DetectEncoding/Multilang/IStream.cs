#pragma warning disable 0108

namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [ComImport, Guid("0000000C-0000-0000-C000-000000000046"), InterfaceType(1)]
    public interface IStream : ISequentialStream
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoteRead(IntPtr pv, uint cb, ref uint pcbRead);
      
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoteWrite([In] ref byte pv, [In] uint cb, ref uint pcbWritten);
      
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoteSeek([In] LargeInteger dlibMove, [In] uint dwOrigin, IntPtr plibNewPosition);
       
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSize([In] UlargeInteger libNewSize);
     
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoteCopyTo([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In] UlargeInteger cb, out UlargeInteger pcbRead, out UlargeInteger pcbWritten);
     
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Commit([In] uint grfCommitFlags);
      
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Revert();
    
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LockRegion([In] UlargeInteger libOffset, [In] UlargeInteger cb, [In] uint dwLockType);
     
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnlockRegion([In] UlargeInteger libOffset, [In] UlargeInteger cb, [In] uint dwLockType);
   
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Stat(out TagStatstg pstatstg, [In] uint grfStatFlag);
    
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Clone([MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
    }
}

#pragma warning restore 0108
