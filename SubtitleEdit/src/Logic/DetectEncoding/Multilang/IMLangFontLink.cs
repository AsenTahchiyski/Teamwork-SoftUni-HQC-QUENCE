﻿#pragma warning disable 0108

namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [ComImport, InterfaceType(1), ComConversionLoss, Guid("359F3441-BD4A-11D0-B188-00AA0038C969")]
    public interface IMLangFontLink : IMLangCodePages
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCharCodePages([In] ushort chSrc, out uint pdwCodePages);
      
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetStrCodePages([In] ref ushort pszSrc, [In] int cchSrc, [In] uint dwPriorityCodePages, out uint pdwCodePages, out int pcchCodePages);
      
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CodePageToCodePages([In] uint uCodePage, out uint pdwCodePages);
       
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CodePagesToCodePage([In] uint dwCodePages, [In] uint uDefaultCodePage, out uint puCodePage);
     
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFontCodePages([In, ComAliasName("MultiLanguage.wireHDC")] ref RemotableHandle hDc, [In, ComAliasName("MultiLanguage.wireHFONT")] ref RemotableHandle hFont, out uint pdwCodePages);
      
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void MapFont([In, ComAliasName("MultiLanguage.wireHDC")] ref RemotableHandle hDc, [In] uint dwCodePages, [In, ComAliasName("MultiLanguage.wireHFONT")] ref RemotableHandle hSrcFont, [Out, ComAliasName("MultiLanguage.wireHFONT")] IntPtr phDestFont);
     
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ReleaseFont([In, ComAliasName("MultiLanguage.wireHFONT")] ref RemotableHandle hFont);
     
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ResetFontMapping();
    }
}
