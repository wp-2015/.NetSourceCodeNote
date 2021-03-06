//------------------------------------------------------------------------------
// <copyright file="RequestResizeEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Windows.Forms {

    using System.Diagnostics;

    using System;
    using System.Drawing;
    using Microsoft.Win32;

    /// <include file='doc\RequestResizeEvent.uex' path='docs/doc[@for="ContentsResizedEventArgs"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides data for the <see cref='System.Windows.Forms.RichTextBox.ContentsResized'/> event.
    ///    </para>
    /// </devdoc>
    public class ContentsResizedEventArgs : EventArgs {
        readonly Rectangle newRectangle;

        /// <include file='doc\RequestResizeEvent.uex' path='docs/doc[@for="ContentsResizedEventArgs.ContentsResizedEventArgs"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Windows.Forms.ContentsResizedEventArgs'/>
        ///       class.
        ///    </para>
        /// </devdoc>

        public ContentsResizedEventArgs(Rectangle newRectangle) {
            this.newRectangle = newRectangle;
        }
        
        /// <include file='doc\RequestResizeEvent.uex' path='docs/doc[@for="ContentsResizedEventArgs.NewRectangle"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Represents the requested size of the <see cref='System.Windows.Forms.RichTextBox'/> control.
        ///    </para>
        /// </devdoc>
        public Rectangle NewRectangle {
            get {
                return newRectangle;
            }
        }
    }
}
