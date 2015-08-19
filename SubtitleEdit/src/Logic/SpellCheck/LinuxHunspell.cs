﻿namespace Nikse.SubtitleEdit.Logic.SpellCheck
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public class LinuxHunspell : Hunspell
    {
        private IntPtr hunspellHandle;

        public LinuxHunspell(string affDirectory, string dicDictory)
        {
            //Also search - /usr/share/hunspell
            try
            {
                hunspellHandle = NativeMethods.Hunspell_create(affDirectory, dicDictory);
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Unable to start hunspell spell checker - make sure hunspell is installed!");
                throw;
            }
        }

        public override bool Spell(string word)
        {
            return NativeMethods.Hunspell_spell(hunspellHandle, word) != 0;
        }

        public override List<string> Suggest(string word)
        {
            IntPtr pointerToAddressStringArray = Marshal.AllocHGlobal(IntPtr.Size);
            int resultCount = NativeMethods.Hunspell_suggest(hunspellHandle, pointerToAddressStringArray, word);
            IntPtr addressStringArray = Marshal.ReadIntPtr(pointerToAddressStringArray);
            List<string> results = new List<string>();
            for (int i = 0; i < resultCount; i++)
            {
                IntPtr addressCharArray = Marshal.ReadIntPtr(addressStringArray, i * IntPtr.Size);
                string suggestion = Marshal.PtrToStringAuto(addressCharArray);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    results.Add(suggestion);
                }
            }

            NativeMethods.Hunspell_free_list(hunspellHandle, pointerToAddressStringArray, resultCount);
            Marshal.FreeHGlobal(pointerToAddressStringArray);

            return results;
        }

        ~LinuxHunspell()
        {
            Dispose(false);
        }

        private void ReleaseUnmangedResources()
        {
            if (hunspellHandle == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.Hunspell_destroy(hunspellHandle);
            hunspellHandle = IntPtr.Zero;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            //if (disposing)
            //{
            //    //ReleaseManagedResources();
            //}
            ReleaseUnmangedResources();
        }
    }
}
