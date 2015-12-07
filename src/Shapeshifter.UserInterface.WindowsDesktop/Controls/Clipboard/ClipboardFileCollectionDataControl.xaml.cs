﻿namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Controls;

    using Interfaces;

    /// <summary>
    ///     Interaction logic for ClipboardFileCollectionDataControl.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardFileCollectionDataControl
        : UserControl,
          IClipboardControl
    {
        public ClipboardFileCollectionDataControl()
        {
            InitializeComponent();
        }
    }
}