﻿namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using MultiLanguage;

    [ComImport, Guid("275C23E3-3747-11D0-9FEA-00AA003F8646"), InterfaceType(1)]
    public interface IEnumCodePage
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumCodePage ppEnum);
     
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Next([In] uint celt, out TagMimecpinfo rgelt, out uint pceltFetched);
      
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Reset();
      
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Skip([In] uint celt);
    }
}
