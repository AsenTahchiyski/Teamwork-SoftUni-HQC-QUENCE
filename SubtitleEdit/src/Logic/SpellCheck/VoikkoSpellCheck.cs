namespace Nikse.SubtitleEdit.Logic.SpellCheck
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    public class VoikkoSpellCheck : Hunspell
    {
        // Voikko functions in dll
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr VoikkoInit(ref IntPtr error, byte[] languageCode, byte[] path);
        private VoikkoInit voikkoInit;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void VoikkoTerminate(IntPtr libVlc);
        private VoikkoTerminate voikkoTerminate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Int32 VoikkoSpell(IntPtr handle, byte[] word);
        private VoikkoSpell voikkoSpell;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr VoikkoSuggest(IntPtr handle, byte[] word);
        private VoikkoSuggest voikkoSuggest;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr VoikkoFreeCstrArray(IntPtr array);
        private VoikkoFreeCstrArray voikkoFreeCstrArray;

        private IntPtr libDll = IntPtr.Zero;
        private IntPtr libVoikko;

        private static string N2S(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            List<byte> bytes = new List<byte>();
            unsafe
            {
                for (byte* p = (byte*)ptr; *p != 0; p++)
                {
                    bytes.Add(*p);
                }
            }

            return N2S(bytes.ToArray());
        }

        private static string N2S(byte[] bytes)
        {
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }

        private static byte[] S2N(string str)
        {
            return S2Encoding(str, Encoding.UTF8);
        }

        private static byte[] S2Ansi(string str)
        {
            return S2Encoding(str, Encoding.Default);
        }

        private static byte[] S2Encoding(string str, Encoding encoding)
        {
            return str == null ? null : encoding.GetBytes(str + '\0');
        }

        private object GetDllType(Type type, string name)
        {
            IntPtr address = NativeMethods.GetProcAddress(libDll, name);
            return address != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer(address, type) : null;
        }

        /// <summary>
        /// Load dll dynamic + set pointers to needed methods
        /// </summary>
        /// <param name="baseFolder"></param>
        private void LoadLibVoikkoDynamic(string baseFolder)
        {
            string dllFile = Path.Combine(baseFolder, "Voikkox86.dll");
            if (IntPtr.Size == 8)
            {
                dllFile = Path.Combine(baseFolder, "Voikkox64.dll");
            }

            if (!File.Exists(dllFile))
            {
                throw new FileNotFoundException(dllFile);
            }

            libDll = NativeMethods.LoadLibrary(dllFile);
            if (libDll == IntPtr.Zero)
            {
                throw new FileLoadException("Unable to load " + dllFile);
            }

            voikkoInit = (VoikkoInit)GetDllType(typeof(VoikkoInit), "voikkoInit");
            voikkoTerminate = (VoikkoTerminate)GetDllType(typeof(VoikkoTerminate), "voikkoTerminate");
            voikkoSpell = (VoikkoSpell)GetDllType(typeof(VoikkoSpell), "voikkoSpellCstr");
            voikkoSuggest = (VoikkoSuggest)GetDllType(typeof(VoikkoSuggest), "voikkoSuggestCstr");
            voikkoFreeCstrArray = (VoikkoFreeCstrArray)GetDllType(typeof(VoikkoFreeCstrArray), "voikkoFreeCstrArray");

            if (voikkoInit == null || voikkoTerminate == null || voikkoSpell == null || voikkoSuggest == null || voikkoFreeCstrArray == null)
            {
                throw new FileLoadException("Not all methods in Voikko dll could be found!");
            }
        }

        public override bool Spell(string word)
        {
            return !string.IsNullOrEmpty(word) && Convert.ToBoolean(voikkoSpell(libVoikko, S2N(word)));
        }

        public override List<string> Suggest(string word)
        {
            var suggestions = new List<string>();
            if (string.IsNullOrEmpty(word))
            {
                return suggestions;
            }

            IntPtr voikkoSuggestCstr = voikkoSuggest(libVoikko, S2N(word));
            if (voikkoSuggestCstr == IntPtr.Zero)
            {
                return suggestions;
            }

            unsafe
            {
                for (byte** cStr = (byte**)voikkoSuggestCstr; *cStr != (byte*)0; cStr++)
                    suggestions.Add(N2S(new IntPtr(*cStr)));
            }

            voikkoFreeCstrArray(voikkoSuggestCstr);
            return suggestions;
        }

        public VoikkoSpellCheck(string baseFolder, string dictionaryFolder)
        {
            LoadLibVoikkoDynamic(baseFolder);

            var error = new IntPtr();
            libVoikko = voikkoInit(ref error, S2N("fi"), S2Ansi(dictionaryFolder));
            if (libVoikko == IntPtr.Zero && error != IntPtr.Zero)
            {
                throw new Exception(N2S(error));
            }
        }

        ~VoikkoSpellCheck()
        {
            Dispose(false);
        }

        private void ReleaseUnmangedResources()
        {
            try
            {
                if (libVoikko != IntPtr.Zero)
                {
                    voikkoTerminate(libVoikko);
                    libVoikko = IntPtr.Zero;
                }

                if (libDll == IntPtr.Zero)
                {
                    return;
                }

                NativeMethods.FreeLibrary(libDll);
                libDll = IntPtr.Zero;
            }
            catch
            {
            }
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
