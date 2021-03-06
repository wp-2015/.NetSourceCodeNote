// <copyright file="DpiUtil+DpiAwarenessScope.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace MS.Internal
{
    using MS.Utility;
    using MS.Win32;
    using System;
    using System.Security;

    /// <content>
    /// Contains inner type <see cref="DpiAwarenessScope"/>
    /// </content>
    internal static partial class DpiUtil
    {
        /// <summary>
        /// Helper class used to switch back and forth between different
        /// thread DPI_AWARENESS_CONTEXT values
        /// </summary>
        internal class DpiAwarenessScope : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DpiAwarenessScope"/> class, and sets the current thread's
            /// DPI_AWARENESS_CONTEXT to the value requested by <paramref name="dpiAwarenessContextEnumValue"/>
            /// </summary>
            /// <param name="dpiAwarenessContextEnumValue">DPI_AWARENESS_CONTEXT to set the thread to</param>
            public DpiAwarenessScope(DpiAwarenessContextValue dpiAwarenessContextEnumValue)
                : this(
                      dpiAwarenessContextEnumValue,
                      updateIfThreadInMixedHostingMode: false,
                      updateIfWindowIsSystemAwareOrUnaware: false,
                      hWnd: IntPtr.Zero)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DpiAwarenessScope"/> class, and sets the current thread's
            /// DPI_AWARENESS_CONTEXT to the value requested by <paramref name="dpiAwarenessContextEnumValue"/>
            /// </summary>
            /// <param name="dpiAwarenessContextEnumValue">New mode</param>
            /// <param name="updateIfThreadInMixedHostingMode">
            /// When true, checks whether the current thread is in mixed hosting mode,
            /// and only then switches the thread to the new mode
            /// </param>
            public DpiAwarenessScope(
                DpiAwarenessContextValue dpiAwarenessContextEnumValue, bool updateIfThreadInMixedHostingMode)
                : this(
                      dpiAwarenessContextEnumValue,
                      updateIfThreadInMixedHostingMode,
                      updateIfWindowIsSystemAwareOrUnaware: false,
                      hWnd: IntPtr.Zero)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DpiAwarenessScope"/> class, and sets the current thread's
            /// DPI_AWARENESS_CONTEXT to the value requested by <paramref name="dpiAwarenessContextEnumValue"/>
            /// </summary>
            /// <param name="dpiAwarenessContextEnumValue">New DPI awareness value</param>
            /// <param name="updateIfThreadInMixedHostingMode">
            /// When true, checks whether the current thread is in mixed hosting mode,
            /// and only then switches the thread to the new mode
            /// </param>
            /// <param name="hWnd">
            /// The window which is tested to see whether it is in mixed hosting mode.
            /// Only if the window is found to be in mixed hosting mode is the current threads DPI awareness context
            /// switched to the new value
            /// </param>
            public DpiAwarenessScope(
                DpiAwarenessContextValue dpiAwarenessContextEnumValue, bool updateIfThreadInMixedHostingMode, IntPtr hWnd)
                : this(
                    dpiAwarenessContextEnumValue,
                    updateIfThreadInMixedHostingMode: updateIfThreadInMixedHostingMode,
                    updateIfWindowIsSystemAwareOrUnaware: true,
                    hWnd: hWnd)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DpiAwarenessScope"/> class, and sets the current thread's
            /// DPI_AWARENESS_CONTEXT to the value requested by <paramref name="dpiAwarenessContextValue"/>
            /// </summary>
            /// <param name="dpiAwarenessContextValue">New DPI awareness value</param>
            /// <param name="updateIfThreadInMixedHostingMode">When true, the current thread is switched to the new mode iff the current thread is already in mixed hosting mode</param>
            /// <param name="updateIfWindowIsSystemAwareOrUnaware">When true, the current thread is switched to the new mode iff <paramref name="hWnd"/> is in System Aware or Unaware DPI mode</param>
            /// <param name="hWnd">Window which is tested in conjunction with <paramref name="updateIfWindowIsSystemAwareOrUnaware"/></param>
            /// <SecurityNote>
            ///     Critical:   Calls into native methods
            ///     Safe:       Does not return Critical resources back to the caller.
            ///                 The handle saved in this instance is a pseudo-handle which is really just an integer.
            /// </SecurityNote>
            [SecuritySafeCritical]
            private DpiAwarenessScope(
                DpiAwarenessContextValue dpiAwarenessContextValue,
                bool updateIfThreadInMixedHostingMode,
                bool updateIfWindowIsSystemAwareOrUnaware,
                IntPtr hWnd)
            {
                if (dpiAwarenessContextValue == DpiAwarenessContextValue.Invalid)
                {
                    return;
                }

                if (!OperationSupported)
                {
                    return;
                }

                if (updateIfThreadInMixedHostingMode && !this.IsThreadInMixedHostingBehavior)
                {
                    return;
                }

                if (updateIfWindowIsSystemAwareOrUnaware &&
                    (hWnd == IntPtr.Zero || !this.IsWindowUnawareOrSystemAware(hWnd)))
                {
                    return;
                }

                try
                {
                    this.OldDpiAwarenessContext = 
                        UnsafeNativeMethods.SetThreadDpiAwarenessContext(
                            new DpiAwarenessContextHandle(dpiAwarenessContextValue));
                }
                catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                {
                    OperationSupported = false;
                }
            }

            /// <summary>
            /// Gets or sets a value indicating whether SetThreadDpiAwarenessContext is supported
            /// on the running platform. This flag will be set to false and future attempts to use
            /// <see cref="DpiAwarenessScope"/> will be ignored silently when SetThreadDpiAwarenessContext
            /// is not supported.
            /// </summary>
            private static bool OperationSupported { get; set; } = true;

            /// <summary>
            /// Gets a value indicating whether the current thread is in DPI_HOSTING_BEHAVIOR_MIXED
            /// </summary>
            /// <SecurityNote>
            ///     Critical: Calls into native methods
            ///     Safe: returns only non-Critical and safe information to the caller
            /// </SecurityNote>
            private bool IsThreadInMixedHostingBehavior
            {
                [SecuritySafeCritical]
                get
                {
                    return SafeNativeMethods.GetThreadDpiHostingBehavior() == NativeMethods.DPI_HOSTING_BEHAVIOR.DPI_HOSTING_BEHAVIOR_MIXED;
                }
            }

            /// <summary>
            /// Gets or sets the saved valued of the old DPI_AWERENESS_CONTEXT_HANDLE which will be restored
            /// when <see cref="Dispose"/> is called
            /// </summary>
            private DpiAwarenessContextHandle OldDpiAwarenessContext { get; set; } = null;

            /// <summary>
            /// Restores the current thread to its previous DPI_AWARENESS_CONTEXT value
            /// </summary>
            /// <SecurityNote>
            ///     Critical: Calls into native methods
            ///     Safe: Does not return any information to the caller
            /// </SecurityNote>
            [SecuritySafeCritical]
            public void Dispose()
            {
                if (this.OldDpiAwarenessContext != null)
                {
                    UnsafeNativeMethods.SetThreadDpiAwarenessContext(this.OldDpiAwarenessContext);
                    this.OldDpiAwarenessContext = null;
                }
            }

            /// <summary>
            /// Tests whether <paramref name="hWnd"/> is in System Aware or Unaware DPI context
            /// </summary>
            /// <param name="hWnd">Handle to the window</param>
            /// <SecurityNote>
            ///     Critical: Calls into native methods
            ///     Safe: returns only non-Critical and safe information to the caller
            /// </SecurityNote>
            /// <returns>True if the Window is Unaware or System Aware, otherwise False</returns>
            [SecuritySafeCritical]
            private bool IsWindowUnawareOrSystemAware(IntPtr hWnd)
            {
                var dpiAwarenessContext = GetDpiAwarenessContext(hWnd);

                return
                    dpiAwarenessContext.Equals(DpiAwarenessContextValue.Unaware) ||
                    dpiAwarenessContext.Equals(DpiAwarenessContextValue.SystemAware);
            }
        }
    }
}
