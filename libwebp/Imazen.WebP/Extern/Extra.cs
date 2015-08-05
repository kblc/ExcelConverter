﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Imazen.WebP.Extern {
    public partial class NativeMethods {

        [DllImportAttribute("libwebp", EntryPoint = "WebPSafeFree")]
        public static extern void WebPSafeFree(IntPtr toDeallocate);
    }
}